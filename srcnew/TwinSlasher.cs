using System;
using System.Collections.Generic;

namespace MMXOnline;

public class TwinSlasher : Weapon {

	public static TwinSlasher netWeapon = new TwinSlasher();

	public TwinSlasher() {
		index = (int)WeaponIds.TwinSlasher;
		displayName = "Twin Slasher";
		killFeedIndex = 182;
		weaponBarIndex = 68;
		weaponBarBaseIndex = 79;
		weaponSlotIndex = 130;
		weaknessIndex = (int)WeaponIds.LightningWeb;
		shootSounds = new string[] { "buster2X4", "buster2X4", "buster2X4", "twinSlasherCharged" };
		fireRate = 15;
		switchCooldown = 30;
		/* damage = "1";
		hitcooldown = "0.5";
		Flinch = "0/26";
		FlinchCD = hitcooldown;
		effect = "Pierces enemies."; */

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
			//player.setNextActorNetId(player.getNextActorNetId());
			new TwinSlasherProj(this, pos, xDir, 0, player, player.getNextActorNetId(), true);
			new TwinSlasherProj(this, pos, xDir, 1, player, player.getNextActorNetId(), true);
		} else {
			if (ammo >= 6) {
				for (int i = 0; i < 9; i++) {
					if (i != 4) {
						var tsc = new TwinSlasherProjCharged(
							this, pos, xDir, i, player, i, player.getNextActorNetId(), true) {
						};
					}
				}
			}
		}
	}
}


public class TwinSlasherProj : Projectile {
	public int numHits = 0;
	
	private int maxHits = 2;
	private bool changedSprite;

	public TwinSlasherProj(
		Weapon weapon, Point pos, int xDir, int type, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 400f, 1, player, "twin_slasher_proj", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.35f;
		projId = (int)ProjIds.TwinSlasher;
		destroyOnHit = false;
		reflectable = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		vel.y = type == 0 ? -100 : 100;
		yDir = type == 0 ? 1 : -1;
		/*fadeSprite = "twin_slasher_trail";
		fadeOnAutoDestroy = true;*/
		
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}

		if (type == 1) projId = (int)ProjIds.TwinSlasher2;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TwinSlasherProj(
			TwinSlasher.netWeapon, arg.pos, arg.xDir,
			arg.extraData[0], arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (time >= maxTime / 2 && !changedSprite) {
			changeSprite("twin_slasher_charged_proj", false);
			changedSprite = true;
		} 
		if (sprite.name is "twin_slasher_trail" && sprite.isAnimOver()) {
			destroySelfNoEffect();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		damager.damage = 0;
		changeSprite("twin_slasher_trail", true);
		vel *= 0.5f;
		changedSprite = true;
		if (!ownedByLocalPlayer) {
			return;
		}
		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) && 
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				numHits++;
			}
			if (numHits >= maxHits) {
				destroySelf();
			}
		}
	}
}


public class TwinSlasherProjCharged : Projectile {

	public int numHits = 0;
	private int maxHits = 3;
	string trailName = "";
	float animCooldown;
	float ang;
	float ogAng;

	public TwinSlasherProjCharged(
		Weapon weapon, Point pos, int xDir, int type, 
		Player player, int id, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 350, 0.5f, player, "twin_slasher_charged_proj2", 
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.40f;
		projId = (int)ProjIds.TwinSlasherCharged;
		yDir *= 1;
		destroyOnHit = false;
		reflectable = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;

		ang = -48 + (type * 12);

		trailName = MathF.Abs(ang) <= 24 ? "twin_slasher_trail" : "twin_slasher_trail2";
		if (Math.Abs(ang) <= 24) changeSprite("twin_slasher_charged_proj", false);
		yDir = ang > 0 ? -1 : 1;

		ogAng = ang;
		if (xDir < 0) ang = -ang + 128;

		base.vel = Point.createFromByteAngle(ang) * speed;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type, (byte)id };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	
		projId = (int)ProjIds.TwinSlasherCharged + id;
	}	

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TwinSlasherProjCharged(
			TwinSlasher.netWeapon, arg.pos, arg.xDir,
			arg.extraData[0], arg.player, arg.extraData[1], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref animCooldown);

		if (animCooldown <= 0) {
			Anim trail = new Anim(pos, trailName, xDir, damager.owner.getNextActorNetId(), true, true);
			animCooldown = 4;
			trail.yDir = yDir;
		}
	}
	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) {
			return;
		}
		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) && 
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				numHits++;
			}
			if (numHits >= maxHits) {
				destroySelf();
			}
		}
	}
}
