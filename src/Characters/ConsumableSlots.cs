using System;

namespace MMXOnline;

public partial class Character {
	int[] activeSlotItems => player.activeConsumableSlotItems;
	public static int consumableSlot(int item) => item is 6 or 10 or 11 ? 1 : item is 0 or 1 or 4 or 5 or 7 or 8 or 9 ? 0 : -1;
	public static float consumableCooldown(int item) => item switch {
		0 or 4 or 9 => 8, 1 or 5 or 6 or 10 or 11 => 6, 8 => 12, 7 => 4, _ => 0
	};
	public bool canUseConsumableSlot(int slot) => player.consumableCooldowns[slot] <= 0 && activeSlotItems[slot] < 0;
	public int getConsumableHudItem(int slot) => activeSlotItems[slot] >= 0 ? activeSlotItems[slot] :
		player.consumableCooldowns[slot] > 0 ? player.lastConsumedSlotItems[slot] : player.consumableSlots[slot];
	public void refreshPassiveSlot() {
		spareEnergyArmed = player.consumableSlots[1] == 6 && canUseConsumableSlot(1);
		retryChipArmed = player.consumableSlots[1] == 11 && canUseConsumableSlot(1);
	}
	void trackConsumableUse(int item) {
		int slot = consumableSlot(item);
		if (slot < 0) return;
		player.consumableSlots[slot] = player.consumableInventory[item] > 0 ? item : -1;
		activeSlotItems[slot] = item;
		player.lastConsumedSlotItems[slot] = item;
		bool usedInHypermode = this is MegamanX && player.hyperModeTime > 0 ||
			this is Zero zero && zero.hypermodeActive();
		// Capture the penalty at use time, even if Hypermode ends before the effect.
		player.consumableCooldownMax[slot] = consumableCooldown(item) + (usedInHypermode ? 5 : 0);
		refreshPassiveSlot();
	}
	bool consumableEffectActive(int item) => item switch {
		0 => lifeRecoverActive, 1 => consumableRecoveryWeapons.Count > 0,
		4 => weaponDriveTime > 0, 5 => powerDriveTime > 0,
		6 => spareHealRemaining > 0 || consumableShieldTime > 0,
		7 => metalGeneratorTime > 0, 8 => overdriveTime > 0, 9 => hyperdriveTime > 0,
		10 => escapeBoostTime > 0, 11 => invulnTime > 0, _ => false
	};
	void updateConsumableSlots() {
		if (!Menu.inMenu) Helpers.decrementTime(ref player.consumableClickCooldown);
		for (int slot = 0; slot < 2; slot++) {
			Helpers.decrementTime(ref player.consumableCooldowns[slot]);
			int item = activeSlotItems[slot];
			if (item >= 0 && (!alive || health <= 0 || !consumableEffectActive(item))) {
				player.consumableCooldowns[slot] = player.consumableCooldownMax[slot];
				activeSlotItems[slot] = -1;
			}
		}
		refreshPassiveSlot();
	}
	public void tryActivateEscapeBoost() {
		if (player.consumableSlots[1] == 10 && canUseConsumableSlot(1))
			useConsumable(10, out _, automatic: true);
	}
	internal void transferConsumableRecovery(Weapon previous, Weapon replacement) {
		int index = consumableRecoveryWeapons.IndexOf(previous);
		if (index >= 0) consumableRecoveryWeapons[index] = replacement;
	}
}
