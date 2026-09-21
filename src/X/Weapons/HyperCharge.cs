using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HyperCharge : Weapon {
	public bool active;
	public const float ammoUsage = 8;

	public HyperCharge() : base() {
		index = (int)WeaponIds.HyperCharge;
		killFeedIndex = 48;
		weaponBarBaseIndex = 32;
		weaponBarIndex = 31;
		weaponSlotIndex = 36;
		//shootSounds = new string[] { "buster3X3", "buster3X3", "buster3X3", "buster3X3" };
		fireRate = 120;
		switchCooldown = 15;
		ammo = 0;
		maxAmmo = 16;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		allowSmallBar = false;
		useForceHelmetBuff = false;
	}

	public override void update() {
		base.update();
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) {
			return 0;
		}
		return 7;
	}

	public float getChipFactoredAmmoUsage(Player player) {
		return player.character is MegamanX mmx && mmx.hyperArmArmor == ArmorId.Max ? ammoUsage / 2 : ammoUsage;
	}

	public static float getRateofFireMod(Player player) {
		if (player != null &&
			(player.character as MegamanX)?.hasUltimateArmor != true
		) {
			return 0.75f;
		}
		return 1;
	}

	public float getRateOfFire(Player player) {
		return fireRate * getRateofFireMod(player);
	}

	public override bool canShoot(int chargeLevel, MegamanX mmx) {
		if (mmx.stockedMaxBusterLv >= 1) {
			return false;
		}
		return (
			(ammo >= getChipFactoredAmmoUsage(mmx.player) || chargeLevel >= 3) && 
			base.canShoot(chargeLevel, mmx) && mmx.flag == null
		);
	}

	public bool canShootIncludeCooldown(Player player) {
		return ammo >= getChipFactoredAmmoUsage(player);
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		character.changeState(new X3ChargeShot(this), true);
		if (!mmx.hasUltimateArmor) {
			character.playSound("buster3X3");
		}
	}
}
