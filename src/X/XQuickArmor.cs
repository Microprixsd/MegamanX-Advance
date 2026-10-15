using System;

namespace MMXOnline;

public partial class MegamanX {
	public bool quickArmorSelecting { get; private set; }
	internal bool readingQuickArmorInput;
	int quickArmorIndex;
	bool quickUseSelecting;
	float quickArmorRepeatTime;
	bool canQuickSwitchArmor => !hasFullHyperMaxArmor;

	void updateQuickArmorSelector() {
		if (!ownedByLocalPlayer || player.isAI) return;
		Helpers.decrementTime(ref quickArmorRepeatTime);
		if (Menu.inMenu || !player.canControl || !alive || !charState.normalCtrl ||
			rideArmor != null || rideChaser != null) {
			quickArmorSelecting = false;
			quickUseSelecting = false;
			return;
		}
		if (!player.input.isHeld(Control.Special3, player)) {
			if (quickArmorSelecting) {
				quickArmorSelecting = false;
				if (quickUseSelecting) {
					int item = player.consumableSlots[0];
					if (item >= 0 && player.consumableClickCooldown == 0) {
						player.consumableClickCooldown = 0.2f;
						useConsumable(item, out _);
					}
				} else if (canQuickSwitchArmor) equipQuickArmor(quickArmorIndex);
				quickUseSelecting = false;
			}
			return;
		}
		if (!quickArmorSelecting) {
			quickArmorSelecting = true;
			quickUseSelecting = !canQuickSwitchArmor;
			for (int i = 0; i < 4; i++) {
				var set = XQuickArmorSet.get(i);
				if (set.getPart(0) == (int)helmetArmor && set.getPart(1) == (int)chestArmor &&
					set.getPart(2) == (int)armArmor && set.getPart(3) == (int)legArmor) {
					quickArmorIndex = i;
					break;
				}
			}
		}
		int direction;
		int vertical;
		readingQuickArmorInput = true;
		try {
			direction = (player.input.isHeld(Control.Right, player) ? 1 : 0) -
				(player.input.isHeld(Control.Left, player) ? 1 : 0);
			vertical = (player.input.isHeld(Control.Up, player) ? 1 : 0) -
				(player.input.isHeld(Control.Down, player) ? 1 : 0);
		} finally {
			readingQuickArmorInput = false;
		}
		if (quickArmorRepeatTime > 0) return;
		if (vertical > 0 && !quickUseSelecting) {
			quickUseSelecting = true;
			quickArmorRepeatTime = 0.2f;
		} else if (vertical < 0 && quickUseSelecting && canQuickSwitchArmor) {
			quickUseSelecting = false;
			quickArmorRepeatTime = 0.2f;
		} else if (direction != 0 && vertical == 0 && canQuickSwitchArmor) {
			quickUseSelecting = false;
			quickArmorIndex = (quickArmorIndex + direction + 4) % 4;
			quickArmorRepeatTime = 0.2f;
		}
	}

	void equipQuickArmor(int index) {
		var set = XQuickArmorSet.get(index);
		int armor = set.getPart(0);
		bool changed = false;
		if ((armor == 0 || player.isHeadArmorPurchased(armor)) && (int)helmetArmor != armor) {
			UpgradeArmorMenu.upgradeHelmetArmor(player, armor);
			changed = true;
		}
		armor = set.getPart(1);
		if ((armor == 0 || player.isBodyArmorPurchased(armor)) && (int)chestArmor != armor) {
			UpgradeArmorMenu.upgradeBodyArmor(player, armor);
			changed = true;
		}
		armor = set.getPart(2);
		if ((armor == 0 || player.isArmArmorPurchased(armor)) && (int)armArmor != armor) {
			UpgradeArmorMenu.upgradeArmArmor(player, armor);
			changed = true;
		}
		armor = set.getPart(3);
		if ((armor == 0 || player.isBootsArmorPurchased(armor)) && (int)legArmor != armor) {
			UpgradeArmorMenu.upgradeBootsArmor(player, armor);
			changed = true;
		}
		if (changed) {
			forceNetUpdateNextFrame = true;
			changeState(new XQuickArmorSwitchState(), true);
		}
	}

	void drawQuickArmorSelector(float x, float y) {
		if (!ownedByLocalPlayer || !quickArmorSelecting) return;
		if (canQuickSwitchArmor && Global.sprites.TryGetValue("quick_xarmors", out var icons)) {
			for (int side = -1; side <= 1; side++) {
				int index = (quickArmorIndex + side + 4) % 4;
				var set = XQuickArmorSet.get(index);
				icons.draw(set.iconFrame, pos.x + x + side * 23, pos.y + y - 14,
					1, 1, null, side == 0 && !quickUseSelecting ? 1 : 0.35f, 1, 1, zIndex + 10);
				if (set.mixed) Fonts.drawText(FontType.Yellow, QuickArmorSwitchMenu.setNumbers[index],
					pos.x + x + side * 23 + 7, pos.y + y - 10, isWorldPos: true,
					depth: zIndex + 11, alpha: (byte)(side == 0 && !quickUseSelecting ? 255 : 90));
			}
		}
		int item = player.consumableSlots[0];
		bool ready = item >= 0 && canUseConsumableSlot(0);
		int iconItem = item >= 0 ? item : getConsumableHudItem(0);
		if (iconItem >= 0 && Global.sprites.TryGetValue("store_hud", out var quickUseIcon))
			quickUseIcon.draw(ConsumableShopMenu.getIconFrame(iconItem), pos.x + x, pos.y + y - 38,
				1, 1, null, ready ? (quickUseSelecting ? 1 : 0.35f) : 0.2f, 1, 1, zIndex + 10);
		else if (iconItem < 0 && Global.sprites.TryGetValue("char_icon", out var emptyQuickUseIcon))
			emptyQuickUseIcon.draw(0, pos.x + x, pos.y + y - 38,
				1, 1, null, 0.2f, 1, 1, zIndex + 10);
		if (quickUseSelecting) {
			string label = item >= 0 ? ConsumableShopMenu.inventoryNames[item] : "Quick Use: Empty";
			if (player.consumableCooldowns[0] > 0)
				label += $" ({MathF.Ceiling(player.consumableCooldowns[0])}s)";
			Fonts.drawText(ready ? FontType.Yellow : FontType.Grey, label,
				pos.x + x, pos.y + y - 54, Alignment.Center, isWorldPos: true, depth: zIndex + 11);
		}
	}
}

public class XQuickArmorSwitchState : CharState {
	public XQuickArmorSwitchState() : base("quick_armor") { }

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.playSound("ching", sendRpc: true);
		if (Helpers.randomRange(0, 1) == 0) {
   		character.playSound(Helpers.randomRange(0, 1) == 0 ? 
		"mvsci_suit_up" : "mvsci_armor_equipped",sendRpc: true);
}
	}

	public override void update() {
		base.update();
		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}
