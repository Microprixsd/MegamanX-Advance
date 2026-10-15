using SFML.Graphics;

namespace MMXOnline;

// Reserved for Zero's upcoming skill tree design.
public class ZeroSkillTreeMenu : IMainMenu {
	readonly UpgradeMenu upgradesMenu;

	public ZeroSkillTreeMenu(UpgradeMenu upgradesMenu) {
		this.upgradesMenu = upgradesMenu;
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.WeaponRight)) {
			Menu.change(new ConsumableShopMenu(this));
			return;
		}
		if (Global.input.isPressedMenu(Control.MenuBack) ||
			Global.input.isPressedMenu(Control.MenuLeft) ||
			Global.input.isPressedMenu(Control.WeaponLeft)) {
			Menu.change(upgradesMenu);
		}
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Yellow, "Zero Skill Tree", Global.halfScreenW, 12, Alignment.Center);
		DrawWrappers.DrawRect(20, 23, Global.screenW - 20, 194, true,
			new Color(13, 23, 39, 230), 1, ZIndex.HUD, false, outlineColor: new Color(62, 89, 116));
		Fonts.drawTextEX(FontType.Grey, "[WeaponL]/[WeaponR]: Change Menu",
			Global.halfScreenW, Global.screenH - 28, Alignment.Center);
		Fonts.drawTextEX(FontType.Grey, "[BACK]: Back",
			Global.halfScreenW, Global.screenH - 18, Alignment.Center);
	}
}
