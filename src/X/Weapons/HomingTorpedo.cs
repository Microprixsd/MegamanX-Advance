﻿using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HomingTorpedo : Weapon {
	public static HomingTorpedo netWeapon = new();

	public HomingTorpedo() : base() {
		displayName = "Homing Torpedo";
		index = (int)WeaponIds.HomingTorpedo;
		killFeedIndex = 1;
		weaponBarBaseIndex = 1;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 1;
		weaknessIndex = (int)WeaponIds.RollingShield;
		shootSounds = ["torpedo", "torpedo", "torpedo", "buster3"];
		fireRate = 30;
		switchCooldown = 30;
		damage = "2/1*6";
		effect = "Both:Destroys on contact with projectiles or enemies.\nBesides of its homing capabilities.";
		hitcooldown = "0";
		flinch = "0/13";

		ammoDisplayScale = 1;
		maxAmmo = 16;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3 && ammo >=6) { return 6; }
		return 1;
	}
	public override void update() {
		base.update();
		if (ammo < maxAmmo) {
			rechargeAmmo(2);
		}
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;


		if (chargeLevel < 3 || chargeLevel >= 3 && ammo < 6) {
			new TorpedoProjX(pos, xDir, mmx, player, player.getNextActorNetId(true), rpc: true);
		} else {
			if (ammo >= 6) {
				player.getNextActorNetId(true);
				new TorpedoProjChargedX(pos.addxy(0, 2), xDir, mmx, player, player.getNextActorNetId(true), 30, new Point(25, -100), true);
				new TorpedoProjChargedX(pos.addxy(0, 1), xDir, mmx, player, player.getNextActorNetId(true), 15, new Point(175 * xDir, -75), true);
				new TorpedoProjChargedX(pos.addxy(0, 0), xDir, mmx, player, player.getNextActorNetId(true), 0, new Point(200 * xDir, 0), true);
				new TorpedoProjChargedX(pos.addxy(0, -1), xDir, mmx, player, player.getNextActorNetId(true), -15, new Point(175 * xDir, 75), true);
				new TorpedoProjChargedX(pos.addxy(0, -2), xDir, mmx, player, player.getNextActorNetId(true), -30, new Point(25, 100), true);
			}
		}
	}
} 
public class TorpedoProjX : Projectile, IDamagable {
	public Actor? target;
	public float smokeTime = 0;
	public float maxSpeed = 200;
	public Point? oldAmount;
	public TorpedoProjX(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, float? angle = null, Point? mov = null, bool rpc = false
	) : base(
		pos, xDir, owner, "torpedo", netId, player
	) {
		weapon = HomingTorpedo.netWeapon;
		netcodeOverride = NetcodeModel.FavorDefender;
		damager.damage = 2;
		vel = new Point(150 * xDir, 0);
		fadeSprite = "explosion";
		fadeSound = "explosion";
		maxDistance = 260;
		projId = (int)ProjIds.Torpedo;
		fadeOnAutoDestroy = true;
		reflectableFBurner = true;
		customAngleRendering = true;
		this.angle = this.xDir == -1 ? 180 : 0;
		if (angle != null) {
			this.angle = angle.Value + (this.xDir == -1 ? 180 : 0);
		}
		if (mov != null) vel = (Point)mov;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TorpedoProjX(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
	bool homing = true;
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
		if (ownedByLocalPlayer && homing) {
			if (target != null) {
				if (!Global.level.gameObjects.Contains(target)) {
					target = null;
				}
			}
			if (target != null) {
				var dTo = pos.directionTo(target.getCenterPos()).normalize();
				var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
				destAngle = Helpers.to360(destAngle);
				angle = Helpers.lerpAngle((float)angle, destAngle, 0.08f);
			}
			if (time >= 0.15) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, false, aMaxDist: Global.screenW * 0.75f);
			}
			
			if (target != null) {
				Point amount = pos.directionToNorm(target.getCenterPos()).times(180);
				oldAmount = amount;
				vel = Point.lerp(vel, amount, 0.06f);
			} else if (oldAmount != null) {
				vel = Point.lerp(vel, (Point)oldAmount, 0.06f);
			}
			if (vel.magnitude > maxSpeed) {
				vel = vel.normalize().times(maxSpeed);
			} else {
				vel += vel * 0.02f;
			}
		} 
		smokeTime += Global.spf;
		if (smokeTime > 0.1) {
			Point oldPos = pos;
			smokeTime = 0;
			if (homing) {
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					new Anim(oldPos, "torpedo_smoke", 1, null, true);}
				, 2f / 60f ));
			}
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
public class TorpedoProjChargedX : Projectile, IDamagable {
	public Actor? target;
	public float smokeTime = 0;
	public float maxSpeed = 350;
	public Point? oldAmount;
	public TorpedoProjChargedX(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, float? angle = null, Point? mov = null, bool rpc = false
	) : base(
		pos, xDir, owner, "torpedo_charge", netId, player
	) {
		weapon = HomingTorpedo.netWeapon;
		netcodeOverride = NetcodeModel.FavorDefender;
		damager.damage = 1;
		damager.flinch = Global.halfFlinch;
		//vel = new Point(150 * xDir, 0);
		fadeSprite = "explosion";
		fadeSound = "explosion";
		maxDistance = 300;
		projId = (int)ProjIds.TorpedoCharged;
		fadeOnAutoDestroy = true;
		reflectableFBurner = true;
		customAngleRendering = true;
		this.angle = this.xDir == -1 ? 180 : 0;
		if (angle != null) {
			this.angle = angle.Value + (this.xDir == -1 ? 180 : 0);
		}
		if (mov != null) vel = (Point)mov;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		canBeLocal = false;
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new TorpedoProjChargedX(
			args.pos, args.xDir, args.owner, args.player, args.netId
		);
	}
	bool homing = true;
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
		if (ownedByLocalPlayer && homing) {
			if (target != null) {
				if (!Global.level.gameObjects.Contains(target)) {
					target = null;
				}
			}
			if (target != null) {
				var dTo = pos.directionTo(target.getCenterPos()).normalize();
				var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
				destAngle = Helpers.to360(destAngle);
				angle = Helpers.lerpAngle((float)angle, destAngle, 0.08f);
			}
			if (time >= 0.3) {
				target = Global.level.getClosestTarget(pos, damager.owner.alliance, false, aMaxDist: Global.screenW * 0.75f);
			}
			if (target != null) {
				Point amount = pos.directionToNorm(target.getCenterPos()).times(180);
				oldAmount = amount;
				vel = Point.lerp(vel, amount, 0.06f);
			} else if (oldAmount != null) {
				vel = Point.lerp(vel, (Point)oldAmount, 0.06f);
			}
			if (vel.magnitude > maxSpeed) {
				vel = vel.normalize().times(maxSpeed);
			} else {
				vel += vel * 0.02f;
			}
		}
		smokeTime += Global.spf;
		if (smokeTime > 0.1) {
			Point oldPos = pos;
			smokeTime = 0;
			if (homing) {
				Global.level.delayedActions.Add(new DelayedAction(delegate {
					new Anim(oldPos, "torpedo_smoke", 1, null, true);}
				, 2f / 60f ));
			}
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
			//yDir = -1;
			normAngle = angle;
		}
		if (angle >= 90 && angle < 180) {
			xDir = -1;
			//yDir = -1;
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
