﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public partial class Player {
	public List<Weapon> weapons => character?.weapons ?? oldWeapons;
	public List<Weapon> oldWeapons = [];

	public int weaponSlot {
		get => character?.weaponSlot ?? 0;
		set {
			if (character == null) { return; }
			character.weaponSlot = value;
		}
	}
	public Weapon? weapon => character?.currentWeapon;

	public Weapon? lastHudWeapon = null;

	public MaverickWeapon? maverickWeapon {
		get { return weapon as MaverickWeapon; }
	}

	public List<Maverick> mavericks {
		get {
			List<Maverick> mavericks = new();
			if (character != null) {
				foreach (var weapon in character.weapons) {
					if (weapon is MaverickWeapon mw && mw.maverick != null) {
						mavericks.Add(mw.maverick);
					}
				}
			}
			return mavericks;
		}
	}

	/// <summary> Returns the current manually-controlled maverick. </summary>
	public MaverickWeapon? currentMaverickWeapon {
		get {
			if (character == null) {
				return null;
			}
			foreach (var weapon in character.weapons) {
				if (weapon is MaverickWeapon mw && mw.maverick != null && mw.maverick == currentMaverick) {
					return mw;
				}
			}
			return null;
		}
	}

	public Maverick? currentMaverick => character?.currentMaverick;

	public bool gridModeHeld;
	public Point gridModePos = new Point();
	public void changeWeaponControls() {
		if (character == null) return;
		if (!character.canChangeWeapons()) return;

		if (isGridModeEnabled() && character.weapons.Count > 1) {
			if (input.isHeldMenu(Control.WeaponRight)) {
				gridModeHeld = true;
				if (input.isPressedMenu(Control.Up)) gridModePos.y--;
				else if (input.isPressedMenu(Control.Down)) gridModePos.y++;

				if (input.isPressedMenu(Control.Left)) gridModePos.x--;
				else if (input.isPressedMenu(Control.Right)) gridModePos.x++;

				if (gridModePos.y < -1) gridModePos.y = -1;
				if (gridModePos.y > 1) gridModePos.y = 1;
				if (gridModePos.x < -1) gridModePos.y = -1;
				if (gridModePos.x > 1) gridModePos.x = 1;

				var gridPoints = gridModePoints();

				for (int i = 0; i < character.weapons.Count; i++) {
					if (i >= gridPoints.Length) break;
					var gridPoint = gridPoints[i];
					if (gridModePos.x == gridPoint.x && gridModePos.y == gridPoint.y && character.weapons.Count >= i + 1) {
						changeWeaponSlot(i);
					}
				}
			} else {
				gridModeHeld = false;
				gridModePos = new Point();
			}
			return;
		}

		if ((isAxl || isDisguisedAxl) && isMainPlayer) {
			if (Input.mouseScrollUp) {
				weaponLeft();
				return;
			} else if (Input.mouseScrollDown) {
				weaponRight();
				return;
			}
		}

		if (input.isPressed(Control.WeaponLeft, this)) {
			weaponLeft();
		} else if (input.isPressed(Control.WeaponRight, this)) {
			weaponRight();
		} else if (character != null && !Control.isNumberBound(realCharNum, Options.main.axlAimMode)) {
			if (input.isPressed(Key.Num1, canControl) && weapons.Count >= 1) {
				changeWeaponSlot(0);
			} else if (input.isPressed(Key.Num2, canControl) && weapons.Count >= 2) {
				changeWeaponSlot(1);
			} else if (input.isPressed(Key.Num3, canControl) && weapons.Count >= 3) {
				changeWeaponSlot(2);
			} else if (input.isPressed(Key.Num4, canControl) && weapons.Count >= 4) {
				changeWeaponSlot(3);
			} else if (input.isPressed(Key.Num5, canControl) && weapons.Count >= 5) {
				changeWeaponSlot(4);
			} else if (input.isPressed(Key.Num6, canControl) && weapons.Count >= 6) {
				changeWeaponSlot(5);
			} else if (input.isPressed(Key.Num7, canControl) && weapons.Count >= 7) {
				changeWeaponSlot(6);
			} else if (input.isPressed(Key.Num8, canControl) && weapons.Count >= 8) {
				changeWeaponSlot(7);
			} else if (input.isPressed(Key.Num9, canControl) && weapons.Count >= 9) {
				changeWeaponSlot(8);
			} else if (input.isPressed(Key.Num0, canControl) && weapons.Count >= 10) {
				changeWeaponSlot(9);
			}
		
		}
	}

	public void changeToSigmaSlot() {
		for (int i = 0; i < weapons.Count; i++) {
			if (weapons[i] is SigmaMenuWeapon) {
				changeWeaponSlot(i);
				return;
			}
		}
	}

	public void removeWeaponSlot(int index) {
		if (index < 0 || index >= weapons.Count) return;
		if (index < weaponSlot && weaponSlot > 0) weaponSlot--;
		for (int i = weapons.Count - 1; i >= 0; i--) {
			if (i == index) {
				weapons.RemoveAt(i);
				return;
			}
		}
	}

	public void changeWeaponSlot(int newWeaponSlot) {
		if (weaponSlot == newWeaponSlot) return;
		if (isDead) return;
		if (!weapons.InRange(newWeaponSlot)) return;
		if (weapons[newWeaponSlot].index == (int)WeaponIds.MechMenuWeapon) {
			selectedRAIndex = 0;
		}

		Weapon oldWeapon = weapon;
		if (oldWeapon is MechMenuWeapon mmw) {
			mmw.isMenuOpened = false;
		}

		weaponSlot = newWeaponSlot;
		Weapon newWeapon = weapon;

		if (newWeapon is MaverickWeapon mw) {
			mw.selCommandIndex = 1;
			mw.selCommandIndexX = 1;
			mw.isMenuOpened = false;
		}

		character.onWeaponChange(oldWeapon, newWeapon);

		/* if (isX) {
			if (character.getChargeLevel() >= 2) {
				newWeapon.shootTime = 0;
			} else {
				// Switching from laggy move (like tornado) to a fast one
				if (oldWeapon.switchCooldown != null && oldWeapon.shootTime > 0) {
					newWeapon.shootTime = Math.Max(newWeapon.shootTime, oldWeapon.switchCooldown.Value);
				} else {
					newWeapon.shootTime = Math.Max(newWeapon.shootTime, oldWeapon.shootTime);
				}
				/*
				if (newWeapon is NovaStrike ns) {
					ns.shootTime = 0;
				}
				
			}
		} */

		if (character is Axl axl) {
			if (oldWeapon is AxlWeapon) {
				axl.axlSwapTime = axl.switchTime;
				axl.axlAltSwapTime = axl.altSwitchTime;
			}
			if (axl.isZooming()) {
				axl.zoomOut();
			}
		}
	}

	public void weaponLeft() {
		int ws = weaponSlot - 1;
label:
		if (ws < 0) {
			ws = weapons.Count - 1;
		}
		if ((weapons.ElementAtOrDefault(ws) is GigaCrush && Options.main.gigaCrushSpecial) ||
			(weapons.ElementAtOrDefault(ws) is HyperNovaStrike && Options.main.novaStrikeSpecial)
		) {
			ws--;
			goto label;
		}
		changeWeaponSlot(ws);
	}

	public void weaponRight() {
		int ws = weaponSlot + 1;
label:
		int max = weapons.Count;
		if (ws >= max) {
			ws = 0;
		}
		if ((weapons.ElementAtOrDefault(ws) is GigaCrush && Options.main.gigaCrushSpecial) || (weapons.ElementAtOrDefault(ws) is HyperNovaStrike && Options.main.novaStrikeSpecial)) {
			ws++;
			goto label;
		}
		changeWeaponSlot(ws);
	}
	public void clearXWeapons() {
		preXWeapons = new List<Weapon>(weapons);
		weapons.Clear();
	}
	public List<Weapon>? preXWeapons;

	public void configureWeapons(Character character) {
		// Save weapons for cross-life maverick HP if not an Axl.
		if (!character.isATrans) {
			oldWeapons = character.weapons;
		}
	}

	private Weapon getAxlBullet(int axlBulletType) {
		switch (axlBulletType) {
			case (int)AxlBulletWeaponType.DoubleBullets:
				return new DoubleBullet();
			case (int)AxlBulletWeaponType.MetteurCrash:
				return new MettaurCrash();
			case (int)AxlBulletWeaponType.BeastKiller:
				return new BeastKiller();
			case (int)AxlBulletWeaponType.MachineBullets:
				return new MachineBullets();
			case (int)AxlBulletWeaponType.RevolverBarrel:
				return new RevolverBarrel();
			case (int)AxlBulletWeaponType.AncientGun:
				return new AncientGun();		
			default:
				return new AxlBullet((AxlBulletWeaponType)axlBulletType);
		}
	}

	public Weapon getAxlBulletWeapon() {
		return getAxlBulletWeapon(axlBulletType);
	}

	public Weapon getAxlBulletWeapon(int type) {
		switch (type) {
			case (int)AxlBulletWeaponType.DoubleBullets:
				return new DoubleBullet();
			case (int)AxlBulletWeaponType.MetteurCrash:
				return new MettaurCrash();
			case (int)AxlBulletWeaponType.BeastKiller:
				return new BeastKiller();
			case (int)AxlBulletWeaponType.MachineBullets:
				return new MachineBullets();
			case (int)AxlBulletWeaponType.RevolverBarrel:
				return new RevolverBarrel();
			case (int)AxlBulletWeaponType.AncientGun:
				return new AncientGun();			
			default:
				return new AxlBullet((AxlBulletWeaponType)type);
		}
	}

	public int getLastWeaponIndex() {
		int miscSlots = 0;
		if (weapons.Any(w => w is GigaCrush)) miscSlots++;
		if (weapons.Any(w => w is HyperCharge)) miscSlots++;
		if (weapons.Any(w => w is HyperNovaStrike)) miscSlots++;
		return weapons.Count - miscSlots;
	}

	public void addGigaCrush() {
		if (!weapons.Any(w => w is GigaCrush)) {
			weapons.Add(new GigaCrush());
		}
	}


	public void removeNovaStrike() {
		if (weapon is HyperNovaStrike) {
			weaponSlot = 0;
		}
		weapons.RemoveAll(w => w is HyperNovaStrike);
	}

	public void removeHyperCharge() {
		if (character != null) {

		}
		if (weapon is HyperCharge) {
			weaponSlot = 0;
		}
		weapons.RemoveAll(w => w is HyperCharge);
	}

	public void removeGigaCrush() {
		if (weapon is GigaCrush) {
			weaponSlot = 0;
		}
		weapons.RemoveAll(w => w is GigaCrush);
	}

	public void updateWeapons() {
		if (character?.weapons == null) {
			return;
		}
		foreach (var weapon in character.weapons) {
			weapon.update();
			if (character != null && character.alive) {
				bool alwaysOn = false;
				if (weapon is GigaCrush && Options.main.gigaCrushSpecial ||
					weapon is HyperNovaStrike && Options.main.novaStrikeSpecial
				) {
					alwaysOn = true;
				}
				weapon.charLinkedUpdate(character, alwaysOn);
			}
		}
	}
}
