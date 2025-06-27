using System;
using System.Collections.Generic;

namespace MMXOnline;

public class CrescentShot : Weapon {
	public static CrescentShot netWeapon = new();

	public CrescentShot() : base() {
		// Icons and ID.
		displayName = "Crescent Shot";
		weaponBarBaseIndex = 51;
		weaponSlotIndex = 51;
		weaponBarIndex = weaponBarBaseIndex;
		index = (int)WeaponIds.CrescentShot;
		//weaknessIndex = 0;
		killFeedIndex = 21;
		// Sounds that play when you shoot the weapon.
		shootSounds = ["boomerang", "boomerang", "boomerang", "boomerang"];
		// Frames between shots.
		fireRate = 9;
		maxAmmo = 20;
		ammo = maxAmmo;
		effect = "Test weapon, YO!";
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) {
			return 4;
		}
		return 0.5f;
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel >= 3) {
			// Insert changed weapon projectile here.
		} else {
			float angle = Helpers.randomRange(-6, 6);
			new CrescentShotProj(pos, xDir, angle, mmx, player, player.getNextActorNetId(), sendRpc: true);
		}
	}
}

public class CrescentShotProj : Projectile {
	// Local variable.
	// We save the original Vel Y here for later use.
	public float ogVelY;

	public CrescentShotProj(
		Point pos, int xDir, float projAngle, Actor owner, Player player, ushort? netId, bool sendRpc = false
	) : base(
		pos, xDir, owner, "buster2", netId, player	
	) {
		// Determines the killfeed icon.
		weapon = CrescentShot.netWeapon;
		damager.damage = 1;

		// Create vel from projAngle.
		vel = Point.createFromByteAngle(projAngle) * 350;
		// Multiply XVel by XDir fo it faces the correct direction.
		vel.x *= xDir;
		// Multiply projAngly by XDir so it also is offset corrently if backwards.
		projAngle *= xDir;
		// Apply projAngle to byteAngle (the display one).
		byteAngle = projAngle;

		// Save the VelY for later use.
		ogVelY = vel.y;

		// Other stuff.
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster2;
		fadeOnAutoDestroy = true;
		destroyOnHitWall = true;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, [(byte)this.byteAngle]);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new CrescentShotProj(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		// Apply the original vel Y for a smooth curve.
		vel.y += vel.y * 0.08f;
		// Update the angle with the new speed.
		byteAngle = (vel * xDir).byteAngle;
	}
}
