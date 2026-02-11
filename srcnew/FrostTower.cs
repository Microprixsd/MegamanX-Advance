using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FrostTower : Weapon {

	public static FrostTower netWeapon = new FrostTower();
	public FrostTower() {
		shootSounds = new string[] { "frostTower", "frostTower", "frostTower", "" };
		displayName = "Frost Tower";
		fireRate = 120;
		switchCooldown = 30;
		index = (int)WeaponIds.FrostTower;
		weaponBarIndex = 62;
		weaponBarBaseIndex = 73;
		weaponSlotIndex = 124;
		killFeedIndex = 184;
		weaknessIndex = (int)WeaponIds.RisingFire;
		hasCustomAnim = true;
		/* damage = "1-2/3";
		hitcooldown = "0.75/0.5";
		Flinch = "0-13/26";
		FlinchCD = hitcooldown;
		effect = "Blocks projectiles. C: Summons huge icicles that drop from above."; */

		ammoDisplayScale = 1;
		maxAmmo = 16;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3 && ammo >=6) { return 6; }
		return 3;
	}
	public override void update() {
		base.update();
		if (ammo < maxAmmo) {
			rechargeAmmo(2);
		}
	}
	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];

		if (chargeLevel < 3 || chargeLevel >= 3 && ammo < 6) {
			character.changeState(new FrostTowerState(), true);
		} else {
			if (chargeLevel >= 3 && ammo >= 6)
			character.changeState(new FrostTowerChargedState(), true);
		}
		rechargeCooldown = 1;
	}
}

public class FrostTowerState : CharState {

	bool fired;
	public FrostTowerState() : base("summon") {
		attackCtrl = false;
		normalCtrl = false;
		useGravity = false;
		useDashJumpSpeed = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
	}

	public override void update() {
		base.update();

		if (!fired && character.frameIndex == 1) {
			Point shootPos = character.getCenterPos();
			int xDir = character.getShootXDir();
			Player player = character.player;

			new FrostTowerProj(character, shootPos, xDir, player.getNextActorNetId(), true, player);
			fired = true;
		}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}

public class FrostTowerProj : Projectile, IDamagable {
	public float health = 5;
	public float maxHealth = 5;
	public bool landed;
	float zTime;

	public FrostTowerProj(
		Actor owner, Point pos, int xDir, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "frosttower_proj", netId, player
	) {
		weapon = FrostTower.netWeapon;
		maxTime = 3f;
		projId = (int)ProjIds.FrostTower;
		grounded = false;
		canBeGrounded = true;
		isShield = true;
		destroyOnHit = false;
		shouldShieldBlock = false;

		damager.damage = 1;
		damager.hitCooldown = 30;

		if (collider != null) {
			collider.isClimbable = true;
		}
		
		zIndex = ZIndex.MainPlayer + 10;
		
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostTowerProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();
		if (sprite.frameIndex >= 3) useGravity = true;
		updateProjectileCooldown();

		if (landed && ownedByLocalPlayer) moveWithMovingPlatform();

		if (!grounded && MathF.Abs(vel.y) > 60) updateDamager(Helpers.clamp(MathF.Floor(deltaPos.y * 0.6f), 1, 4), Global.halfFlinch);
		else updateDamager(1, 0);

		zIndex = zTime % 2 == 0 ? ZIndex.MainPlayer + 10 : ZIndex.Character - 10;
		zTime += Global.speedMul;
		
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		bool isRisingFire =
			projId == (int)ProjIds.RisingFire ||
			projId == (int)ProjIds.RisingFireCharged ||
			projId == (int)ProjIds.RisingFireChargedStart ||
			projId == (int)ProjIds.RisingFireUnderwater ||
			projId == (int)ProjIds.RisingFireUnderwaterCharged;

		if (health <= 0 || isRisingFire) destroySelf();
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return base.owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
	}
	public bool isPlayableDamagable() {
		return false;
	}
}



public class FrostTowerChargedState : CharState {

	bool fired;
	int lap = 1;
	float cooldown = 15;
	Point spawnPos;
	float extraPos;
	float p = 48;

	public FrostTowerChargedState() : base("summon") {
		normalCtrl = false;
		attackCtrl = false;
		useDashJumpSpeed = true;
		useGravity = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		spawnPos = character.getCenterPos().addxy(0, -96);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		mmx.shootCooldown = 60;
	}

	public override void update() {
		base.update();

	
		int xDir = character.getShootXDir();

		if (character.frameIndex >= 1) {
			Helpers.decrementFrames(ref cooldown);
		}

		if (cooldown <= 0) {
			if (lap > 4) character.changeToIdleOrFall();
			else shoot(spawnPos, xDir, lap);
		} 
	}

	void shoot(Point pos, int xDir, int l) {
		for (int i = 0; i < l; i++) {
			float extra = extraPos + (i * p * 2);
			new FrostTowerProjCharged(character, pos.addxy(extra, 0), xDir, player.getNextActorNetId(), true, player); 
			//{ canReleasePlasma = player.hasPlasma() && l == 1 };
			character.playSound("frostTower", sendRpc: true);
		}
		character.shakeCamera(true);
		cooldown = 45;
		extraPos -= p;
		lap++;
	} 
}

public class FrostTowerProjCharged : Projectile, IDamagable {
	public float health = 4;
	public float maxHealth = 4;

	public bool canReleasePlasma;

	public FrostTowerProjCharged(
		Actor owner, Point pos, int xDir, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "frosttowercharged_proj", netId, player
	) {
		weapon = FrostTower.netWeapon;
		maxTime = 2f;
		projId = (int)ProjIds.FrostTowerCharged;
		isShield = false;

		damager.damage = 2;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostTowerProjCharged(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();
		if (isAnimOver()) useGravity = true;
		
	}
	
	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		bool isRisingFire =
			projId == (int)ProjIds.RisingFire ||
			projId == (int)ProjIds.RisingFireCharged ||
			projId == (int)ProjIds.RisingFireChargedStart ||
			projId == (int)ProjIds.RisingFireUnderwater ||
			projId == (int)ProjIds.RisingFireUnderwaterCharged;
			
		if (health <= 0 || isRisingFire) destroySelf();
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return base.owner.alliance != damagerAlliance;
	}
	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}
	public bool canBeHealed(int healerAlliance) {
		return false;
	}
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {}


	/*public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		/* if (other.isCeilingHit()) return;
		else if (other.isSideWallHit()) return;
		else if (other.isGroundHit()) destroySelf(); 
	}*/
	public bool isPlayableDamagable() {
		return false;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		/*if (canReleasePlasma && !hasReleasedPlasma) {
			new BusterForcePlasmaHit(
				6, weapon, pos, xDir, damager.owner,
				damager.owner.getNextActorNetId(), true
			);
			hasReleasedPlasma = true;
		}*/
	}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
		/*if (canReleasePlasma && !hasReleasedPlasma) {
			new BusterForcePlasmaHit(
				6, weapon, pos, xDir, damager.owner,
				damager.owner.getNextActorNetId(), true
			);
			hasReleasedPlasma = true;
		}*/
	}
}
