﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class ChillPenguin : Maverick {
	public static Weapon netWeapon = new Weapon(WeaponIds.ChillPGeneric, 93);
	public ChillPIceShotWeapon iceShotWeapon = new();
	public ChillPIceStatueWeapon iceStatueWeapon = new();
	public ChillPIceBlowWeapon iceWindWeapon = new();
	public ChillPBlizzardWeapon blizzardWeapon = new();
	public ChillPSlideWeapon slideWeapon = new();

	public ChillPenguin(
		Player player, Point pos, Point destPos,
		int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns = new() {
			{ typeof(ChillPIceBlowState), new(2 * 60, false, true) },
			{ typeof(ChillPSlideState), new(30, false, true) },
			{ typeof(ChillPBlizzardState), new(3 * 60) },
			{ typeof(MShoot), new(45, true) }
		};
		spriteToCollider["slide"] = getDashCollider();

		weapon = new Weapon(WeaponIds.ChillPGeneric, 93);

		awardWeaponId = WeaponIds.ShotgunIce;
		weakWeaponId = WeaponIds.FireWave;
		weakMaverickWeaponId = WeaponIds.FlameMammoth;

		netActorCreateId = NetActorCreateId.ChillPenguin;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		armorClass = ArmorClass.Light;
	}

	public override void update() {
		base.update();
		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MTaunt && input.isHeld(Control.Taunt, player)) {
				if (input.isPressed(Control.Down, player) || input.isPressed(Control.Up, player)) {
					player.removeOwnedIceStatues();
				} else if (input.isPressed(Control.Left, player) || input.isPressed(Control.Right, player)) {
					if (player.iceStatues.Count <= 1) {
						player.removeOwnedIceStatues();
					} else {
						var sortedStatues = player.iceStatues.OrderBy(ic => ic.pos.x).ToList();
						var leftStatue = sortedStatues[0];
						var rightStatue = sortedStatues[1];
						if (input.isPressed(Control.Left, player)) {
							leftStatue.destroySelf();
						} else {
							rightStatue.destroySelf();
						}
					}
				}
			}
			if (state is MIdle or MRun or MLand) {
				if (shootPressed()) {
					changeState(getShootState(false));
				} else if (specialPressed()) {
					changeState(new ChillPIceBlowState());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new ChillPSlideState());
				}
			} else if (state is MJump || state is MFall) {
				if (input.isHeld(Control.Special1, player)) {
					var hit = Global.level.checkTerrainCollisionOnce(this, 0, -ChillPBlizzardState.switchSpriteHeight - 5);
					if (vel.y < 0 && hit?.gameObject is Wall wall && !wall.topWall) {
						changeState(new ChillPBlizzardState(false));
					}
				}
			}
		}
	}

	public override string getMaverickPrefix() {
		return "chillp";
	}

	public MaverickState getShootState(bool isAI) {
		var mshoot = new MShoot((Point pos, int xDir) => {
			new ChillPIceProj(
				pos, xDir, this, player,
				input.isHeld(Control.Down, player) ? 1 : 0, player.getNextActorNetId(), rpc: true
			);
		}, null!);
		if (isAI) {
			mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.5f);
		}
		return mshoot;
	}

	public override MaverickState[] strikerStates() {
		return [
			getShootState(true),
			new ChillPIceBlowState(),
			new ChillPSlideState(),
			new ChillPBlizzardState(true)
		];
	}

	public override MaverickState[] aiAttackStates() {
		float enemyDist = 300;
		if (target != null) {
			enemyDist = MathF.Abs(target.pos.x - pos.x);
		}
		List<MaverickState> aiStates = [
			getShootState(false),
			new ChillPIceBlowState()
		];
		if (enemyDist <= 180) {
			aiStates.Add(new ChillPSlideState());
		}
		if (Helpers.randomRange(0, 2) == 0 && grounded && player.iceStatues.Count >= 1) {
			aiStates.Add(new ChillPBlizzardState(true));
		}
		return aiStates.ToArray();
	}

	/*
	public override void onHitboxHit(Collider attackHitbox, CollideData collideData)
	{
		var damagable = collideData.gameObject as IDamagable;
		if (isSlidingAndCanDamage() && damagable != null && damagable.canBeDamaged(player.alliance, player.id, null))
		{
			slideWeapon.applyDamage(damagable, false, this, (int)ProjIds.ChillPSlide);
		}
	}
	*/

	public bool isSlidingAndCanDamage() {
		return sprite.name.EndsWith("slide") && MathF.Abs(deltaPos.x) > 1.66f;
	}
	
	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Slide,
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"chillp_slide" => MeleeIds.Slide,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Slide => new GenericMeleeProj(
				slideWeapon, pos, ProjIds.ChillPSlide, player,
				3, Global.defFlinch, addToLevel: addToLevel
			),
			_ => null
		};
	}
}

#region weapons
public class ChillPIceShotWeapon : Weapon {
	public static ChillPIceShotWeapon netWeapon = new();
	public ChillPIceShotWeapon() {
		index = (int)WeaponIds.ChillPIceShot;
		killFeedIndex = 93;
	}
}

public class ChillPIceBlowWeapon : Weapon {
	public static ChillPIceBlowWeapon netWeapon = new();
	public ChillPIceBlowWeapon() {
		index = (int)WeaponIds.ChillPIceBlow;
		killFeedIndex = 93;
	}
}

public class ChillPIceStatueWeapon : Weapon {
	public static ChillPIceStatueWeapon netWeapon = new();
	public ChillPIceStatueWeapon() {
		index = (int)WeaponIds.ChillPIcePenguin;
		killFeedIndex = 93;
	}
}

public class ChillPBlizzardWeapon : Weapon {
	public static ChillPBlizzardWeapon netWeapon = new();
	public ChillPBlizzardWeapon() {
		index = (int)WeaponIds.ChillPBlizzard;
		killFeedIndex = 93;
	}
}

public class ChillPSlideWeapon : Weapon {
	public static ChillPSlideWeapon netWeapon = new();
	public ChillPSlideWeapon() {
		index = (int)WeaponIds.ChillPSlide;
		killFeedIndex = 93;
	}
}

#endregion

#region projectiles
public class ChillPIceProj : Projectile {
	public int type = 0;
	public Character? hitChar;
	public ChillPIceProj(
		Point pos, int xDir, Actor owner, Player player, int type,
		ushort netProjId, Character? hitChar = null, bool rpc = false
	) : base(
		pos, xDir, owner, "chillp_proj_ice", netProjId, player
	) {
		weapon = ChillPIceShotWeapon.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 1;
		vel = new Point(250 * xDir, 0);
		projId = (int)ProjIds.ChillPIceShot;
		maxTime = 0.75f;
		this.hitChar = hitChar;
		this.type = type;
		if (collider != null) { collider.wallOnly = true; }
		isShield = true;
		if (type == 1) {
			useGravity = true;
			vel.x *= 0.75f;
			vel.y = -50;
			maxTime = 1;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ChillPIceProj(
			args.pos, args.xDir, args.owner, 
			args.player, args.extraData[0],
			args.netId, null
		);
	}

	public override void update() {
		base.update();
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		onHit();
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (type == 1 && other.hitData.normal != null) {
			Point normal = other.hitData.normal.Value;
			if (normal.y != 0 && normal.x == 0) {
				vel.y *= -0.5f;
				return;
			}
		}
		onHit();
		destroySelf();
	}

	bool hit;
	public void onHit() {
		if (!ownedByLocalPlayer) return;
		if (hit) return;
		hit = true;
		Func<float> yVel = () => Helpers.randomRange(-150, -50);
		var pieces = new List<Anim>()
		{
				new Anim(pos.addxy(-5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
				{
					vel = new Point(-50, yVel())
				},
				new Anim(pos.addxy(-5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
				{
					vel = new Point(-100, yVel())
				},
				new Anim(pos.addxy(5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
				{
					vel = new Point(50, yVel())
				},
				new Anim(pos.addxy(5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
				{
					vel = new Point(100, yVel())
				},
			};
		foreach (var piece in pieces) {
			piece.frameSpeed = 0;
			piece.useGravity = true;
			piece.ttl = 1f;
		}

		playSound("freezebreak2", sendRpc: true);
	}
}


public class ChillPIceStatueProj : Projectile, IDamagable {
	public float health = 8;
	public float maxHealth = 8;

	public ChillPIceStatueProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "chillp_proj_statue", netId, player
	) {
		weapon = ChillPIceStatueWeapon.netWeapon;
		damager.damage = 2;
		vel = new Point(0 * xDir, 0);
		projId = (int)ProjIds.ChillPIcePenguin;
		fadeSound = "iceBreak";
		shouldShieldBlock = false;
		destroyOnHit = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ChillPIceStatueProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		if (sprite.isAnimOver()) {
			useGravity = true;
			if (collider != null) { collider.wallOnly = true; }
			damager.flinch = Global.defFlinch;
			damager.damage = 4;
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (other.gameObject is Wall && other.hitData?.normal != null &&
		other.hitData.normal.Value.isSideways() && !deltaPos.isZero() && MathF.Abs(deltaPos.x) > 0.1f
	) {
			destroySelf();
		} else if (other.gameObject is ChillPenguin cp && cp.isSlidingAndCanDamage() && cp.player == owner) {
			destroySelf();
		}

	}

	public override void onDestroy() {
		breakFreeze(owner, pos.addxy(0, -16));
		owner.iceStatues.RemoveAll(i => i == this);
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class ChillPBlizzardProj : Projectile {
	const float pushSpeed = 150;
	public ChillPBlizzardProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "chillp_wind", netId, player
	) {
		weapon = ChillPBlizzardWeapon.netWeapon;
		projId = (int)ProjIds.ChillPBlizzard;
		shouldShieldBlock = false;
		destroyOnHit = false;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new ChillPBlizzardProj(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (sprite.loopCount > 30) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.gameObject is ChillPIceStatueProj iceStatue && iceStatue.owner == owner && iceStatue.isAnimOver()) {
			iceStatue.move(new Point(pushSpeed * xDir, 0));
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!damagable.isPlayableDamagable()) { return; }
		if (damagable is not Actor actor || !actor.ownedByLocalPlayer) {
			return;
		}
		float modifier = 1;
		if (actor.grounded) { modifier = 0.5f; };
		if (damagable is Character character) {
			if (character.isPushImmune()) { return; }
			if (character.charState is Crouch) { modifier = 0.25f; }
		}
		actor.move(new Point(pushSpeed * xDir * modifier, 0));
	}
}
#endregion

#region states
public class PenguinMState : MaverickState {
	public ChillPenguin IcyPenguigo = null!;
	public PenguinMState(
		string sprite, string transitionSprite = ""
	) : base(
		sprite, transitionSprite
	) {
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		IcyPenguigo = maverick as ChillPenguin ?? throw new NullReferenceException();

	}
}
public class ChillPIceBlowState : PenguinMState {
	float shootTime;
	bool soundOnce;
	bool statueOnce;
	public ChillPIceBlowState() : base("blow") {
	}

	public override void update() {
		base.update();
		if (IcyPenguigo == null) return;

		Helpers.decrementTime(ref shootTime);

		if (shootTime == 0) {
			Point? shootPos = maverick.getFirstPOI();
			if (shootPos != null) {
				shootTime = 0.1f;
				new ShotgunIceProjCharged(
					shootPos.Value, maverick.xDir, IcyPenguigo,
					player, 1, true, player.getNextActorNetId(), rpc: true
				);
				if (!soundOnce) {
					soundOnce = true;
					maverick.playSound("icyWind", sendRpc: true);
				}
			}
		}

		if (stateTime > 0.25f && !statueOnce) {
			var iceStatuePos1 = new Point(maverick.pos.x + maverick.xDir * 35, maverick.pos.y - 5);
			var iceStatuePos2 = new Point(maverick.pos.x + maverick.xDir * 65, maverick.pos.y - 5);

			statueOnce = true;
			if (player.iceStatues.Count == 0) {
				addIceStatueIfSpace(iceStatuePos1);
				addIceStatueIfSpace(iceStatuePos2);
			} else if (player.iceStatues.Count == 1) {
				var existingIceStatue = player.iceStatues[0];
				if (existingIceStatue.pos.distanceTo(iceStatuePos1) > existingIceStatue.pos.distanceTo(iceStatuePos2)) {
					addIceStatueIfSpace(iceStatuePos1);
				} else {
					addIceStatueIfSpace(iceStatuePos2);
				}
			}
		}

		if (maverick.sprite.loopCount > 12) {
			maverick.changeState(new MIdle());
		}
	}

	public void addIceStatueIfSpace(Point pos) {
		player.iceStatues.Add(new ChillPIceStatueProj(
				pos, maverick.xDir, IcyPenguigo,
				player, player.getNextActorNetId(), rpc: true
			)
		);
		/*
		var rect = new Rect(pos.addxy(-14, -32), pos.addxy(14, 0));
		if (Global.level.checkCollisionShape(rect.getShape(), null) == null) {
			player.iceStatues.Add(
				new ChillPIceStatueProj((maverick as ChillPenguin).iceStatueWeapon,
				pos, maverick.xDir, player, player.getNextActorNetId(), sendRpc: true)
			);
		}
		*/
	}
}

public class ChillPBlizzardState : PenguinMState {
	Point? switchPos;
	int state;
	new bool isAI;
	public const float switchSpriteHeight = 60;
	public ChillPBlizzardState(bool isAI) : base("jump") {
		this.isAI = isAI;
	}

	public override bool canEnter(Maverick maverick) {
		var ceiling = Global.level.raycast(maverick.pos, maverick.pos.addxy(0, -175), new List<Type> { typeof(Wall) });
		if (ceiling?.hitData?.hitPoint != null) {
			switchPos = ceiling.hitData.hitPoint.Value;
		}

		if (switchPos == null) {
			return false;
		}

		return base.canEnter(maverick);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		if (!isAI) {
			state = 1;
		} else {
			maverick.vel.y = -maverick.getJumpPower() * 1.75f;
		}
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}

	public override void update() {
		base.update();
		if (IcyPenguigo == null) return;
		if (switchPos != null) {
			if (state == 0) {
				if (maverick.pos.y - switchSpriteHeight <= switchPos.Value.y + 5) {
					state = 1;
				}
			} else if (state == 1) {
				if (!maverick.sprite.name.Contains("switch")) {
					maverick.changeSpriteFromName("switch", true);
					maverick.changePos(new Point(maverick.pos.x, switchPos.Value.y + switchSpriteHeight));
				}
				maverick.useGravity = false;
				maverick.stopMovingS();
				if (!once && maverick.frameIndex == 3) {
					once = true;
					float topY = Global.level.getTopScreenY(maverick.pos.y);
					if (maverick.controlMode == MaverickModeId.Puppeteer && player.currentMaverick == maverick) {
						topY = maverick.pos.y - 80;
					}
					new ChillPBlizzardProj(
						new Point(maverick.pos.x, topY), maverick.xDir, IcyPenguigo,
						player, player.getNextActorNetId(), rpc: true
					);
					
					maverick.playSound("chillpBlizzard", sendRpc: true);
				}
				if (maverick.sprite.isAnimOver()) {
					maverick.changeState(new MFall());
				}
			}
		}
	}
}

public class ChillPSlideState : PenguinMState {
	public float slideTime;
	float slideSpeed = 350;
	const float timeBeforeSlow = 0.75f;
	const float slowTime = 0.5f;
	bool soundOnce;
	public ChillPSlideState() : base("slide") {
	}

	public override bool canEnter(Maverick maverick) {
		return base.canEnter(maverick);
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
	}

	public override void update() {
		base.update();
		if (player == null) return;
		if (!maverick.isAnimOver()) return;

		if (!soundOnce) {
			soundOnce = true;
			maverick.playSound("chillpSlide", sendRpc: true);
		}

		slideTime += Global.spf;

		Point moveAmount = new Point(maverick.xDir * slideSpeed, 0);
		var hitWall = checkCollisionSlide(moveAmount.x * Global.spf * 2, -2);
		if (hitWall?.isSideWallHit() == true) {
			maverick.xDir *= -1;
			moveAmount.x *= -1;
		}
		maverick.move(moveAmount);

		var inputDir = input.getInputDir(player);
		if (inputDir.x != 0 && MathF.Sign(inputDir.x) != MathF.Sign(maverick.xDir)) {
			slideTime += Global.spf;
		}

		if (input.isPressed(Control.Jump, player) && maverick.grounded && canDamageOrJump()) {
			maverick.vel.y = -maverick.getJumpPower() * 0.75f;
		}
		if (!maverick.grounded && maverick.vel.y < 0 &&
			Global.level.checkTerrainCollisionOnce(maverick, 0, maverick.vel.y * Global.spf * 2) != null
		) {
			maverick.vel.y = 0;
		}

		if (slideTime >= timeBeforeSlow) {
			float perc = 1 - ((slideTime - timeBeforeSlow) / slowTime);
			slideSpeed = 300 * perc;
			if (slideSpeed <= 1) {
				maverick.changeState(new MIdle());
			}
		}
	}

	public bool canDamageOrJump() {
		return slideTime > 0 && slideTime < (timeBeforeSlow + slowTime) * 0.75f;
	}
}

public class ChillPBurnState : PenguinMState {
	Point pushDir;
	public ChillPBurnState() : base("burn") {
		aiAttackCtrl = true;
		canBeCanceled = false;
	}

	public override void update() {
		base.update();
		if (player == null) return;

		maverick.move(pushDir);

		if (stateTime > 0.5f) {
			maverick.changeState(new MIdle());
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.playSound("chillpBurn", sendRpc: true);
		pushDir = new Point(-maverick.xDir * 75, 0);
		maverick.vel.y = -100;
	}
}

#endregion
