using System;
using System.Collections.Generic;

namespace MMXOnline;

public partial class Character {
	public float weaponDriveTime, powerDriveTime, hyperdriveTime, overdriveTime, escapeBoostTime, metalGeneratorTime;
	public bool retryChipArmed, spareEnergyArmed;
	public decimal consumableShield;
	float consumableShieldTime, spareHealTime;
	int spareHealRemaining;
	bool lifeRecoverActive;
	readonly List<Weapon> consumableRecoveryWeapons = new();
	internal readonly XBuster overdriveBuster = new() { consumableOverdrive = true, fireRate = 12 };
	readonly Dictionary<Weapon, Weapon> overdriveOriginalWeapons = new();
	public bool hasConsumableRecovery => lifeRecoverActive && healAmount > 0 || consumableRecoveryWeapons.Exists(w => w.weaponHealAmount > 0);

	public static bool isConsumableGiga(Weapon weapon) => weapon is GigaCrush or ForceNovaStrike or HyperNovaStrike or
		RakuhouhaWeapon or Messenkou or RekkohaWeapon or ShinMessenkou or DarkHoldWeapon or TenshouhaWeapon;
	public static bool isConsumableSpecialWeapon(Weapon weapon) => !isConsumableGiga(weapon) && weapon is not XBuster and not EmptyWeapon && weapon.maxAmmo > 0;
	internal static bool isOverdriveWeapon(Weapon weapon) => !isConsumableGiga(weapon) && weapon is not HyperCharge and not EmptyWeapon;

	List<Weapon> consumableAmmoTargets(bool includeGiga) {
		var targets = new List<Weapon>();
		void add(Weapon? weapon) {
			if (weapon != null && weapon.maxAmmo > 0 && weapon is not XBuster and not EmptyWeapon &&
				(includeGiga || !isConsumableGiga(weapon) && weapon is not HyperCharge) && !targets.Contains(weapon)) targets.Add(weapon);
		}
		foreach (Weapon weapon in weapons) add(overdriveOriginalWeapons.GetValueOrDefault(weapon, weapon));
		if (includeGiga && this is Zero zero) add(zero.gigaAttack);
		if (includeGiga && this is MegamanX x) {
			add(x.gigaCrush);
			add(x.hyperNovaStrike);
			add(x.hyperBuster);
			add(x.gigaWeapon);
		}
		return targets;
	}

	public bool useConsumable(int item, out string message, bool automatic = false) {
		message = "Cannot use now.";
		if (item < 0 || item >= player.consumableInventory.Length || player.consumableInventory[item] <= 0) {
			message = "No item in inventory.";
			return false;
		}
		if (!ownedByLocalPlayer || !alive || health <= 0 || this is not MegamanX and not Zero ||
			(!automatic && (!charState.normalCtrl || !charState.attackCtrl || hasConsumableRecovery))) return false;
		int slot = consumableSlot(item);
		if (slot >= 0 && !canUseConsumableSlot(slot)) { message = "Slot effect or cooldown active."; return false; }
		List<Weapon> ammoTargets = consumableAmmoTargets(item == 1);
		if ((item == 0 || item == 12 || item == 13) && (health >= maxHealth || healAmount > 0)) {
			message = "Life is already full or recovering."; return false;
		}
		if (item == 2 && !player.subtanks.Exists(t => t.health < SubTank.maxHealth && !t.isInUse)) {
			message = "No empty Sub Tank capacity."; return false;
		}
		if ((item == 1 || item == 3) && !ammoTargets.Exists(w => w.ammo < w.maxAmmo)) {
			message = "Weapon energy is already full."; return false;
		}
		Weapon? bottleTarget = currentWeapon;
		if (item >= 14 && (bottleTarget == null || bottleTarget is HyperCharge || !isConsumableSpecialWeapon(bottleTarget) || bottleTarget.ammo >= bottleTarget.maxAmmo)) {
			message = "Select a special weapon needing energy."; return false;
		}
		if (item == 0 || item == 1) {
			if (!grounded || healAmount > 0 || !changeState(new Taunt(recovering: true), true)) return false;
		}
		if (item == 4 && weaponDriveTime > 0 || item == 5 && powerDriveTime > 0 ||
			item == 6 && spareHealRemaining > 0 || item == 7 && metalGeneratorTime > 0 ||
			item == 8 && overdriveTime > 0 || item == 9 && hyperdriveTime > 0 ||
			item == 10 && escapeBoostTime > 0) {
			message = "This effect is already active."; return false;
		}
		player.consumableInventory[item]--;
		switch (item) {
			case 0:
				lifeRecoverActive = true;
				charState.superArmor = true;
				healTime = 0;
				addHealth(32, false);
				break;
			case 1:
			case 3:
				foreach (Weapon weapon in ammoTargets) {
					weapon.weaponHealAmount = weapon.maxAmmo - weapon.ammo;
					weapon.weaponHealTime = 0;
					if (item == 1 && weapon.weaponHealAmount > 0) consumableRecoveryWeapons.Add(weapon);
				}
				break;
			case 2:
				float remaining = SubTank.maxHealth;
				foreach (SubTank tank in player.subtanks) {
					if (tank.isInUse) continue;
					float amount = MathF.Min(remaining, SubTank.maxHealth - tank.health);
					tank.health += amount;
					remaining -= amount;
					if (remaining <= 0) break;
				}
				break;
			case 4: weaponDriveTime = 5; break;
			case 5: powerDriveTime = 7; break;
			case 6: startSpareEnergy(); break;
			case 7: metalGeneratorTime = 10; break;
			case 8: overdriveTime = 4; stopCharge(); updateOverdriveWeapons(); break;
			case 9: hyperdriveTime = 4; break;
			case 10: escapeBoostTime = 4; break;
			case 11: invulnTime = MathF.Max(invulnTime, 2); break;
			case 12: addHealth(4, false); break;
			case 13: addHealth(8, false); break;
			case 14: bottleTarget!.addAmmoHeal(6); break;
			case 15: bottleTarget!.addAmmoHeal(12); break;
		}
		trackConsumableUse(item);
		foreach (Weapon weapon in consumableAmmoTargets(true)) weapon.consumableOwner = this;
		forceNetUpdateNextFrame = true;
		message = "Used " + ConsumableShopMenu.inventoryNames[item] + ".";
		return true;
	}

	void updateOverdriveWeapons() {
		if (this is not MegamanX) return;
		for (int i = 0; i < weapons.Count; i++) {
			Weapon weapon = weapons[i];
			if (overdriveTime <= 0) {
				if (overdriveOriginalWeapons.TryGetValue(weapon, out Weapon? original)) weapons[i] = original;
			} else if (isOverdriveWeapon(weapon) && !overdriveOriginalWeapons.ContainsKey(weapon)) {
				var replacement = new XBuster { consumableOverdrive = true, fireRate = 12 };
				overdriveOriginalWeapons[replacement] = weapon;
				weapons[i] = replacement;
			}
		}
		if (overdriveTime <= 0) overdriveOriginalWeapons.Clear();
	}

	void updateConsumables() {
		if (!ownedByLocalPlayer) return;
		weaponDriveTime = MathF.Max(0, weaponDriveTime - Global.spf);
		powerDriveTime = MathF.Max(0, powerDriveTime - Global.spf);
		hyperdriveTime = MathF.Max(0, hyperdriveTime - Global.spf);
		overdriveTime = MathF.Max(0, overdriveTime - Global.spf);
		escapeBoostTime = MathF.Max(0, escapeBoostTime - Global.spf);
		metalGeneratorTime = MathF.Max(0, metalGeneratorTime - Global.spf);
		updateOverdriveWeapons();
		if (overdriveTime > 0 || overdriveBuster.shootCooldown > 0) overdriveBuster.update();
		// Giga weapons absent from the loadout still use the pickup refill cadence.
		foreach (Weapon weapon in consumableAmmoTargets(true)) {
			weapon.consumableOwner = this;
			if (overdriveOriginalWeapons.ContainsValue(weapon)) weapon.update();
			if (weapon.weaponHealAmount > 0 && !weapons.Contains(weapon) && !(this is Zero z && weapon == z.gigaAttack)) weapon.charLinkedUpdate(this, true);
		}
		if (lifeRecoverActive && (healAmount <= 0 || health >= maxHealth || charState is not Taunt)) {
			if (charState is not Taunt) healAmount = 0;
			lifeRecoverActive = false;
			if (charState is Taunt) charState.superArmor = false;
		}
		if (consumableRecoveryWeapons.Count > 0 && charState is not Taunt) {
			foreach (Weapon weapon in consumableRecoveryWeapons) weapon.weaponHealAmount = 0;
			consumableRecoveryWeapons.Clear();
		} else consumableRecoveryWeapons.RemoveAll(w => w.weaponHealAmount <= 0);
		if (!alive || health <= 0) { updateConsumableSlots(); return; }
		tryActivateSpareEnergy();
		consumableShieldTime = MathF.Max(0, consumableShieldTime - Global.spf);
		if (consumableShieldTime <= 0) consumableShield = 0;
		if (spareHealRemaining > 0) {
			spareHealTime -= Global.spf;
			if (spareHealTime <= 0) {
				spareHealTime += 1;
				spareHealRemaining--;
				health = Math.Min(maxHealth, health + 1);
				playAltSound("heal", sendRpc: true, altParams: "harmor");
			}
		}
		updateConsumableSlots();
	}

	void tryActivateSpareEnergy() {
		refreshPassiveSlot();
		if (!spareEnergyArmed || health <= 0 || health >= maxHealth / 8) return;
		useConsumable(6, out _, automatic: true);
	}

	void startSpareEnergy() {
		spareEnergyArmed = false;
		consumableShield = 4;
		consumableShieldTime = 3;
		spareHealRemaining = 4;
		// A one-second delay, then one HP per second over four seconds.
		spareHealTime = 2;
	}
}

public class Hurt : CharState {
	public float flinchYPos;
	public bool isCombo;
	public int hurtDir;
	public float hurtSpeed;
	public float flinchTime;
	public bool spiked;

	public float flinchLeft => (flinchTime - stateFrames);

	public Hurt(int dir, int flinchFrames, bool spiked = false, float? oldComboPos = null) : base("hurt") {
		hurtDir = dir;
		hurtSpeed = dir * 1.6f;
		flinchTime = flinchFrames;
		this.spiked = spiked;
		if (oldComboPos != null) {
			isCombo = true;
			flinchYPos = oldComboPos.Value;
		}
	}

	public bool isMiniFlinch() {
		return flinchTime <= 6;
	}

	public override bool canEnter(Character character) {
		if (character.isStatusImmune()) return false;
		if (character.vaccineTime > 0) return false;
		if (character.rideArmorPlatform != null) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (character.escapeBoostTime > 0) flinchTime = MathF.Min(flinchTime, Global.halfFlinch);
		if (character is MegamanX mmx)
		 {  string? hurtSound = null;
			if (flinchTime >= Global.superFlinch) {
    			hurtSound = Helpers.randomRange(0, 1) == 0
       			 ? "mvsci_hurt31" : "mvsci_hurt32";
				} else if (flinchTime >= Global.defFlinch) {
    				hurtSound = "x8_hurt2";
					} else if (flinchTime <= Global.halfFlinch) {
    					hurtSound = "x8_hurt1";
						}
						if (hurtSound != null && Helpers.randomRange(0, 99) < 20) {
   						 	character.playSound(hurtSound, sendRpc: true);
							}
			bool hasShockBuffer = player.xSkills.has(XSkill.ShockBuffer1) ||
				player.xSkills.has(XSkill.ShockBuffer2);
			bool useHurt2 = flinchTime <= Global.defFlinch &&
				(mmx.chestArmor != ArmorId.None || hasShockBuffer);
			if (mmx.chestArmor == ArmorId.Light) {
				flinchTime = MathF.Floor(flinchTime * 0.75f);
			}
			if (useHurt2) {
				sprite = "hurt2";
				character.changeSpriteFromName("hurt2", true);
			}
		}
		if (!spiked && character is not BaseSigma && !character.isToughGuyHyperMode()) {
			float limitedFlinchTime = MathF.Min(flinchTime, 30);
			float verticalImpulse = -7.5f * (limitedFlinchTime - 1);
			if (isCombo && character.pos.y < flinchYPos) {
				verticalImpulse = (0.002f * limitedFlinchTime - 0.076f) *
					(flinchYPos - character.pos.y) + 1;
			}
			// Retain the original reaction and combo adjustment at two-thirds strength.
			character.vel.y = verticalImpulse * (2f / 3f);
		}
		if (!isCombo) flinchYPos = character.pos.y;
	}

	public override void update() {
		base.update();
		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 1.6f / flinchTime * Global.speedMul, hurtDir);
			if (character is not BaseSigma && !character.isToughGuyHyperMode()) //Tough guy
				character.move(new Point(hurtSpeed * 60f, 0));
		}

		if (isMiniFlinch()) {
			character.frameSpeed = 0;
			if (Global.floorFrameCount % 2 == 0) {
				if (player.charNum == 0) character.frameIndex = 3;
				if (player.charNum == 1) character.frameIndex = 3;
				if (player.charNum == 2) character.frameIndex = 0;
				if (player.charNum == 3) character.frameIndex = 3;
			} else {
				if (player.charNum == 0) character.frameIndex = 2;
				if (player.charNum == 1) character.frameIndex = 2;
				if (player.charNum == 2) character.frameIndex = 1;
				if (player.charNum == 3) character.frameIndex = 2;
			}
		}

		if (stateFrames >= flinchTime) {
			character.changeToLandingOrFall(false);
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		//Came back from the death as Custom Setting
		if (Global.customSettings?.flinchAirDashReset == true) {
			character.dashedInAir = 0;
		}
	}
}

// Applies to freeze, stun, other effects.
public class GenericStun : CharState {
	public Anim? paralyzeAnim;
	public bool changeAnim = true;
	public bool canPlayFrozenSound = true;
	public bool canPlayStaticSound = true;
	public int hurtDir = 1;
	public float hurtSpeed;
	public float flinchYPos;

	public float flinchTime;
	public float flinchMaxTime;

	public GenericStun() : base("hurt") {

	}

	public override void update() {
		Helpers.decrementFrames(ref flinchTime);

		crystalizeLogic();
		paralizeAnimLogic();
		freezeLogic();

		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 1.6f / flinchMaxTime * Global.speedMul, hurtDir);
			character.move(new Point(hurtSpeed * 60f, 0));
		}

		if (changeAnim) {
			string stunAnim = getStunAnim();
			character.changeSpriteFromName(getStunAnim(), true);
			if (stunAnim == "idle") {
				character.sprite.frameSpeed = 0;
			}
		}

		if (character.frozenTime == 0 && character.crystalizedTime == 0 && character.paralyzedTime == 0) {
			if (flinchTime > 0) {
				character.changeState(
					new Hurt(hurtDir, MathInt.Ceiling(flinchTime), false, flinchYPos
					), true
				);
				return;
			}
			character.changeToIdleOrFall();
		}
	}
	
	public void freezeLogic() {
		if (character.frozenTime == 0) {
			return;
		}
		if (canPlayFrozenSound) {
			character.playSound("igFreeze", true);
			canPlayFrozenSound = false;
		}
		reduceStunFrames(ref character.frozenTime);
		character.freezeInvulnTime = 2;

		if (character.frozenTime == 0) {
			character.breakFreeze(player, sendRpc: true);
			canPlayFrozenSound = true;
			changeAnim = true;
		}
	}

	public void crystalizeLogic() {
		if (character.crystalizedTime == 0 && !character.isCrystalized) {
			return;
		}
		reduceStunFrames(ref character.crystalizedTime);
		character.crystalizeInvulnTime = 2;

		if (!character.isCrystalized && character.crystalizedTime > 0) {
			changeAnim = true;
			character.crystalizeStart();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StartCrystalize);
		}
		else if (character.isCrystalized && character.crystalizedTime == 0) {
			changeAnim = true;
			character.crystalizeEnd();
			Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StopCrystalize);
		}
	}

	public void paralizeAnimLogic() {
		if (character.paralyzedTime == 0) {
			return;
		}
		if (canPlayStaticSound) {
			character.playSound("voltcStatic");
			canPlayStaticSound = false;
		}
		reduceStunFrames(ref character.paralyzedTime);
		character.stunInvulnTime = 2;

		if (paralyzeAnim == null && character.paralyzedTime > 0) {
			paralyzeAnim = new Anim(
				character.getCenterPos(), "vile_stun_static",
				1, character.player.getNextActorNetId(), false,
				host: character, sendRpc: true
			);
			paralyzeAnim.setzIndex(character.zIndex + 100);
		}
		if (character.paralyzedTime == 0) {
			changeAnim = true;
			canPlayStaticSound = true;
			if (paralyzeAnim != null) {
				paralyzeAnim.destroySelf();
				paralyzeAnim = null;
			}
		}
	}

	public string getStunAnim() {
		if (character.frozenTime > 0) {
			return "frozen";
		}
		if (character.isCrystalized) {
			return "idle";
		}
		if (character.paralyzedTime > 0 && character.grounded) {
			return "lose";
		}
		return "hurt";
	}

	public void activateFlinch(int flinchFrames, int xDir) {
		if (flinchFrames > 0) character.tryActivateEscapeBoost();
		if (character.escapeBoostTime > 0) flinchFrames = Math.Min(flinchFrames, Global.halfFlinch);
		hurtDir = xDir;
		if (character is MegamanX mmx && mmx.chestArmor == ArmorId.Light) {
			flinchFrames = MathInt.Floor(flinchFrames * 0.75f);
		}
		if (flinchTime > flinchFrames) {
			return;
		}
		if (character.paralyzedTime >= 3) {
			character.paralyzedTime = 2;
		}
		bool isCombo = (flinchTime != 0);
		hurtSpeed = 1.6f * xDir;
		if (!isCombo) {
			flinchYPos = character.pos.y;
		}
		if (flinchFrames >= 2) {
			character.vel.y = -0.125f * (flinchFrames - 1) * 60f;
			if (isCombo && character.pos.y < flinchYPos) {
				character.vel.y = (0.002f * flinchTime - 0.076f) * (flinchYPos - character.pos.y) + 1;
			}
		}
		flinchTime = flinchFrames;
		flinchMaxTime = flinchFrames;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		hurtDir = -character.xDir;
		// To continue the flinch if was flinched before the stun.
		if (oldState is Hurt hurtState) {
			hurtDir = hurtState.hurtDir;
			hurtSpeed = hurtState.hurtSpeed;
			flinchTime = hurtState.flinchTime - hurtState.stateFrames;
			flinchYPos = hurtState.flinchYPos;
			if (flinchTime < 0) {
				flinchTime = 0;
			}
		}
	}

	public override void onExit(CharState? newState) {
		if (paralyzeAnim != null) {
			paralyzeAnim.destroySelf();
			paralyzeAnim = null;
		}
		if (character.crystalizedTime != 0 || character.isCrystalized) {
			character.crystalizeEnd();
			Global.serverClient?.rpc(
				RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StopCrystalize
			);
		}
		character.paralyzedTime = 0;
		character.frozenTime = 0;
		character.crystalizedTime = 0;

		base.onExit(newState);
	}

	public void reduceStunFrames(ref float arg) {
		arg -= player.mashValue() * 60f;
		if (arg <= 0) {
			arg = 0;
		}
	}

	public float getTimerFalloff() {
		float healthPercent = (float)(character.health / character.maxHealth);
		return (Global.speedMul * (2 + healthPercent));
	}
}

public class KnockedDown : CharState {
	public int hurtDir;
	public float hurtSpeed;
	public float flinchTime;
	readonly bool retryRecovery;
	public KnockedDown(int dir, bool retryRecovery = false) : base("knocked_down") {
		this.retryRecovery = retryRecovery;
		hurtDir = dir;
		hurtSpeed = dir * 100;
		flinchTime = 0.5f;
	}

	public override bool canEnter(Character character) {
		if (retryRecovery) return true;
		if (character.isStatusImmune()) return false;
		if (character.isFlinchImmune()) return false;
		if (character.isInvulnerable()) return false;
		if (character.vaccineTime > 0) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel.y = retryRecovery ? 0 : -100;
		if (!retryRecovery && character.escapeBoostTime > 0) flinchTime = MathF.Min(flinchTime, Global.halfFlinch / 60f);
	}

	public override void update() {
		base.update();
		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 400 * Global.spf, hurtDir);
			character.move(new Point(hurtSpeed, 0));
		}

		if (!retryRecovery && character.canCharge() && player.input.isHeld(Control.Shoot, player)) {
			character.increaseCharge();
		}

		if (stateTime >= flinchTime) {
			character.changeToIdleOrFall();
		}
	}
}

public class GoliathDragged : CharState {
	public RideArmor goliath;
	public GoliathDragged(RideArmor goliath) : base("hurt") {
		this.goliath = goliath;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel.y = 0;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}

	public override void update() {
		base.update();

		var goliathDash = goliath.rideArmorState as RADash;
		if (goliathDash == null || !goliath.isAttacking()) {
			character.changeToIdleOrFall();
			return;
		}

		character.move(goliathDash.getDashVel());
	}
}
