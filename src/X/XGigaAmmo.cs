using System;

namespace MMXOnline;

public partial class MegamanX {
	public float sharedGigaAmmo { get; private set; }
	public bool canChargeSharedGigaAmmo => chestArmor is ArmorId.Giga or ArmorId.Force;

	public T bindSharedGigaAmmo<T>(T weapon) where T : Weapon {
		if (weapon is GigaCrush or ForceNovaStrike) {
			weapon.sharedGigaAmmoOwner = this;
		}
		return weapon;
	}

	internal void setSharedGigaAmmo(float value) {
		if (!float.IsFinite(value)) return;
		value = Math.Clamp(value, 0, 16);
		// Remote weapon snapshots must still be accepted without the charging armor.
		if (ownedByLocalPlayer && value > sharedGigaAmmo && !canChargeSharedGigaAmmo) return;
		sharedGigaAmmo = value;
	}

	public void chargeSharedGigaAmmo(float amount) {
		if (!canChargeSharedGigaAmmo) return;
		float previousAmmo = sharedGigaAmmo;
		setSharedGigaAmmo(sharedGigaAmmo + amount);
		if (player.isMainPlayer) {
			Weapon.gigaAttackSoundLogic(this, previousAmmo, sharedGigaAmmo, 16, 16);
		}
	}
}
