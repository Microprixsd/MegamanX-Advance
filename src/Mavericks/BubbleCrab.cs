﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class BubbleCrab : Maverick {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.BCrabGeneric, 143); }

	public BCrabShieldProj? shield;
	public List<BCrabSummonCrabProj> crabs = new List<BCrabSummonCrabProj>();
	public bool lastFrameWasUnderwater;

	float clawSoundTime;

	public BubbleCrab(
		Player player, Point pos, Point destPos, int xDir,
		ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		/*stateCooldowns = new() {
			{ typeof(BCrabShieldStartState), new(45, true) }
		};*/

		weapon = getWeapon();

		awardWeaponId = WeaponIds.BubbleSplash;
		weakWeaponId = WeaponIds.SpinWheel;
		weakMaverickWeaponId = WeaponIds.WheelGator;

		netActorCreateId = NetActorCreateId.BubbleCrab;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		usesAmmo = true;
		canHealAmmo = true;
		ammo = 32;
		maxAmmo = 32;
		grayAmmoLevel = 8;
		barIndexes = (62, 51);

		armorClass = ArmorClass.Light;
		gameMavs = GameMavs.X2;
		height = 28;
	}

	public override void update() {
		base.update();
		subtractTargetDistance = 30;

		Helpers.decrementTime(ref clawSoundTime);
		if (sprite.name.Contains("jump_attack")) {
			if (clawSoundTime == 0) {
				clawSoundTime = 0.03f;
				playSound("bcrabClaw");
			}
		}

		if (!ownedByLocalPlayer) return;

		if (sprite.name.Contains("jump_attack")) {
			if (shield != null) {
				shield.destroySelf();
				shield = null;
			}
		}

		if (shield != null) {
			if (shield.destroyed) {
				shield = null;
			} else {
				//shield.changePos(getFirstPOIOrDefault("shield"));
				shield.changePos(getCenterPos());
			}
		}

		if (shield == null) {
			rechargeAmmo(1);
		}

		bool floating = false;
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					changeState(new BCrabShootState());
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(getSpecialState());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new BCrabClawState());
				}
			} else if (state is MJump || state is MFall) {
				if (input.isPressed(Control.Dash, player)) {
					changeState(new BCrabClawJumpState());
				}
			}

			float floatY = -150;
			if ((state is MJump || state is MFall) && !grounded) {
				if (isUnderwater() && shield != null) {
					if (input.isHeld(Control.Jump, player) && vel.y > floatY) {
						vel.y = floatY;
						if (state is MFall) {
							floating = true;
							changeSpriteFromName("jump", true);
						}
					}
				} else {
					if (lastFrameWasUnderwater && input.isHeld(Control.Jump, player) && input.isHeld(Control.Up, player)) {
						vel.y = -425;
					}
				}
			}
		}

		if (!floating && state is MFall && vel.y > 0) {
			changeSpriteFromName("fall", true);
		}

		lastFrameWasUnderwater = isUnderwater() && shield != null;
	}

	public MaverickState getSpecialState() {
		if (shield == null && ammo >= 8) {
			return new BCrabShieldStartState();
		} else if (crabs.Count <= 3) {
			removeCrabs();
			return new BCrabSummonState();
		} else {
			return null!;
		}
	}
	public void removeCrabs() {
		if (crabs.Count >= 1) {
			for (int i = crabs.Count - 1; i >= 0; i--) {
				crabs[i].destroySelf();
			}
			crabs.Clear();
		}
	}

	public override string getMaverickPrefix() {
		return "bcrab";
	}

	public override MaverickState[] strikerStates() {
		return [
			new BCrabShootState(),
			//new BCrabSummonState(),
			new BCrabClawState()
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		if (shield == null) {
			return [
				new BCrabShieldStartState()
			];
		}
		if (enemyDist <= 30) {
			return [
				new BCrabShootState(),
				new BCrabClawState()
			];
		}
		return [
			new BCrabShootState(),
		];
	}

	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		JumpAttack,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"bcrab_jump_attack" or "bcrab_jump_attack_start" => MeleeIds.JumpAttack,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.JumpAttack => new GenericMeleeProj(
				weapon, pos, ProjIds.BCrabClaw, player,
				1, Global.defFlinch, 9, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void onDestroy() {
		base.onDestroy();
		shield?.destroySelf();
		foreach (var crab in crabs.ToList()) {
			crab?.destroySelf();
		}
		crabs.Clear();
	}
}
public class BCrabGenericWeapon : Weapon {
	public static BCrabGenericWeapon netWeapon = new();
	public BCrabGenericWeapon() {
		index = (int)WeaponIds.BCrabGeneric;
		killFeedIndex = 143;
	}
}

public class BCrabBubbleSplashProj : Projectile {
	bool once;
	int num;
	int type;
	public BCrabBubbleSplashProj(
		Point pos, int xDir, int num,
		int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bcrab_bubble_ring_start", netId, player
	) {
		weapon = BCrabGenericWeapon.netWeapon;
		damager.damage = 0;
		damager.hitCooldown = 9;
		damager.flinch = 0;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.BCrabBubbleSplash;
		this.num = num;
		this.type = type;
		fadeSprite = "bcrab_bubble_ring_poof";
		destroyOnHit = false;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir,
			 new byte[] { (byte)num, (byte)type}
			);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new BCrabBubbleSplashProj(
			args.pos, args.xDir, args.extraData[0], args.extraData[1], 
			args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (type == 1 && once) {
			vel.y -= Global.spf * 500;
			speed = vel.magnitude;
			if (vel.y < -150) {
				vel.y = -150;
			}
		}

		if (isAnimOver() && !once) {
			once = true;
			changeSprite("bcrab_bubble_ring", true);
			if (type == 0) {
				vel = new Point(xDir * 200, Helpers.randomRange(num == 0 ? -25 : -50, 0));
			} else {
				vel = new Point(xDir * 150, 0);
			}
			speed = vel.magnitude;
			maxDistance = 150;
			updateDamager(2);
			destroyOnHit = true;
		}
	}
}
public class BCrablosMState : MaverickState {
	public BubbleCrab BubblyCrablos = null!;
	public BCrablosMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		BubblyCrablos = maverick as BubbleCrab ?? throw new NullReferenceException();

	}
}
public class BCrabShootState : BCrablosMState {
	bool secondAnim;
	float shootCooldown;
	int num;

	public BCrabShootState() : base("ring_attack_start") {
	}

	public override void update() {
		base.update();
		if (BubblyCrablos == null) return;

		if (maverick.frameIndex == 0) {
			maverick.turnToInput(input, player);
		}

		Point? shootPos = maverick.getFirstPOI("bubble_ring");
		Helpers.decrementTime(ref shootCooldown);
		if (shootCooldown == 0 && shootPos != null) {
			shootCooldown = 0.25f;
			num = (num == 1 ? 0 : 1);
			maverick.playSound("bcrabShoot", sendRpc: true);
			int type = input.isHeld(Control.Up, player) ? 1 : 0;
			new BCrabBubbleSplashProj(
				shootPos.Value, maverick.xDir, num, type,
				BubblyCrablos, player, player.getNextActorNetId(), rpc: true
			);
		}

		if (!secondAnim) {
			if (maverick.isAnimOver()) {
				maverick.changeSpriteFromName("ring_attack", true);
				secondAnim = true;
			}
		} else {
			if (isAI) {
				if (maverick.loopCount >= 4) {
					maverick.changeState(new MIdle());
				}
			} else if (maverick.loopCount >= 4) {
				maverick.changeState(new MIdle());
			} else if (maverick.loopCount >= 1 && !input.isHeld(Control.Shoot, player)) {
				maverick.changeState(new MIdle());
			}
		}
	}
}

public class BCrabClawState : BCrablosMState {
	public BCrabClawState() : base("jump_attack_start") {
		canJump = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		maverick.turnToInput(input, player);
		if (!maverick.grounded) {
			maverick.changeState(new BCrabClawJumpState());
			return;
		}

		if (isAI) {
			if (stateTime > 2) {
				maverick.changeToIdleOrFall();
			}
		} else if (!input.isHeld(Control.Dash, player)) {
			maverick.changeToIdleOrFall();
			return;
		}
	}
}

public class BCrabClawJumpState : BCrablosMState {
	public BCrabClawJumpState() : base("jump_attack") {
		airMove = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		if (maverick.grounded) {
			maverick.changeState(new BCrabClawState());
			return;
		}

		if (isAI) {
			if (stateTime > 1) {
				maverick.changeToIdleOrFall();
			}
		} else if (!input.isHeld(Control.Dash, player)) {
			maverick.changeToIdleOrFall();
			return;
		}
	}
}

public class BCrabShieldProj : Projectile, IDamagable {
	public float health = 8;
	public float maxHealth = 8;

	public bool once;
	public BCrabShieldProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bcrab_shield", netId, player
	) {
		weapon = BCrabGenericWeapon.netWeapon;
		damager.damage = 0;
		damager.hitCooldown = 60;
		damager.flinch = 0;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.BCrabBubbleShield;
		syncScale = true;
		yScale = 0;
		setIndestructableProperties();
		if (ownerPlayer.character != null) setzIndex(ownerPlayer.character.zIndex - 100);
		alpha = 0.75f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new BCrabShieldProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (!once) {
			if (yScale < 1) {
				yScale += Global.spf * 2;
				if (yScale > 1) {
					once = true;
					yScale = 1;
				}
			}
		} else {
			updateBubbleBounce();
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;

		if (projId == (int)ProjIds.SpinWheel ||
			projId == (int)ProjIds.SpinWheelCharged ||
			projId == (int)ProjIds.WheelGSpinWheel
		) {
			damage *= 2;
		}
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damagerAlliance != owner.alliance;
	}
	public bool canBeHealed(int healerAlliance) {
		return (health < maxHealth);
	}
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		commonHealLogic(healer, healAmount, health, maxHealth, drawHealText);
		health += healAmount;
		if (health > maxHealth) {
			health = maxHealth;
		}
	}
	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}
	public bool isPlayableDamagable() {
		return false;
	}
}

public class BCrabShieldStartState : BCrablosMState {
	public BCrabShieldStartState() : base("shield_start") {
		aiAttackCtrl = true;
	}
	public override void update() {
		base.update();
		if (BubblyCrablos == null) return;

		if (!once) {
			Point? shootPos = BubblyCrablos.getFirstPOI("shield");
			if (!once && shootPos != null) {
				once = true;
				BubblyCrablos.deductAmmo(8);
				BubblyCrablos.playSound("bcrabShield", sendRpc: true);
				var shield = new BCrabShieldProj(
					shootPos.Value, maverick.xDir, BubblyCrablos,
					player, player.getNextActorNetId(), rpc: true
				);
				BubblyCrablos.shield = shield;
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class BCrabSummonBubbleProj : Projectile, IDamagable {
	public float health = 2;
	public float maxHealth = 2;

	public BCrabSummonBubbleProj(
		Point pos, int xDir, Actor owner,
		Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bcrab_summon_bubble", netId, player

	) {
		weapon = BCrabGenericWeapon.netWeapon;
		damager.damage = 0;
		damager.hitCooldown = 60;
		damager.flinch = 0;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.BCrabCrablingBubble;
		setIndestructableProperties();
		syncScale = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new BCrabSummonBubbleProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		updateBubbleBounce();
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;

		if (projId == (int)ProjIds.SpinWheel ||
			projId == (int)ProjIds.SpinWheelCharged ||
			projId == (int)ProjIds.WheelGSpinWheel
		) {
			damage *= 2;
		}
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return false;
	}
	public bool canBeHealed(int healerAlliance) {
		return false;
	}
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		commonHealLogic(healer, healAmount, health, maxHealth, drawHealText);
		health += healAmount;
		if (health > maxHealth) {
			health = maxHealth;
		}
	}
	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}
	public bool isPlayableDamagable() {
		return false;
	}
}

public class BCrabSummonCrabProj : Projectile, IDamagable {
	public float health = 4;
	public float maxHealth = 4;
	public float bubbleSummonCooldown = 120;

	int? moveDirOnce = null;

	BCrabSummonBubbleProj? shield;
	public BubbleCrab Crab;
	public BCrabSummonCrabProj(
		Weapon weapon, Point pos, Point vel, BubbleCrab Crab,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 0, 2, player, "bcrab_summon_crab",
		Global.halfFlinch, 1, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.BCrabCrabling;
		this.vel = vel;
		this.Crab = Crab;
		fadeSprite = "explosion";
		fadeSound = "explosionX2";
		if (collider != null) { collider.wallOnly = true; }
		useGravity = true;
		netcodeOverride = NetcodeModel.FavorDefender;
		setIndestructableProperties();

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}

	public override void onStart() {
		base.onStart();
		if (!ownedByLocalPlayer) return;
		shield = new BCrabSummonBubbleProj(pos, xDir, Crab, owner, owner.getNextActorNetId(), rpc: true);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (shield != null) {
			if (shield.destroyed) {
				shield = null;
			} else {
				shield.changePos(pos);
			}
		} else {
			bubbleSummonCooldown -= Global.speedMul;
			if (bubbleSummonCooldown <= 0) {
				bubbleSummonCooldown = 0;
				shield = new BCrabSummonBubbleProj(pos, xDir, Crab, owner, owner.getNextActorNetId(), rpc: true);
			}
		}
		patrol();
	}

	public void patrol() {
		var closestTarget = Global.level.getClosestTarget(pos, owner.alliance, false, 150, true);
		if (closestTarget != null) {
			if (moveDirOnce == null) moveDirOnce = MathF.Sign(closestTarget.pos.x - pos.x);
			var hitGround = Global.level.checkTerrainCollisionOnce(this, moveDirOnce.Value * 30, 20);
			var hitWall = Global.level.checkTerrainCollisionOnce(this, moveDirOnce.Value * Global.spf * 2, -5);
			bool blocked = (grounded && hitGround == null) || hitWall?.isSideWallHit() == true;
			if (!blocked) {
				move(new Point(moveDirOnce.Value * 100, 0));
			} else {
				moveDirOnce = null;
			}
		}
	}

	int bounces;
	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;

		if (shield == null) {
			if (other.isGroundHit()) {
				stopMoving();
			}
		} else {
			bounces++;
			if (bounces >= 2) {
				stopMoving();
				return;
			}

			var normal = other.hitData.normal ?? new Point(0, -1);
			if (normal.isSideways()) {
				vel.x *= -0.5f;
				shield.startShieldBounceX();
				incPos(new Point(5 * MathF.Sign(vel.x), 0));
			} else {
				vel.y *= -0.5f;
				shield.startShieldBounceY();
				if (vel.y < -300) vel.y = -300;
				incPos(new Point(0, 5 * MathF.Sign(vel.y)));
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		Crab.crabs.Remove(this);
		shield?.destroySelf();
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;
		if (shield != null) {
			shield.applyDamage(damage, owner, actor, weaponIndex, projId);
			return;
		}
		if (projId == (int)ProjIds.SpinWheel ||
			projId == (int)ProjIds.SpinWheelCharged ||
			projId == (int)ProjIds.WheelGSpinWheel
		) {
			damage *= 2;
		}
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damagerAlliance != owner.alliance;
	}
	public bool canBeHealed(int healerAlliance) {
		return (health < maxHealth);
	}
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
		commonHealLogic(healer, healAmount, health, maxHealth, drawHealText);
		health += healAmount;
		if (health > maxHealth) {
			health = maxHealth;
		}
	}
	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}
	public bool isPlayableDamagable() {
		return true;
	}
}

public class BCrabSummonState : BCrablosMState {
	public BCrabSummonState() : base("summon") {
	}
	public override void update() {
		base.update();
		if (BubblyCrablos == null) return;

		Point? shootPos = BubblyCrablos.getFirstPOI("summon_crab");
		if (!once && shootPos != null) {
			once = true;
			if (BubblyCrablos.crabs.Count < 3) BubblyCrablos.crabs.Add(
				new BCrabSummonCrabProj(
					maverick.weapon, shootPos.Value, new Point(-50, -300),
					BubblyCrablos, player, player.getNextActorNetId(), rpc: true)
				);
			if (BubblyCrablos.crabs.Count < 3) BubblyCrablos.crabs.Add(
				new BCrabSummonCrabProj(
					maverick.weapon, shootPos.Value, new Point(0, -300),
					BubblyCrablos, player, player.getNextActorNetId(), rpc: true)
				);
			if (BubblyCrablos.crabs.Count < 3) BubblyCrablos.crabs.Add(
				new BCrabSummonCrabProj(
					maverick.weapon, shootPos.Value, new Point(50, -300),
					BubblyCrablos, player, player.getNextActorNetId(), rpc: true)
				);
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}
}
