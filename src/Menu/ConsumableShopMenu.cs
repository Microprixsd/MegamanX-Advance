using System;
using SFML.Graphics;

namespace MMXOnline;

public class ConsumableShopMenu : IMainMenu {
	const int itemCount = 14;
	const int columns = 4;
	readonly IMainMenu prevMenu;
	int selectedItem;
	int bottleChoice;
	bool expanded;
	string message = "";
	public static readonly string[] inventoryNames = {
		"Life Recover", "Weapon Recover", "Life Charge", "Weapon Charge", "Weapon Drive", "Power Drive",
		"Spare Energy", "Metal Generator", "Overdrive", "Hyperdrive", "Escape Boost", "Retry Chip",
		"Life Bottle Half", "Life Bottle Full", "Weapon Bottle Half", "Weapon Bottle Full"
	};
	static readonly int[] prices = { 12, 12, 8, 8, 8, 8, 10, 4, 12, 12, 12, 12, 4, 7, 3, 6 };
	// store_hud order differs from the inventory; Hyperbuster represents Overdrive.
	static readonly int[] iconFrames = { 2, 4, 12, 13, 5, 3, 7, 15, 1, 0, 14, 6, 9, 8, 11, 10 };
	public static int getIconFrame(int item) => iconFrames[item];
	static readonly string[] descriptions = {
		"32 HP, slower healing with hyperarmor.",
		"Refill all weapon and giga energy while taunting.",
		"Fill 16 HP into Sub Tanks; overflow to the next.",
		"Refill weapons. Excludes giga and Hypercharge.",
		"Special weapons use no energy for 5 seconds.",
		"Weak special weapon flinch becomes half for 7s.",
		"Below 1/8 HP: 4 shield HP (3s), then heal 4 HP.",
		"Double earned scraps for 10 seconds.",
		"4s: X fires level 2 shots; Zero gets free buster.",
		"Replace defense with 50% damage reduction, 4s.",
		"Received flinch cannot exceed half for 4s.",
		"Survive a lethal hit at 1 HP; 2s invulnerability.",
		"Restore 4 HP.", "Restore 8 HP.",
		"Restore 6 energy to the selected special weapon.",
		"Restore 12 energy to the selected special weapon."
	};
	int inventoryItem => selectedItem < 12 ? selectedItem : 12 + (selectedItem - 12) * 2 + bottleChoice;
	string selectedName => selectedItem >= itemCount ? selectedItem == itemCount ? "Quick Use" : "Passive" :
		selectedItem < 12 || expanded ? inventoryNames[inventoryItem] : selectedItem == 12 ? "Life Bottle" : "Weapon Bottle";
	static readonly Color itemBorder = new(145, 91, 46);
	static readonly Color passiveBorder = new(80, 220, 65);
	static readonly Color panelFill = new(13, 30, 40, 235);

	public ConsumableShopMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
	}

	public void update() {
		Player player = Global.level.mainPlayer;
		Helpers.decrementTime(ref player.consumableClickCooldown);
		if (Global.input.isPressedMenu(Control.MenuAlt)) {
			if (player.consumableClickCooldown > 0) return;
			player.consumableClickCooldown = 0.2f;
			if (selectedItem >= itemCount) {
				player.consumableSlots[selectedItem - itemCount] = -1;
				player.character?.refreshPassiveSlot();
				message = "Unequipped.";
				return;
			}
			int slot = Character.consumableSlot(inventoryItem);
			if (slot < 0) { message = "Used automatically on purchase."; return; }
			if (player.consumableSlots[slot] >= 0) {
				if (player.character != null && player.character.useConsumable(player.consumableSlots[slot], out message)) Menu.exit();
			} else if (player.consumableInventory[inventoryItem] > 0) {
				player.consumableSlots[slot] = inventoryItem;
				player.character?.refreshPassiveSlot();
				message = "Assigned " + inventoryNames[inventoryItem] + ".";
			} else message = "No item in inventory.";
			return;
		}
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (expanded) { expanded = false; return; }
			Menu.change(prevMenu);
			return;
		}
		if (Global.input.isPressedMenu(Control.WeaponLeft)) {
			Menu.change(Global.level.mainPlayer.realCharNum == (int)CharIds.Zero ? prevMenu : new XSkillTreeMenu(prevMenu));
			return;
		}
		if (Global.input.isPressedMenu(Control.WeaponRight)) {
			Menu.change(Global.level.mainPlayer.realCharNum == (int)CharIds.Zero ? prevMenu : new UpgradeArmorMenu(prevMenu));
			return;
		}
		if (expanded) {
			int previousChoice = bottleChoice;
			Helpers.menuLeftRightInc(ref bottleChoice, 0, 1);
			if (previousChoice != bottleChoice) message = "";
		}
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectedItem >= itemCount) return;
			if (selectedItem >= 12 && !expanded) { expanded = true; bottleChoice = 0; return; }
			if (player.currency < prices[inventoryItem]) message = "Not enough scraps.";
			else {
				player.consumableInventory[inventoryItem]++;
				int slot = Character.consumableSlot(inventoryItem);
				if (slot < 0) {
					if (player.character == null || !player.character.useConsumable(inventoryItem, out message)) {
						player.consumableInventory[inventoryItem]--;
						if (player.character == null) message = "Cannot use while defeated.";
						return;
					}
				} else {
					if (player.consumableSlots[slot] < 0) {
						player.consumableSlots[slot] = inventoryItem;
						player.character?.refreshPassiveSlot();
						message = "Assigned " + inventoryNames[inventoryItem] + ".";
					} else message = "Stored " + inventoryNames[inventoryItem] + ".";
				}
				player.currency -= prices[inventoryItem];
				Global.playSound("menuX2");
			}
			return;
		}
		if (expanded) return;
		int previousItem = selectedItem;
		if (selectedItem >= itemCount) {
			if (Global.input.isPressedMenu(Control.MenuUp)) selectedItem = itemCount;
			else if (Global.input.isPressedMenu(Control.MenuDown)) selectedItem = itemCount + 1;
			else if (Global.input.isPressedMenu(Control.MenuLeft)) selectedItem = selectedItem == itemCount ? 3 : 11;
			if (previousItem != selectedItem) message = "";
			return;
		}
		int row = selectedItem / columns;
		int column = selectedItem % columns;
		if (Global.input.isPressedMenu(Control.MenuLeft) && column > 0) selectedItem--;
		else if (Global.input.isPressedMenu(Control.MenuRight)) {
			if (column < columns - 1 && selectedItem + 1 < itemCount) selectedItem++;
			else selectedItem = itemCount + (row < 2 ? 0 : 1);
		}
		else if (Global.input.isPressedMenu(Control.MenuUp) && row > 0) selectedItem -= columns;
		else if (Global.input.isPressedMenu(Control.MenuDown) && row < (itemCount - 1) / columns)
			selectedItem = Math.Min(selectedItem + columns, itemCount - 1);
		if (previousItem != selectedItem) message = "";
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		DrawWrappers.DrawRect(8, 8, Global.screenW - 8, 195, true,
			panelFill, 1, ZIndex.HUD, false);
		Player player = Global.level.mainPlayer;
		Fonts.drawText(FontType.Yellow, selectedName, 18, 15);
		Fonts.drawText(FontType.Grey, selectedItem >= itemCount ? "[ALT]: Unequip without using or losing the item." : descriptions[inventoryItem], 18, 28);
		Fonts.drawText(FontType.Grey, message.Length > 0 ? message : selectedItem >= itemCount ?
			(player.consumableSlots[selectedItem - itemCount] >= 0 ? inventoryNames[player.consumableSlots[selectedItem - itemCount]] : "Empty") :
			$"Cost: {prices[inventoryItem]} scraps", 18, 39);

		// Catalog quantities and the two equipped slots on the right.
		float gridWidth = Global.screenW * 0.68f;
		for (int i = 0; i < itemCount; i++) {
			float x = 18 + gridWidth * (i % columns + 0.5f) / columns;
			float y = 65 + i / columns * 32;
			drawSlot(x, y, itemBorder);
			int iconItem = i < 12 ? i : 12 + (i - 12) * 2 + (i == selectedItem && expanded ? bottleChoice : 1);
			Global.sprites["store_hud"].drawToHUD(getIconFrame(iconItem), x, y - 2);
			int count = i < 12 ? player.consumableInventory[i] : player.consumableInventory[12 + (i - 12) * 2] + player.consumableInventory[13 + (i - 12) * 2];
			Fonts.drawText(FontType.OrangeMenu, $"x{count}", x, y + 10, Alignment.Center);
			if (i == selectedItem) DrawWrappers.DrawRect(x - 17, y - 13, x + 17, y + 18,
				false, Color.White, 1, ZIndex.HUD, false);
		}
		if (expanded) {
			DrawWrappers.DrawRect(17, 143, Global.screenW * 0.68f + 17, 177, true, panelFill, 1, ZIndex.HUD, false);
			for (int choice = 0; choice < 2; choice++) {
				int item = 12 + (selectedItem - 12) * 2 + choice;
				float x = 24 + choice * 120;
				Global.sprites["store_hud"].drawToHUD(getIconFrame(item), x, 160);
				Fonts.drawText(choice == bottleChoice ? FontType.Yellow : FontType.Grey,
					$"{(choice == 0 ? "Half" : "Full")} ({prices[item]}) x{player.consumableInventory[item]}", x + 12, 155);
			}
		}

		float passiveX = Global.screenW - 42;
		for (int i = 0; i < 2; i++) {
			float y = 76 + i * 72;
			Fonts.drawText(FontType.Green, i == 0 ? "Quick Use" : "Passive", passiveX, y - 24, Alignment.Center);
			drawSlot(passiveX, y, passiveBorder);
			int item = player.consumableSlots[i];
			if (item >= 0) {
				Global.sprites["store_hud"].drawToHUD(getIconFrame(item), passiveX, y - 2);
				Fonts.drawText(FontType.OrangeMenu, $"x{player.consumableInventory[item]}", passiveX, y + 10, Alignment.Center);
			} else Fonts.drawText(FontType.Grey, "-", passiveX, y - 3, Alignment.Center);
			if (selectedItem == itemCount + i) DrawWrappers.DrawRect(passiveX - 17, y - 13, passiveX + 17, y + 18,
				false, Color.White, 1, ZIndex.HUD, false);
		}
		Fonts.drawText(FontType.OrangeMenu, $"{Global.nameCoins}: {Global.level.mainPlayer.currency}", 18, 183);
		Fonts.drawTextEX(FontType.Grey, "[OK]: Buy  [ALT]: Assign/Use/Unequip  Arrows: Select", Global.halfScreenW, 198, Alignment.Center);
		Fonts.drawTextEX(FontType.Grey, "[WeaponL]/[WeaponR]: Page  [BACK]: Back", Global.halfScreenW, 209, Alignment.Center);
	}

	static void drawSlot(float x, float y, Color border) => DrawWrappers.DrawRect(
		x - 15, y - 11, x + 15, y + 17, false, border, 1, ZIndex.HUD, false);
}
