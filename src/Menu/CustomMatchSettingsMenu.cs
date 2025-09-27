﻿using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

[ProtoContract]
public class CustomMatchSettings {
	[ProtoMember(1)] public bool hyperModeMatch;
	[ProtoMember(2)] public int startCurrency;
	[ProtoMember(3)] public int startHeartTanks;
	[ProtoMember(4)] public int startSubTanks;
	[ProtoMember(5)] public int healthModifier;
	[ProtoMember(5)] public int damageModifier;
	[ProtoMember(6)] public int sameCharNum;
	[ProtoMember(7)] public int redSameCharNum;
	[ProtoMember(8)] public int maxHeartTanks;
	[ProtoMember(9)] public int maxSubTanks;
	[ProtoMember(10)] public int heartTankHp;
	[ProtoMember(11)] public int heartTankCost;
	[ProtoMember(12)] public int currencyGain;
	[ProtoMember(13)] public int respawnTime;
	[ProtoMember(14)] public bool pickupItems;
	[ProtoMember(15)] public int subtankGain;
	[ProtoMember(16)] public int assistTime;
	[ProtoMember(16)] public bool assistable;
	[ProtoMember(17)] public int largeHealthPickup;
	[ProtoMember(18)] public int smallHealthPickup;
	[ProtoMember(19)] public int largeAmmoPickup;
	[ProtoMember(20)] public int smallAmmoPickup;
	[ProtoMember(21)] public int subTankCost;
	[ProtoMember(22)] public bool frostShieldNerf;
	[ProtoMember(23)] public bool frostShieldChargedNerf;
	[ProtoMember(24)] public bool axlBackwardsDebuff;
	[ProtoMember(25)] public float axlDodgerollCooldown;
	[ProtoMember(26)] public bool axlCustomReload;
	[ProtoMember(27)] public bool oldATrans;
	[ProtoMember(28)] public bool flinchairDashReset;
	[ProtoMember(29)] public bool ComboFlinch;


	public CustomMatchSettings() {
	}

	public static CustomMatchSettings getDefaults() {
		return new CustomMatchSettings {
			hyperModeMatch = false,
			startCurrency = 3,
			startHeartTanks = 0,
			startSubTanks = 0,
			healthModifier = 16,
			damageModifier = 1,
			sameCharNum = -1,
			redSameCharNum = -1,
			maxHeartTanks = 8,
			maxSubTanks = 2,
			heartTankHp = 1,
			heartTankCost = 2,
			currencyGain = 1,
			respawnTime = 5,
			pickupItems = true,
			subtankGain = 4,
			assistTime = 2,
			assistable = true,
			largeHealthPickup = 8,
			smallHealthPickup = 4,
			largeAmmoPickup = 50,
			smallAmmoPickup = 25,
			subTankCost = 4,
			frostShieldNerf = true,
			frostShieldChargedNerf = false,
			axlBackwardsDebuff = true,
			axlDodgerollCooldown = 1.25f,
			axlCustomReload = false,
			oldATrans = false,
			flinchairDashReset = false,
			ComboFlinch = true,
		};
	}
}

public class CustomMatchSettingsMenu : IMainMenu {
	public int selectArrowPosY;
	public int selectArrowPosY2;
	public int selectArrowPosY3;
	public const int startX = 30;
	public int startY = 40;
	public const int lineH = 10;
	public const int startX2 = 30;
	public int startY2 = 40;
	public const int lineH2 = 10;
	public const int startX3 = 30;
	public int startY3 = 40;
	public const int lineH3 = 10;
	public const uint fontSize = 24;
	public IMainMenu prevMenu;
	public bool inGame;
	public int Page = 1;
	public bool isOffline;
	public List<MenuOption> menuOptions = new List<MenuOption>();
	public List<MenuOption> menuOptions2 = new List<MenuOption>();
	public List<MenuOption> menuOptions3 = new List<MenuOption>();


	SavedMatchSettings savedMatchSettings { get { return isOffline ? SavedMatchSettings.mainOffline : SavedMatchSettings.mainOnline; } }

	public CustomMatchSettingsMenu(IMainMenu prevMenu, bool inGame, bool isOffline) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		int currentY = startY;
		int currentY2 = startY2;
		int currentY3 = startY3;
		this.isOffline = isOffline;
		#region  Page 1
		menuOptions.Add(
			new MenuOption(startX, currentY,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.healthModifier, 8, 32);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Base Health: " +
						(savedMatchSettings.customMatchSettings.healthModifier).ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 0
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.heartTankCost, 0, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Heart Tanks Cost: " +
						savedMatchSettings.customMatchSettings.heartTankCost.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 1
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startHeartTanks, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Start Heart Tanks: " +
						savedMatchSettings.customMatchSettings.startHeartTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 2
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxHeartTanks, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Max Heart Tanks: " +
						savedMatchSettings.customMatchSettings.maxHeartTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 3
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.heartTankHp, 1, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Heart Tank HP: " +
						savedMatchSettings.customMatchSettings.heartTankHp.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 4
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.subTankCost, 0, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Sub Tanks Cost: " +
						savedMatchSettings.customMatchSettings.subTankCost.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 5
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startSubTanks, 0, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Start Sub Tanks: " +
						savedMatchSettings.customMatchSettings.startSubTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 6
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxSubTanks, 0, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Max Sub Tanks: " +
						savedMatchSettings.customMatchSettings.maxSubTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 7
					);
				}
			)
		);

		menuOptions.Add(
				new MenuOption(
					startX, currentY += lineH,
					() => {
						Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.subtankGain, 1, 4, true);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue,
							"Sub Tank Gain: " +
							savedMatchSettings.customMatchSettings.subtankGain.ToString(),
							pos.x, pos.y, selected: selectArrowPosY == 8
						);
					}
				)
		);

		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.hyperModeMatch);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Hypermode Match : " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.hyperModeMatch),
						pos.x, pos.y, selected: selectArrowPosY == 9
					);
				}
			)
		);
		/*
			menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.damageModifier, 1, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Damage modifier: " +
						(savedMatchSettings.customMatchSettings.damageModifier * 100).ToString() + "%",
						pos.x, pos.y, selected: selectArrowPosY == 9
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.sameCharNum, -1, 4);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Mono character: " +
						getSameCharString(savedMatchSettings.customMatchSettings.sameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == 9
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.redSameCharNum, -1, 4);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Red mono character: " +
						getSameCharString(savedMatchSettings.customMatchSettings.redSameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == 10
					);
				}
			)
		);
		*/
		#endregion
		#region  Page 2
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startCurrency, 0, 9999, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Start " + Global.nameCoins + ": " +
						savedMatchSettings.customMatchSettings.startCurrency.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 0
					);
				}
			)
		);
		//Currency Gain Custom Setting
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.currencyGain, 1, 10, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Currency Gain modifier: " +
						savedMatchSettings.customMatchSettings.currencyGain.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 1
					);
				}
			)
		);
		//Respawn Time Custom Setting
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.respawnTime, 1, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Respawn Time modifier: " +
						savedMatchSettings.customMatchSettings.respawnTime.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 2
					);
				}
			)
		);
		menuOptions2.Add(
				new MenuOption(
					startX2, currentY2 += lineH2,
					() => {
						Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.assistTime, 0, 5, true);
					},
					(Point pos, int index) => {
						Fonts.drawText(
							FontType.Blue,
							"Assist Time: " +
							savedMatchSettings.customMatchSettings.assistTime.ToString(),
							pos.x, pos.y, selected: selectArrowPosY2 == 3
						);
					}
				)
			);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.assistable);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Unassistable List: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.assistable),
						pos.x, pos.y, selected: selectArrowPosY2 == 4
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.pickupItems);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Pick Up Items: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.pickupItems),
						pos.x, pos.y, selected: selectArrowPosY2 == 5
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.largeHealthPickup, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Large Health Recovery: " +
						savedMatchSettings.customMatchSettings.largeHealthPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 6
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.smallHealthPickup, 0, 32, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Small Health Recovery: " +
						savedMatchSettings.customMatchSettings.smallHealthPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 7
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.largeAmmoPickup, 0, 100, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Large Ammo Recovery: " +
						savedMatchSettings.customMatchSettings.largeAmmoPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 8
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.smallAmmoPickup, 0, 100, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Small Ammo Recovery: " +
						savedMatchSettings.customMatchSettings.smallAmmoPickup.ToString(),
						pos.x, pos.y, selected: selectArrowPosY2 == 9
					);
				}
			)
		);
		#endregion
		#region Page 3
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.frostShieldNerf, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Frost Shield Uncharged 'Shield' Nerf: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.frostShieldNerf),
						pos.x, pos.y, selected: selectArrowPosY3 == 0
					);
				}
			)
		);
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.frostShieldChargedNerf, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Frost Shield Charged 'Shield' Nerf: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.frostShieldChargedNerf),
						pos.x, pos.y, selected: selectArrowPosY3 == 1
					);
				}
			)
		);
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.axlBackwardsDebuff, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Axl Shooting Backwards Debuff: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.axlBackwardsDebuff),
						pos.x, pos.y, selected: selectArrowPosY3 == 2
					);
				}
			)
		);
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightIncFloat(ref savedMatchSettings.customMatchSettings.axlDodgerollCooldown, 1.25f, 3, true, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Axl Dodge Roll Cooldown: " +
						savedMatchSettings.customMatchSettings.axlDodgerollCooldown.ToString(),
						pos.x, pos.y, selected: selectArrowPosY3 == 3
					);
				}
			)
		);
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.axlCustomReload, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Axl Weapons Capable to Reload: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.axlCustomReload),
						pos.x, pos.y, selected: selectArrowPosY3 == 4
					);
				}
			)
		);
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.oldATrans, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Axl Vanilla DNA: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.oldATrans),
						pos.x, pos.y, selected: selectArrowPosY3 == 5
					);
				}
			)
		);
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.flinchairDashReset, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Flinch resets Air Dash: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.flinchairDashReset),
						pos.x, pos.y, selected: selectArrowPosY3 == 6
					);
				}
			)
		);
		menuOptions3.Add(
			new MenuOption(
				startX3, currentY3 += lineH3,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.ComboFlinch, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Purple,
						"Flinch stack: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.ComboFlinch),
						pos.x, pos.y, selected: selectArrowPosY3 == 7
					);
				}
			)
		);
		#endregion
	}

	public string getSameCharString(int charNum) {
		if (charNum == -1) return "No";
		return Character.charDisplayNames[charNum];
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.Special1)) {
			Page++;
			if (Page > 3) Page = 1;
		}
		if (Page == 1) {
			menuOptions[selectArrowPosY].update();
			Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);
		} else if (Page == 2) {
			menuOptions2[selectArrowPosY2].update();
			Helpers.menuUpDown(ref selectArrowPosY2, 0, menuOptions2.Count - 1);
		}
		else if (Page == 3) {
			menuOptions3[selectArrowPosY3].update();
			Helpers.menuUpDown(ref selectArrowPosY3, 0, menuOptions3.Count - 1);
		}

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (savedMatchSettings.customMatchSettings.maxHeartTanks < savedMatchSettings.customMatchSettings.startHeartTanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max heart tanks can't be", "less than start heart tanks." }, this));
				return;
			}

			if (savedMatchSettings.customMatchSettings.maxSubTanks < savedMatchSettings.customMatchSettings.startSubTanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max sub tanks can't be", "less than start sub tanks." }, this));
				return;
			}

			Menu.change(prevMenu);
		}
	}

	public void render() {
		Cursor();
		drawText();
		int i = 0;
		if (Page == 1)
		foreach (var menuOption in menuOptions) {
			menuOption.render(menuOption.pos, i);
			i++;
		}
		if (Page == 2)
		foreach (var menuOption2 in menuOptions2) {
			menuOption2.render(menuOption2.pos, i);
			i++;
		}
		if (Page == 3)
		foreach (var menuOption3 in menuOptions3) {
			menuOption3.render(menuOption3.pos, i);
			i++;
		}
	}
	public void drawText() {
		Fonts.drawText(
			FontType.Yellow, "Custom Match Options",
			Global.halfScreenW, 20, alignment: Alignment.Center
		);
		Fonts.drawText(
			FontType.Yellow, "Page: " + Page,
			Global.halfScreenW+150, 20, alignment: Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Grey, "[MLEFT]/[MRIGHT]: Change setting, [SPC]: Change Page, [BACK]: Back",
			Global.halfScreenW-6, Global.screenH - 26, Alignment.Center
		);
	}
	public void Cursor() {
		if (Page == 1) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions[selectArrowPosY].pos.x - 8,
					menuOptions[selectArrowPosY].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions[selectArrowPosY].pos.x - 8, menuOptions[selectArrowPosY].pos.y + 5
				);
			}
		} else if (Page == 2) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions2[selectArrowPosY2].pos.x - 8,
					menuOptions2[selectArrowPosY2].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions2[selectArrowPosY2].pos.x - 8, menuOptions2[selectArrowPosY2].pos.y + 5
				);
			}
		}
		else if (Page == 3) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions3[selectArrowPosY3].pos.x - 8,
					menuOptions3[selectArrowPosY3].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions3[selectArrowPosY3].pos.x - 8, menuOptions3[selectArrowPosY3].pos.y + 5
				);
			}
		}
	}
}
