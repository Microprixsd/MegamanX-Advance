using System;
using SFML.Graphics;
using SFML.Window;

namespace MMXOnline;

public sealed class QuickArmorSwitchMenu : IMainMenu {
	readonly IMainMenu loadout;
	public static readonly string[] setNumbers = { "1", "2", "3", "4" };
	static readonly string[] partNames = { "Helmet", "Body", "Arms", "Legs" };
	static readonly string[] armorNames = { "X", "Light", "Giga", "Max", "Force", "Plasma" };
	int selectedSet, selectedPart, choice;
	bool editing, expanded;
	static float partX(int part) => 140 + part * 62;
	static float setY(int index) => 55 + index * 35;
	static float choiceY(int armor) => 77 + armor * 21;
	float choiceLeft => Math.Min(partX(selectedPart) - 26, Global.screenW - 124);
	XQuickArmorSet current => XQuickArmorSet.get(selectedSet);

	public QuickArmorSwitchMenu(IMainMenu loadout) {
		this.loadout = loadout;
		for (int i = 0; i < 4; i++) XQuickArmorSet.get(i);
	}
	void save() => Options.main.saveToFile();
	void openPart(int part) {
		selectedPart = part;
		editing = expanded = true;
		choice = current.getPart(part);
	}
	void choose() {
		current.setPart(selectedPart, choice);
		expanded = false;
		save();
		Global.playSound("menuX2");
	}
	public void update() {
		if (Global.input.isPressedMenu(Control.WeaponLeft) || Global.input.isPressedMenu(Control.WeaponRight)) {
			save();
			Menu.change(Global.input.isPressedMenu(Control.WeaponLeft) ? new XSkillTreeMenu(loadout, planning: true) : loadout);
			return;
		}
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (expanded) expanded = false;
			else if (editing) editing = false;
			else { save(); Menu.change(loadout); }
			return;
		}
		if (Input.isMousePressed(Mouse.Button.Left, true)) {
			var mouse = Global.window.MapPixelToCoords(Mouse.GetPosition(Global.window), DrawWrappers.hudView);
			for (int i = 0; i < 4; i++) {
				if (mouse.X >= 22 && mouse.X <= 104 && Math.Abs(mouse.Y - setY(i)) <= 16) {
					selectedSet = i; editing = true; expanded = false; return;
				}
			}
			if (editing) {
				for (int part = 0; part < 4; part++) {
					if (Math.Abs(mouse.X - partX(part)) <= 20 && Math.Abs(mouse.Y - 51) <= 16) {
						openPart(part); return;
					}
				}
				if (expanded && mouse.X >= choiceLeft && mouse.X <= choiceLeft + 104) {
					for (int armor = 0; armor <= (selectedPart == 2 ? 5 : 4); armor++) {
						if (Math.Abs(mouse.Y - choiceY(armor)) <= 10) { choice = armor; choose(); return; }
					}
				}
			}
		}
		if (!editing) {
			Helpers.menuUpDown(ref selectedSet, 0, 3);
			if (Global.input.isPressedMenu(Control.MenuConfirm) || Global.input.isPressedMenu(Control.MenuRight)) editing = true;
		} else if (expanded) {
			Helpers.menuUpDown(ref choice, 0, selectedPart == 2 ? 5 : 4);
			if (Global.input.isPressedMenu(Control.MenuConfirm)) choose();
		} else {
			if (selectedPart == 0 && Global.input.isPressedMenu(Control.MenuLeft)) { editing = false; return; }
			Helpers.menuLeftRightInc(ref selectedPart, 0, 3);
			if (Global.input.isPressedMenu(Control.MenuConfirm)) openPart(selectedPart);
		}
	}
	static void cursor(float x, float y, float halfSize = 17) => DrawWrappers.DrawRect(
		x - halfSize, y - halfSize, x + halfSize, y + halfSize, false,
		Color.White, 1, ZIndex.HUD, false);
	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Yellow, "Quick Armor Switch", Global.halfScreenW, 9, Alignment.Center);
		DrawWrappers.DrawRect(20, 25, 106, 190, true, new Color(13, 23, 39, 230), 1, ZIndex.HUD, false);
		DrawWrappers.DrawRect(114, 25, Global.screenW - 20, 190, true, new Color(13, 23, 39, 230), 1, ZIndex.HUD, false);
		Global.sprites.TryGetValue("quick_xarmors", out var helmets);
		Global.sprites.TryGetValue("newmenu_xarmors", out var pieces);
		for (int i = 0; i < 4; i++) {
			var set = XQuickArmorSet.get(i);
			helmets?.drawToHUD(set.iconFrame, 48, setY(i), selectedSet == i ? 1 : 0.35f);
			if (set.mixed) Fonts.drawText(FontType.Yellow, setNumbers[i], 57, setY(i) + 4);
			Fonts.drawText(selectedSet == i ? FontType.Yellow : FontType.Grey, "Set " + setNumbers[i], 69, setY(i) - 4);
			if (selectedSet == i) cursor(48, setY(i), 13);
		}
		for (int part = 0; part < 4; part++) {
			Fonts.drawText(FontType.Grey, partNames[part], partX(part), 29, Alignment.Center);
			// The collapsed buttons always show X's default pieces.
			pieces?.drawToHUD(part, partX(part), 51, editing ? 1 : 0.35f);
			if (editing && selectedPart == part) cursor(partX(part), 51);
			if (!expanded || selectedPart != part) {
				Fonts.drawText(FontType.Blue, armorNames[current.getPart(part)], partX(part), 79, Alignment.Center);
				int armor = current.getPart(part);
				pieces?.drawToHUD(armor == 5 ? 20 : armor * 4 + part, partX(part), 109, 0.7f);
			}
		}
		if (expanded) {
			float left = choiceLeft;
			float x = left + 15;
			DrawWrappers.DrawRect(left, 66, left + 104, choiceY(selectedPart == 2 ? 5 : 4) + 11,
				true, new Color(13, 23, 39, 250), 1, ZIndex.HUD, false);
			for (int armor = 0; armor <= (selectedPart == 2 ? 5 : 4); armor++) {
				if (pieces != null) {
					// Crop the outer frame to keep the pixel art at native size in the list.
					var rect = pieces.frames[armor == 5 ? 20 : armor * 4 + selectedPart].rect;
					DrawWrappers.DrawTextureHUD(pieces.bitmap,
						rect.x1 + (rect.w() - 20) / 2, rect.y1 + (rect.h() - 20) / 2, 20, 20,
						x - 10, choiceY(armor) - 10, armor == current.getPart(selectedPart) || armor == choice ? 1 : 0.35f);
				}
				Fonts.drawText(choice == armor ? FontType.Yellow : FontType.Grey,
					armorNames[armor], left + 32, choiceY(armor) - 4);
				if (choice == armor) DrawWrappers.DrawRect(left + 2, choiceY(armor) - 10,
					left + 102, choiceY(armor) + 10, false, Color.White, 1, ZIndex.HUD, false);
			}
		}
		Fonts.drawTextEX(FontType.Grey, expanded ? "[MUP]/[MDOWN]: Armor  [OK]: Select" :
			editing ? "[MLEFT]/[MRIGHT]: Piece  [OK]: Open" : "[MUP]/[MDOWN]: Set  [OK]: Edit",
			Global.halfScreenW, 198, Alignment.Center);
		Fonts.drawTextEX(FontType.Grey, "[WeaponL]/[WeaponR]: Page  [BACK]: Back", Global.halfScreenW, 208, Alignment.Center);
	}
}
