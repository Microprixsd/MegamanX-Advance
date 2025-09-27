﻿using System;

namespace MMXOnline;

public enum VileMechMenuType {
	None = -1,
	All,
}

public class MechMenuWeapon : Weapon {
	public bool isMenuOpened;
	public const int weight = 16;
	public MechMenuWeapon(VileMechMenuType type) : base() {
		ammo = 0;
		index = (int)WeaponIds.MechMenuWeapon;
		weaponSlotIndex = 46;
		this.type = (int)type;
		drawAmmo = false;
		drawCooldown = false;

		if (type == VileMechMenuType.None) {
			displayName = "None";
			description = new string[] { "Do not equip Ride Armors." };
			killFeedIndex = 126;
		} else if (type == VileMechMenuType.All) {
			displayName = "All";
			description = new string[] { "Vile has all 4 Ride Armors available", "to call down on the battlefield." };
			killFeedIndex = 178;
			vileWeight = weight;
		}
		/*
		else if (type == VileMechMenuType.N)
		{
			displayName = "N";
			description = new string[] { "Vile can call down the", "Neutral Ride Armor." };
			killFeedIndex = 0;
		}
		else if (type == VileMechMenuType.K)
		{
			displayName = "K";
			description = new string[] { "Vile can call down the", "Kangaroo Ride Armor." };
			killFeedIndex = 0;
		}
		else if (type == VileMechMenuType.H)
		{
			displayName = "H";
			description = new string[] { "Vile can call down the", "Hawk Ride Armor." };
			killFeedIndex = 0;
		}
		else if (type == VileMechMenuType.F)
		{
			displayName = "F";
			description = new string[] { "Vile can call down the", "Frog Ride Armor." };
			killFeedIndex = 0;
		}
		*/
	}
}

public class MechPunchWeapon : Weapon {
	public MechPunchWeapon() : base() {
		index = (int)WeaponIds.MechPunch;
		killFeedIndex = 18;
	}
}

public class MechKangarooPunchWeapon : Weapon {
	public MechKangarooPunchWeapon() : base() {
		index = (int)WeaponIds.MechKangarooPunch;
		killFeedIndex = 49;
	}
}

public class MechGoliathPunchWeapon : Weapon {
	public MechGoliathPunchWeapon() : base() {
		index = (int)WeaponIds.MechGoliathPunch;
		killFeedIndex = 57;
	}
}

public class MechDevilBearPunchWeapon : Weapon {
	public MechDevilBearPunchWeapon() : base() {
		index = (int)WeaponIds.MechDevilBearPunch;
		killFeedIndex = 176;
	}
}

public class MechStompWeapon : Weapon {
	public MechStompWeapon() : base() {
		index = (int)WeaponIds.MechStomp;
		killFeedIndex = 19;
	}
}

public class MechKangarooStompWeapon : Weapon {
	public MechKangarooStompWeapon() : base() {
		index = (int)WeaponIds.MechKangarooStomp;
		killFeedIndex = 58;
	}
}

public class MechFrogStompWeapon : Weapon {
	public MechFrogStompWeapon() : base() {
		index = (int)WeaponIds.MechFrogStomp;
		killFeedIndex = 51;
	}
}

public class MechFrogStompShockwave : Projectile {
	public MechFrogStompShockwave(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 0, player, "groundpound_explosion", 0, 1f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.75f;
		projId = (int)ProjIds.MechFrogStompShockwave;
		yScale = 0.5f;
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void onStart() {
		base.onStart();
		shakeCamera();
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf(disableRpc: true);
		}
	}
}

public class MechHawkStompWeapon : Weapon {
	public MechHawkStompWeapon() : base() {
		index = (int)WeaponIds.MechHawkStomp;
		killFeedIndex = 59;
	}
}

public class MechGoliathStompWeapon : Weapon {
	public MechGoliathStompWeapon() : base() {
		index = (int)WeaponIds.MechGoliathStomp;
		killFeedIndex = 60;
	}
}

public class MechDevilBearStompWeapon : Weapon {
	public MechDevilBearStompWeapon() : base() {
		index = (int)WeaponIds.MechDevilBearStomp;
		killFeedIndex = 177;
	}
}

public class MechChainChargeWeapon : Weapon {
	public MechChainChargeWeapon() : base() {
		index = (int)WeaponIds.MechChainCharge;
		killFeedIndex = 49;
	}
}

public class MechChainWeapon : Weapon {
	public MechChainWeapon() : base() {
		index = (int)WeaponIds.MechChain;
		killFeedIndex = 49;
	}
}

public class MechMissileWeapon : Weapon {
	public MechMissileWeapon() : base() {
		index = (int)WeaponIds.MechMissile;
		killFeedIndex = 50;
	}
}

public class MechMissileProj : Projectile, IDamagable {
	public Character? target;
	public float smokeTime = 0;
	public bool isDown;
	public MechMissileProj(Weapon weapon, Point pos, int xDir, bool isDown, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 400, 2, player, "hawk_missile", 0, 0f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.MechMissile;
		maxTime = 0.5f;
		fadeOnAutoDestroy = true;
		fadeSprite = "explosion";
		fadeSound = "explosion";
		reflectableFBurner = true;
		this.isDown = isDown;
		if (isDown) {
			this.xDir = 1;
			angle = 90;
			vel.x = 0;
			vel.y = 400;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		smokeTime += Global.spf;
		if (smokeTime > 0.2) {
			smokeTime = 0;
			new Anim(pos, "torpedo_smoke", 1, null, true);
		}
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
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

public class MechTorpedoWeapon : Weapon {
	public MechTorpedoWeapon() : base() {
		index = (int)WeaponIds.MechTorpedo;
		weaponBarBaseIndex = 52;
		weaponBarIndex = 52;
		killFeedIndex = 52;
	}
}

public class MechChainProj : Projectile {
	public MechChainProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 3, player, "kangaroo_chain_proj", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.MechChain;

		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;
	}
	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
	}
}

public class MechBusterWeapon : Weapon {
	public MechBusterWeapon() : base() {
		index = (int)WeaponIds.MechBuster;
		weaponBarBaseIndex = 53;
		weaponBarIndex = 53;
		killFeedIndex = 53;
	}
}

public class MechBusterProj : Projectile {
	public MechBusterProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 200, 4, player, "goliath_proj", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.MechBuster;
		maxTime = 0.75f;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}
}

public class MechBusterProj2 : Projectile {
	int type = 0;
	float startY;
	public MechBusterProj2(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 200, 4, player, "goliath_proj2", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.75f;
		projId = (int)ProjIds.MechBuster;
		startY = pos.y;
		this.type = type;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		float offsetY;
		if (type == 0) {
			offsetY = 25 * MathF.Sin(Global.time * 10);
		} else {
			offsetY = 25 * MathF.Sin(MathF.PI + Global.time * 10);
		}
		changePos(new Point(pos.x, startY + offsetY));
	}
}
public class TorpedoProjMech : Projectile, IDamagable {
	public Actor? target;
	public float smokeTime = 0;
	public float maxSpeed = 150;
	public TorpedoProjMech(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, float? angle = null, bool rpc = false
	) : base(
		pos, xDir, owner, "frog_torpedo", netId, player	
	) {
		weapon = RideArmor.netWeapon;
		damager.damage = 2;
		vel = new Point(1 * xDir, 0);
		fadeSprite = "explosion";
		fadeSound = "explosion";
		maxTime = 2f;
		projId = (int)ProjIds.MechTorpedo;
		fadeOnAutoDestroy = true;
		reflectableFBurner = true;
		customAngleRendering = true;
		this.angle = this.xDir == -1 ? 180 : 0;
		if (angle != null) {
			this.angle = angle.Value + (this.xDir == -1 ? 180 : 0);
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TorpedoProjMech(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
	bool homing = true;
	public void checkLandFrogTorpedo() {
		if (!isUnderwater()) {
			useGravity = true;
			maxTime = 1f;
			homing = false;
		} else {
			useGravity = true;
			homing = true;
		}
	}

	public void reflect(float reflectAngle) {
		angle = reflectAngle;
		target = null;
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		checkLandFrogTorpedo();
		if (ownedByLocalPlayer && homing) {
			if (target != null) {
				if (!Global.level.gameObjects.Contains(target)) {
					target = null;
				}
			}
			if (target != null) {
				if (time < 3f) {
					var dTo = pos.directionTo(target.getCenterPos()).normalize();
					var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
					destAngle = Helpers.to360(destAngle);
					angle = Helpers.lerpAngle(angle, destAngle, Global.spf * 3);
				}
			}
			if (time >= 0.15) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, true, aMaxDist: Global.screenW * 0.75f);
			} else if (time < 0.15) {
				//this.vel.x += this.xDir * Global.spf * 300;
			}
			vel.x = Helpers.cosd(angle) * maxSpeed;
			vel.y = Helpers.sind(angle) * maxSpeed;
		}
		smokeTime += Global.spf;
		if (smokeTime > 0.2) {
			smokeTime = 0;
			if (homing) new Anim(pos, "torpedo_smoke", 1, null, true);
		}
	}
	public override void renderFromAngle(float x, float y) {
		var angle = this.angle;
		var xDir = 1;
		var yDir = 1;
		var frameIndex = 0;
		float normAngle = 0;
		if (angle < 90) {
			xDir = 1;
			yDir = -1;
			normAngle = angle;
		}
		if (angle >= 90 && angle < 180) {
			xDir = -1;
			yDir = -1;
			normAngle = 180 - angle;
		} else if (angle >= 180 && angle < 270) {
			xDir = -1;
			yDir = 1;
			normAngle = angle - 180;
		} else if (angle >= 270 && angle < 360) {
			xDir = 1;
			yDir = 1;
			normAngle = 360 - angle;
		}

		if (normAngle < 18) frameIndex = 0;
		else if (normAngle >= 18 && normAngle < 36) frameIndex = 1;
		else if (normAngle >= 36 && normAngle < 54) frameIndex = 2;
		else if (normAngle >= 54 && normAngle < 72) frameIndex = 3;
		else if (normAngle >= 72 && normAngle < 90) frameIndex = 4;

		sprite.draw(frameIndex, pos.x + x, pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex, actor: this);
	}
	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
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
