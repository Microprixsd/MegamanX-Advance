using System;

namespace MMXOnline;

public partial class Player {
	public const float zeroHyperModeCapacity = 16 * 21;
	public const float zeroHyperDamageScale = 48;
	public float zeroHyperModeCharge { get; private set; }
	public bool zeroHyperModeReady => zeroHyperModeCharge >= zeroHyperModeCapacity &&
		!(character is Zero zero && (zero.hypermodeActive() || zero.charState is HyperZeroStart));

	// Keep Zero's balance formula independent of X; the match rate still applies to both.
	internal static float calculateZeroHyperCharge(float damage, float maxHealth, float chargeRate) {
		return damage * zeroHyperDamageScale * chargeRate / Math.Max(1, maxHealth);
	}

	void awardZeroHyperDamage(float damage, float chargeRate) {
		if (character is Zero zero && (zero.hypermodeActive() || zero.charState is HyperZeroStart)) return;
		zeroHyperModeCharge = Math.Min(zeroHyperModeCapacity,
			zeroHyperModeCharge + calculateZeroHyperCharge(damage, getMaxHealth(), chargeRate));
	}

	public bool activateZeroHyperMode() {
		if (!ownedByLocalPlayer || !zeroHyperModeReady || character is not Zero zero || zero.health <= 0 ||
			Global.level.gameMode.isOver || !zero.changeState(new HyperZeroStart(), true)) return false;
		zeroHyperModeCharge = 0;
		return true;
	}
}
