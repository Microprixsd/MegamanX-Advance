using System;

namespace MMXOnline;

public partial class Zero {
	public bool quickGigaSelecting { get; private set; }
	internal bool readingQuickGigaInput;
	int quickGigaIndex;
	float quickGigaRepeatTime;
	const float quickGigaRepeatDelay = 0.2f;
	static readonly ZeroGigaType[] normalGigas = {
		ZeroGigaType.Rakuhouha, ZeroGigaType.Messenkou,
		ZeroGigaType.Rekkoha, ZeroGigaType.Tenshouha
	};
	int availableGigaCount => normalGigas.Length + (isAwakened || isViral ? 1 : 0);
	ZeroGigaType gigaAt(int index) => index < normalGigas.Length ? normalGigas[index] :
		isAwakened ? ZeroGigaType.ShinMessenkou : ZeroGigaType.DarkHold;

	internal void equipGiga(ZeroGigaType type) {
		if (type == ZeroGigaType.ShinMessenkou && !isAwakened ||
			type == ZeroGigaType.DarkHold && !isViral || gigaAttack.type == (int)type) return;
		Weapon next = type switch {
			ZeroGigaType.ShinMessenkou => new ShinMessenkou(),
			ZeroGigaType.DarkHold => new DarkHoldWeapon(),
			_ => RakuhouhaWeapon.getWeaponFromIndex((int)type)
		};
		// All gigas share the current energy and attack cooldown, including hyper transitions.
		next.ammo = gigaAttack.ammo;
		next.shootCooldown = gigaAttack.shootCooldown;
		next.weaponHealAmount = gigaAttack.weaponHealAmount;
		next.weaponHealTime = gigaAttack.weaponHealTime;
		next.weaponHealCount = gigaAttack.weaponHealCount;
		transferConsumableRecovery(gigaAttack, next);
		gigaAttack = next;
		if (Array.IndexOf(normalGigas, type) >= 0) gigaAttackSelected = (int)type;
		forceNetUpdateNextFrame = true;
	}

	internal void removeHyperGiga() {
		if (gigaAttack.type == (int)ZeroGigaType.ShinMessenkou || gigaAttack.type == (int)ZeroGigaType.DarkHold)
			equipGiga((ZeroGigaType)gigaAttackSelected);
		quickGigaIndex = Math.Min(quickGigaIndex, availableGigaCount - 1);
	}

	void updateQuickGigaSelector() {
		if (!ownedByLocalPlayer || player.isAI) return;
		Helpers.decrementTime(ref quickGigaRepeatTime);
		if (!isAwakened && gigaAttack.type == (int)ZeroGigaType.ShinMessenkou ||
			!isViral && gigaAttack.type == (int)ZeroGigaType.DarkHold) removeHyperGiga();
		quickGigaIndex = Math.Min(quickGigaIndex, availableGigaCount - 1);
		if (Menu.inMenu || !player.canControl || !alive || !charState.normalCtrl ||
			rideArmor != null || rideChaser != null) {
			quickGigaSelecting = false;
			return;
		}
		if (!player.input.isHeld(Control.Special3, player)) {
			if (quickGigaSelecting) {
				quickGigaSelecting = false;
				if (gigaAttack.type != (int)gigaAt(quickGigaIndex)) {
					equipGiga(gigaAt(quickGigaIndex));
					Global.playSound("ching");
				}
			}
			return;
		}
		if (!quickGigaSelecting) {
			quickGigaSelecting = true;
			for (int i = 0; i < availableGigaCount; i++)
				if ((int)gigaAt(i) == gigaAttack.type) quickGigaIndex = i;
		}
		int direction;
		readingQuickGigaInput = true;
		try {
			direction = (player.input.isHeld(Control.Right, player) ? 1 : 0) -
				(player.input.isHeld(Control.Left, player) ? 1 : 0);
		} finally {
			readingQuickGigaInput = false;
		}
		if (direction != 0 && quickGigaRepeatTime == 0) {
			quickGigaIndex = (quickGigaIndex + direction + availableGigaCount) % availableGigaCount;
			quickGigaRepeatTime = quickGigaRepeatDelay;
		}
	}

	void drawQuickGigaSelector(float x, float y) {
		if (!ownedByLocalPlayer || !quickGigaSelecting ||
			!Global.sprites.TryGetValue("hud_weapon_icon", out var icons)) return;
		for (int side = -1; side <= 1; side++) {
			var type = gigaAt((quickGigaIndex + side + availableGigaCount) % availableGigaCount);
			int frame = type switch {
				ZeroGigaType.Rakuhouha => 51, ZeroGigaType.Messenkou => 64,
				ZeroGigaType.Rekkoha => 63, ZeroGigaType.Tenshouha => 131,
				ZeroGigaType.ShinMessenkou => 64, _ => 122
			};
			icons.draw(frame, pos.x + x + side * 23, pos.y + y - 14,
				1, 1, null, side == 0 ? 1 : 0.35f, 1, 1, zIndex + 10);
		}
		string name = gigaAt(quickGigaIndex) switch {
			ZeroGigaType.ShinMessenkou => "Shin Messenkou",
			ZeroGigaType.DarkHold => "Dark Hold",
			var type => type.ToString()
		};
		Fonts.drawText(FontType.Yellow, name, pos.x + x, pos.y + y - 32,
			Alignment.Center, isWorldPos: true, depth: zIndex + 11);
	}
}
