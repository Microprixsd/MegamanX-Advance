using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RisingFire : Weapon {

	public static RisingFire netWeapon = new RisingFire();

	public RisingFire() {
		shootSounds = new string[] { "ryuenjin", "ryuenjin", "ryuenjin", "ryuenjin" };
		displayName = "Rising Fire";
		fireRate = 60;
		switchCooldown = 30;
		index = (int)WeaponIds.RisingFire;
		weaponBarIndex = 64;
		weaponBarBaseIndex = 75;
		weaponSlotIndex = 126;
		killFeedIndex = 183;
		weaknessIndex = (int)WeaponIds.DoubleCyclone;
		hasCustomAnim = true;
		/* damage = "2+1-1/2+1-1";
		hitcooldown = "0.5";
		Flinch = "0/13-26";
		FlinchCD = hitcooldown;
		effect = "Burns upper enemies. C: Resets airdashes count."; */

		ammoDisplayScale = 1;
		maxAmmo = 16;
		ammo = maxAmmo;
	}
	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3 && ammo >=6) { return 6; }
		return 2;
	}
	public override void update() {
		base.update();
		if (ammo < maxAmmo) {
			rechargeAmmo(2);
		}
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];

		if (chargeLevel < 3f || chargeLevel >= 3 && ammo <6) {
			character.changeState(new RisingFireState(), true);
		} else {
			if (ammo >= 6) {
				character.changeState(new RisingFireChargedState(), true);
			}
		}
	}
}


public class RisingFireState : CharState {
	private bool fired;

	public RisingFireState()
		: base("risingfire")
	{
		superArmor = false;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		
		if (character.currentFrame.getBusterOffset() != null && !fired) {
			Point shootPos = character.getFirstPOI() ?? character.getShootPos();
			int xDir = character.getShootXDir();
			Player player = character.player;

			if (!character.isUnderwater()) {
				new RisingFireProj(new RisingFire(), shootPos, xDir, player, player.getNextActorNetId(), true);
			} else {
				new RisingFireWaterProj(new RisingFire(), shootPos, xDir, player, player.getNextActorNetId(), true);
			}
			
			fired = true;
		}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel = new Point();
		character.useGravity = false;
		
		bool air = !character.grounded || character.vel.y < 0;
		defaultSprite = sprite;
		landSprite = "risingfire";
		if (air) {
			sprite = "risingfire_air";
			defaultSprite = sprite;
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class RisingFireProj : Projectile {
	public RisingFireProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "risingfire_proj", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFire;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		destroyOnHit = false;
		vel.y = -275;
		
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireProj(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (isUnderwater()) destroySelf();
	}
}

public class RisingFireChargedState : CharState {
	private bool jumpedYet;
	private bool fired = false;

	private float timeInWall;

	private Projectile? proj;

    public RisingFireChargedState() : base("risingfire_charged") {
		superArmor = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();
		int xDir = character.xDir;
		Point pos = character.pos;
		Player player = character.player;

		if (character.sprite.frameIndex >= 3 && !jumpedYet) {
			jumpedYet = true;
			character.vel.y = -character.getJumpPower();
			character.useGravity = true;
		} 
		
		if (character.vel.y < 0) character.move(new Point(character.xDir * 165, 0f));

		if (character.currentFrame.getBusterOffset() != null) {
			Point poi = character.currentFrame.POIs[0];
			Point firePos = character.pos.addxy(poi.x * (float)character.xDir, poi.y);

			if (proj == null) {
				if (!character.isUnderwater()){
					proj = new RisingFireProjChargedStart(
						new RisingFire(), pos, xDir, player, player.getNextActorNetId(), true
					);
				} else {
					proj = new RisingFireProjChargedStart(
						new RisingFire(), pos, xDir, player, player.getNextActorNetId(), true
					);
				}
			}
			else proj.changePos(firePos);
		}

		else if (character.sprite.frameIndex == 3 && proj != null) {
			proj.destroySelf();
			proj = null!;
		}
		
		CollideData? wallAbove = Global.level.checkTerrainCollisionOnce(character, 0, -10);
		
		if (wallAbove != null && wallAbove.gameObject is Wall) {
			timeInWall++;
			if (timeInWall > 6) {
				character.vel.y = 1;
				character.changeToIdleOrFall();
				return;
			}
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
		if (character.frameIndex > 3 && !fired) {
			fired = true;
			releaseProj();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.dashedInAir = 0;
		character.stopMoving();
		character.useGravity = false;
		if (!character.grounded) {
			character.frameIndex = 2;
			character.frameTime = 2;
		}
	}
	

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (proj != null) {
			proj.destroySelf();
			if (!fired) releaseProj();
		} 
	}

	void releaseProj() {
		Projectile? rf;
		Point shootPos = character.getShootPos();
		int xDir = character.xDir;

		if (!character.isUnderwater()) {
			rf = new RisingFireProjCharged(
				new RisingFire(), shootPos, xDir, player, 
				player.getNextActorNetId(), rpc: true
			);
		} else {
			rf = new RisingFireWaterProjCharged(
				new RisingFire(), shootPos, xDir, player, 
				player.getNextActorNetId(), rpc: true
			);
		}
		
	}
}

public class RisingFireProjChargedStart : Projectile {
	public RisingFireProjChargedStart(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0f, 2f, player, "risingfire_proj_charged",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireChargedStart;
		shouldShieldBlock = false;
		destroyOnHit = false;
		shouldVortexSuck = false;
		canBeLocal = false;
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireProjChargedStart(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (isUnderwater()) destroySelf();
	}
}


public class RisingFireProjCharged : Projectile {
	public RisingFireProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "risingfire_proj_charged", 
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireCharged;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		vel.y = -275;
		if (isUnderwater()) destroySelf();

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireProjCharged(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (isUnderwater()) destroySelf();
	}
}

public class RisingFireWaterProj : Projectile {
	public RisingFireWaterProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "risingfire_proj_water", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireUnderwater;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		vel.y = -275;
		if (!isUnderwater()) destroySelf();
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}
	
	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireWaterProj(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!isUnderwater()) destroySelf();
	}
}

public class RisingFireWaterProjCharged : Projectile {
	public RisingFireWaterProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "risingfire_proj_water", 
		Global.halfFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireUnderwaterCharged;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		vel.y = -275;
		if (!isUnderwater()) destroySelf();
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireWaterProjCharged(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!isUnderwater()) destroySelf();
	}
}
