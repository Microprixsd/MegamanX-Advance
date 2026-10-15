using System;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

public partial class UpgradeArmorMenu {
	bool selectingTanks;
	int tankIndex;
	static readonly string[] armorNames = { "X", "Light", "Giga", "Max", "Force" };
	static readonly string[] partNames = { "Helmet", "Body", "Arms", "Legs" };
	static readonly string[] previewParts = { "helmet", "body", "arm", "boots" };
	static readonly string[] descriptions = {
		"Headbutt enemies while jumping.\nCan be combined with an upward dash.",
		"Reduces incoming damage by 12.5%.\nReduces flinch duration by 25%.",
		"Grants the Spiral Crush Buster.\nCharges shots 50% faster.",
		"Improves your ground dash.\nDash 15% faster and farther.",
		"Trace enemy health and position.\nPress SPECIAL to use the tracer.",
		"Grants the Giga Crush attack.\nReduces incoming damage by 12.5%.",
		"Grants the Double X-Buster.\nStore an extra charged shot.",
		"Improves your air dash.\nDash 15% faster and farther.",
		"Track enemies on the minimap\nusing the satellite.",
		"Taking damage activates a forcefield.\nThe field reduces damage by 25%.",
		"Grants Hyper Charge, the Z-Saber\nand the Cross Charge shot.",
		"Grants an upward dash\nfor vertical movement in the air.",
		"Fire uncharged special weapons\nwithout consuming ammo.",
		"Grants the Nova Strike attack.\nReduces incoming damage by 12.5%.",
		"Store up to four buster shots\nto fire them in succession.",
		"Grants hover, allowing X\nto stay airborne.",
		"Grants the Plasma Shot\nas your charged buster attack."
	};
	static readonly string[] chipDescriptions = {
		"C: Regenerate HP outside combat.", "C: Stronger defensive forcefield.",
		"C: Enables Hyper Charge.", "C: Dash twice in the air."
	};
	int ownedTankCount => Math.Min(4, mainPlayer.subtanks.Count);
	int selectedPart => selectArrowPosY;
	int selectedArmor => xGame == 4 && selectedPart == 2 && mainPlayer.armArmorNum == 5 ? 5 : xGame;
	static Point armorPosition(int row, int column) => new(76 + column * 36, 36 + (row - 1) * 34);
	static Point tankPosition(int index) => new(338, 32 + index * 34);
	int equipped(int part) => part switch {
		0 => mainPlayer.helmetArmorNum, 1 => mainPlayer.bodyArmorNum,
		2 => mainPlayer.armArmorNum, _ => mainPlayer.legArmorNum
	};
	bool purchased(int part, int armor) => armor == 0 || (part switch {
		0 => mainPlayer.isHeadArmorPurchased(armor), 1 => mainPlayer.isBodyArmorPurchased(armor),
		2 => mainPlayer.isArmArmorPurchased(armor), _ => mainPlayer.isBootsArmorPurchased(armor)
	});
	int armorCost(int part) => part switch {
		0 => MegamanX.headArmorCost, 1 => MegamanX.chestArmorCost,
		2 => MegamanX.armArmorCost, _ => MegamanX.bootsArmorCost
	};
	void equip(int part, int armor) {
		if (mainPlayer.character is MegamanX { hasUltimateArmor: true } && armor == 0) return;
		switch (part) {
			case 0: upgradeHelmetArmor(mainPlayer, armor); break;
			case 1: upgradeBodyArmor(mainPlayer, armor); break;
			case 2: upgradeArmArmor(mainPlayer, armor); break;
			case 3: upgradeBootsArmor(mainPlayer, armor); break;
		}
		if (mainPlayer.character is MegamanX x) x.forceNetUpdateNextFrame = true;
	}
	bool chipActive(MegamanX x, int part) => part switch {
		0 => x.hyperHelmetActive, 1 => x.hyperChestActive, 2 => x.hyperArmActive, _ => x.hyperLegActive
	};
	void toggleChip(MegamanX x, int part) {
		bool enable = !chipActive(x, part);
		clearAllHyperArmor(x);
		if (enable) {
			switch (part) {
				case 0: x.hyperHelmetActive = true; break;
				case 1: x.hyperChestActive = true; break;
				case 2: x.hyperArmActive = true; break;
				case 3: x.hyperLegActive = true; break;
			}
		}
		Global.playSound("ching");
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.MenuBack)) { Menu.change(prevMenu); return; }
		if (Global.input.isPressedMenu(Control.WeaponLeft) || Global.input.isPressedMenu(Control.WeaponRight)) {
			Menu.change(Global.input.isPressedMenu(Control.WeaponRight)
				? new XSkillTreeMenu(prevMenu) : new ConsumableShopMenu(prevMenu));
			return;
		}
		if (selectingTanks && ownedTankCount == 0) selectingTanks = false;
		if (selectingTanks) {
			tankIndex = Math.Clamp(tankIndex, 0, ownedTankCount - 1);
			Helpers.menuUpDown(ref tankIndex, 0, ownedTankCount - 1);
			if (Global.input.isPressedMenu(Control.MenuLeft)) { selectingTanks = false; return; }
			if (Global.input.isPressedMenu(Control.MenuConfirm) && mainPlayer.character != null &&
				UpgradeMenu.subtankDelay == 0 && mainPlayer.canUseSubtank(mainPlayer.subtanks[tankIndex])) {
				mainPlayer.subtanks[tankIndex].use(mainPlayer.character);
			}
			return;
		}
		Helpers.menuUpDown(ref xGame, 1, 4);
		selectArrowPosY = Math.Min(selectArrowPosY, 3);
		if (Global.input.isPressedMenu(Control.MenuLeft)) selectArrowPosY = Math.Max(0, selectArrowPosY - 1);
		if (Global.input.isPressedMenu(Control.MenuRight)) {
			if (selectArrowPosY < 3) selectArrowPosY++;
			else if (ownedTankCount > 0) { selectingTanks = true; tankIndex = Math.Min(xGame - 1, ownedTankCount - 1); return; }
		}
		var x = mainPlayer.character as MegamanX;
		if (x == null || x.hasFullHyperMaxArmor) return;
		int part = selectedPart, armor = selectedArmor;
		if (Global.input.isPressedMenu(Control.MenuAlt)) {
			if (!x.hasUltimateArmor) { equip(part, 0); Global.playSound("ching"); }
		} else if (Global.input.isPressedMenu(Control.MenuConfirm) ||
			(xGame == 4 && part == 2 && Global.input.isPressedMenu(Control.Special1))) {
			if (xGame == 4 && part == 2) {
				armor = Global.input.isPressedMenu(Control.Special1) ? 5 : 4;
			}
			if (armor == 3 && equipped(part) == 3 && mainPlayer.hasAllX3Armor()) { toggleChip(x, part); return; }
			if (!purchased(part, armor)) {
				if (mainPlayer.currency < armorCost(part)) return;
				switch (part) {
					case 0: purchaseHelmetArmor(mainPlayer, armor); break;
					case 1: purchaseBodyArmor(mainPlayer, armor); break;
					case 2: purchaseArmArmor(mainPlayer, armor); break;
					case 3: purchaseBootsArmor(mainPlayer, armor); break;
				}
			}
			equip(part, armor);
			Global.playSound("ching");
		}
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Yellow, "X Armors", Global.halfScreenW, 12, Alignment.Center);
		Fonts.drawText(FontType.OrangeMenu, $"{Global.nameCoins}: {mainPlayer.currency}", Global.screenW - 12, 12, Alignment.Right);
		var x = mainPlayer.character as MegamanX;
		if (x?.hasUltimateArmor == true) {
			drawArmorPreview("menu_xultimate");
		} else if (x?.hasFullHyperMaxArmor == true) {
			drawArmorPreview("menu_xgolden");
		} else {
			drawArmorPreview("menu_xdefault");
			for (int part = 0; part < 4; part++) {
				int armor = equipped(part);
				if (armor == 0) continue;
				string key = "menu_x" + previewParts[part] + (armor == 1 ? "" : armor.ToString());
				drawArmorPreview(key);
			}
		}
		var icons = Global.sprites["newmenu_xarmors"];
		for (int row = 1; row < 5; row++) {
			Fonts.drawText(FontType.Grey, armorNames[row], 18, armorPosition(row, 0).y - 4);
			for (int column = 0; column < 4; column++) {
				int part = column;
				int armor = row == 4 && part == 2 && mainPlayer.armArmorNum == 5 ? 5 : row;
				Point p = armorPosition(row, column);
				drawArmorIcon(icons, armor == 5 ? 20 : row * 4 + column, p,
					purchased(part, armor), equipped(part) == armor);
				if (row == 3) Fonts.drawText(x != null && chipActive(x, part) ? FontType.Yellow : FontType.Grey,
					"C", p.x + 10, p.y + 8, alpha: (byte)(mainPlayer.hasAllX3Armor() ? 255 : 90));
				if (!selectingTanks && row == xGame && column == selectArrowPosY) drawGridCursor(p, 16);
			}
		}
		for (int i = 0; i < 4; i++) {
			Point p = tankPosition(i);
			bool owned = i < ownedTankCount;
			Global.sprites["menu_subtank"].drawToHUD(1, p.x - 2, p.y - 4, owned ? 1 : 0.25f);
			if (owned) {
				var tank = mainPlayer.subtanks[i];
				Global.sprites["menu_subtank_bar"].drawToHUD(0, p.x + 4, p.y - 3);
				DrawWrappers.DrawRect(p.x + 4, p.y - 3, p.x + 9, p.y + 11 - 14 * tank.health / SubTank.maxHealth,
					true, Color.Black, 1, ZIndex.HUD, false);
				if (!mainPlayer.canUseSubtank(tank) || UpgradeMenu.subtankDelay > 0)
					Global.sprites["menu_subtank"].drawToHUD(2, p.x - 2, p.y - 4, 0.5f);
			}
			if (selectingTanks && i == tankIndex) drawGridCursor(p.addxy(6, 4), 14);
		}
		string description;
		if (selectingTanks) {
			Fonts.drawText(FontType.Yellow, $"Sub Tank {tankIndex + 1}", 18, 160);
			description = "Stores health energy for later use.\nPress [OK] to recover HP.";
		}
		else {
			int part = selectedPart, armor = selectedArmor;
			description = descriptions[armor == 5 ? 16 : (xGame - 1) * 4 + part];
			if (armor == 3 && mainPlayer.hasAllX3Armor()) {
				description = (part switch {
					0 => "Track enemies on the minimap.",
					1 => "Gain a forcefield after taking damage.",
					2 => "Hyper Charge and Cross Charge shot.",
					_ => "Grants an upward dash."
				}) + "\n" + chipDescriptions[part];
			}
			string price = purchased(part, armor) ? "" : $"{armorCost(part)} {Global.nameCoins}";
			Fonts.drawText(FontType.Yellow, armorNames[xGame] + " " + partNames[part] + (armor == 5 ? " 5" : ""), 18, 160);
			Fonts.drawText(FontType.OrangeMenu, price, Global.screenW - 12, 160, Alignment.Right);
		}
		Fonts.drawTextEX(FontType.Blue, description, 18, 173);
		bool selectingForceArms = !selectingTanks && xGame == 4 && selectedPart == 2;
		bool selectingMaxChip = !selectingTanks && xGame == 3 &&
			equipped(selectedPart) == 3 && mainPlayer.hasAllX3Armor();
		Fonts.drawTextEX(FontType.Grey, x?.hasUltimateArmor == true
			? (selectingForceArms ? "[OK]: Stock  [BACK]: Back" : "[OK]: Equip  [BACK]: Back")
			: selectingForceArms
			? "[OK]: Stock  [ALT]: Remove  [BACK]: Back"
			: selectingMaxChip
			? "[OK]: Chip  [ALT]: Remove  [BACK]: Back"
			: "[OK]: Equip  [ALT]: Remove  [BACK]: Back", 18, 199);
		Fonts.drawTextEX(FontType.Grey,
			selectingForceArms
				? "[SPC]: Buster 5  [WeaponL]/[WeaponR]: Page"
				: "[WeaponL]/[WeaponR]: Page", 18, 209);
	}
	static void drawGridCursor(Point p, float radius) => DrawWrappers.DrawRect(
		p.x - radius, p.y - radius, p.x + radius, p.y + radius, false, Color.White, 1, ZIndex.HUD, false);

	static void drawArmorPreview(string key) {
		if (!Global.sprites.TryGetValue(key, out var data)) return;
		var frame = data.frames[0];
		var rect = frame.rect;
		using var sprite = new SFML.Graphics.Sprite(data.bitmap,
			new IntRect(((int)rect.x1, (int)rect.y1), ((int)rect.w(), (int)rect.h())));
		sprite.Origin = new Vector2f(
			MathF.Floor(rect.w() * data.baseAlignmentX + data.alignOffX - frame.offset.x),
			MathF.Floor(rect.h() * data.baseAlignmentY + data.alignOffY - frame.offset.y));
		sprite.Position = new Vector2f(262, 86);
		sprite.Scale = new Vector2f(0.65f, 0.65f);
		DrawWrappers.drawToHUD(sprite);
	}
}
