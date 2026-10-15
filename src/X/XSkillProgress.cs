using System;

namespace MMXOnline;

[Flags]
public enum XSkill : uint {
	None = 0, LifeUp = 1, EnergyUp = 2, ShockBuffer1 = 4, ShockBuffer2 = 8,
	EnergySaver1 = 16, EnergySaver2 = 32, EnergySaver3 = 64,
	SuperRecover = 128, DConverter = 256,
	Speedster1 = 1 << 9, Speedster2 = 1 << 10, Jumper1 = 1 << 11, Jumper2 = 1 << 12,
	HyperDash1 = 1 << 13, HyperDash2 = 1 << 14, BusterPlus1 = 1 << 15,
	BusterPlus2 = 1 << 16, BusterPlus3 = 1 << 17, SpeedShot = 1 << 18,
	QuickCharge = 1 << 19, SaberPlus1 = 1 << 20, SaberPlus2 = 1 << 21,
	SaberPlus3 = 1 << 22, ShotEraser = 1 << 23, UltimateBuster = 1 << 24, DoubleBarrier = 1 << 25
}

public sealed partial class XSkillProgress {
	public const int initialPoints = 8;
	public const int maxPoints = 999;
	public const int maxSpentPoints = 90;
	int availablePoints = initialPoints;
	public int points { get => availablePoints; set => availablePoints = Math.Clamp(value, 0, maxPoints); }
	public int spentPoints { get; private set; }
	public static int getCost(string id) => id switch {
		"h1" or "h2" or "h3" or "h4" or "h5" => 3,
		"shield1" or "shield2" or "shield3" or "shield4" => 5,
		"lu" or "eu" or "sr" or "sb1" => 6,
		"sb2" => 7,
		"db" or "dc" or "ss" or "es3" => 4,
		"s1" or "s2" or "j1" or "j2" or "es1" or "bp1" or "sp1" => 2,
		"hd1" or "hd2" or "es2" or "bp2" or "bp3" or "sp2" or "sp3" or "se" => 3,
		"ub" or "qc" => 5,
		_ => 0
	};
	public XSkill purchased;
	public bool has(XSkill skill) => skill != XSkill.None && (purchased & skill) == skill;
	public int energySaverLevel => has(XSkill.EnergySaver3) ? 3 : has(XSkill.EnergySaver2) ? 2 : has(XSkill.EnergySaver1) ? 1 : 0;
	public static XSkill fromNode(string id) => id switch {
		"lu" => XSkill.LifeUp, "eu" => XSkill.EnergyUp,
		"sb1" => XSkill.ShockBuffer1, "sb2" => XSkill.ShockBuffer2,
		"es1" => XSkill.EnergySaver1, "es2" => XSkill.EnergySaver2, "es3" => XSkill.EnergySaver3,
		"sr" => XSkill.SuperRecover, "dc" => XSkill.DConverter,
		"s1" => XSkill.Speedster1, "s2" => XSkill.Speedster2,
		"j1" => XSkill.Jumper1, "j2" => XSkill.Jumper2,
		"hd1" => XSkill.HyperDash1, "hd2" => XSkill.HyperDash2,
		"bp1" => XSkill.BusterPlus1, "bp2" => XSkill.BusterPlus2, "bp3" => XSkill.BusterPlus3,
		"ss" => XSkill.SpeedShot, "qc" => XSkill.QuickCharge,
		"sp1" => XSkill.SaberPlus1, "sp2" => XSkill.SaberPlus2, "sp3" => XSkill.SaberPlus3,
		"se" => XSkill.ShotEraser, "ub" => XSkill.UltimateBuster, "db" => XSkill.DoubleBarrier, _ => XSkill.None
	};
	// At forks, owning either incoming route unlocks the next node.
	static XSkill prerequisite(XSkill skill) => skill switch {
		XSkill.DoubleBarrier => XSkill.HyperDash2,
		XSkill.SaberPlus1 or XSkill.SpeedShot or XSkill.QuickCharge => XSkill.BusterPlus1,
		XSkill.ShotEraser => XSkill.SaberPlus1,
		XSkill.UltimateBuster => XSkill.BusterPlus2 | XSkill.SpeedShot | XSkill.QuickCharge,
		XSkill.Jumper1 => XSkill.Speedster2,
		XSkill.HyperDash1 => XSkill.Jumper1 | XSkill.Jumper2,
		XSkill.SuperRecover or XSkill.DConverter => XSkill.EnergySaver3,
		XSkill.EnergyUp => XSkill.SuperRecover | XSkill.DConverter | XSkill.LifeUp,
		XSkill.LifeUp => XSkill.EnergyUp,
		XSkill.ShockBuffer1 => XSkill.EnergyUp | XSkill.LifeUp,
		XSkill.Speedster2 => XSkill.Speedster1, XSkill.Jumper2 => XSkill.Jumper1,
		XSkill.HyperDash2 => XSkill.HyperDash1, XSkill.BusterPlus2 => XSkill.BusterPlus1,
		XSkill.BusterPlus3 => XSkill.BusterPlus2, XSkill.SaberPlus2 => XSkill.SaberPlus1,
		XSkill.SaberPlus3 => XSkill.SaberPlus2, XSkill.EnergySaver2 => XSkill.EnergySaver1,
		XSkill.EnergySaver3 => XSkill.EnergySaver2, XSkill.ShockBuffer2 => XSkill.ShockBuffer1,
		_ => XSkill.None
	};

	int heartNodes, subNodes;
	int recordedHearts, recordedSubtanks;
	bool hasHeart(int tier) => (heartNodes & (1 << (tier - 1))) != 0;
	bool hasSubtank(int tier) => (subNodes & (1 << (tier - 1))) != 0;
	bool excludedHeart(int tier) => tier >= 3 && tier <= 5 && hasSubtank(tier - 1);
	bool excludedSubtank(int tier) => tier >= 2 && tier <= 4 && hasHeart(tier + 1);

	// Import starting tanks and purchases from the original menu. Counts represent
	// actual inventory, while the masks remember which mutually exclusive nodes won.
	void syncTankInventory(Player player) {
		while (recordedHearts < player.heartTanks) {
			for (int tier = 1; tier <= 5; tier++) {
				if (!hasHeart(tier) && !excludedHeart(tier)) { heartNodes |= 1 << (tier - 1); break; }
			}
			recordedHearts++;
		}
		while (recordedSubtanks < player.subtanks.Count) {
			for (int tier = 1; tier <= 4; tier++) {
				if (!hasSubtank(tier) && !excludedSubtank(tier)) { subNodes |= 1 << (tier - 1); break; }
			}
			recordedSubtanks++;
		}
	}

	public bool isPurchased(Player? player, string id) {
		if (player != null) syncTankInventory(player);
		if (id.Length == 2 && id[0] == 'h') return hasHeart(id[1] - '0');
		if (id.StartsWith("shield", StringComparison.Ordinal)) return hasSubtank(id[^1] - '0');
		return has(fromNode(id));
	}

	bool tankPrerequisite(bool heart, int tier) {
		if (heart && tier <= 2) return tier == 1 || hasHeart(1);
		if (!heart && tier == 1) return true;
		int pair = heart ? tier - 3 : tier - 2;
		return pair == 0 ? hasHeart(2) || hasSubtank(1) : hasHeart(pair + 2) || hasSubtank(pair + 1);
	}

	public static bool canBuyHeartTank(Player player) => canBuyTank(player, true);
	public static bool canBuySubTank(Player player) => canBuyTank(player, false);
	static bool canBuyTank(Player player, bool heart) {
		if (player.realCharNum != 0) return true;
		var skills = player.xSkills;
		skills.syncTankInventory(player);
		for (int tier = 1; tier <= (heart ? 5 : 4); tier++) {
			if (!(heart ? skills.hasHeart(tier) || skills.excludedHeart(tier) : skills.hasSubtank(tier) || skills.excludedSubtank(tier)) &&
				skills.tankPrerequisite(heart, tier)) return true;
		}
		return false;
	}

	public string purchaseStatus(Player? player, string id) {
		if (string.IsNullOrEmpty(id) || getCost(id) == 0) return "Coming soon";
		if (isPurchased(player, id)) return "Purchased";
		bool heart = id.Length == 2 && id[0] == 'h';
		bool tank = id.StartsWith("shield", StringComparison.Ordinal);
		XSkill skill = fromNode(id);
		if (!heart && !tank && skill == XSkill.None) return "Coming soon";
		if (player != null && (player.realCharNum != 0 || Global.level.is1v1())) return "Unavailable";
		if (player != null && (heart || tank) && Global.level.server.disableHtSt) return "Unavailable";
		if (heart && excludedHeart(id[^1] - '0') ||
			tank && excludedSubtank(id[^1] - '0')) return "Excluded";
		if (heart && (!tankPrerequisite(true, id[^1] - '0') || (player != null && player.heartTanks >= UpgradeMenu.getMaxHeartTanks()))) return "Locked";
		if (tank && (!tankPrerequisite(false, id[^1] - '0') || (player != null && player.subtanks.Count >= UpgradeMenu.getMaxSubTanks()))) return "Locked";
		XSkill required = prerequisite(skill);
		bool completedTankBranch = skill == XSkill.LifeUp && (hasHeart(5) || hasSubtank(4));
		if (required != XSkill.None && (purchased & required) == 0 && !completedTankBranch) return "Locked";
		int cost = getCost(id);
		if (cost > maxSpentPoints - spentPoints) return "Spend limit";
		return points < cost ? "Not enough SP" : "Buy";
	}

	public bool purchase(Player player, string id) {
		if (!player.ownedByLocalPlayer || purchaseStatus(player, id) != "Buy") return false;
		decimal previousMax = player.character is MegamanX currentX ? currentX.maxHealth : 0;
		int cost = getCost(id);
		points -= cost;
		spentPoints += cost;
		if (id.Length == 2 && id[0] == 'h') {
			heartNodes |= 1 << (id[^1] - '1');
			player.heartTanks++;
			recordedHearts = player.heartTanks;
			if (player.character is MegamanX x) x.heartTanks = player.heartTanks;
		} else if (id.StartsWith("shield", StringComparison.Ordinal)) {
			subNodes |= 1 << (id[^1] - '1');
			player.subtanks.Add(new SubTank());
			recordedSubtanks = player.subtanks.Count;
		} else {
			purchased |= fromNode(id);
		}
		// UPX and other transformed characters keep their own health limits.
		if (player.character == null) player.maxHealth = player.getMaxHealth();
		if (player.character is MegamanX mmx) {
			mmx.maxHealth = mmx.getMaxHealth();
			if (mmx.maxHealth > previousMax && mmx.health > 0) mmx.addHealth(mmx.maxHealth - previousMax, false);
			mmx.applySkillWeaponBonuses();
			mmx.forceNetUpdateNextFrame = true;
		}
		Global.playSound("upgrade");
		return true;
	}

	// Preview only: uses the same dependencies, exclusions and spending cap without a match.
	public bool planPurchase(string id) {
		if (purchaseStatus(null, id) != "Buy") return false;
		int cost = getCost(id);
		points -= cost;
		spentPoints += cost;
		if (id.Length == 2 && id[0] == 'h') heartNodes |= 1 << (id[^1] - '1');
		else if (id.StartsWith("shield", StringComparison.Ordinal)) subNodes |= 1 << (id[^1] - '1');
		else purchased |= fromNode(id);
		return true;
	}
}

public partial class Player {
	public readonly XSkillProgress xSkills = new();
}

public partial class MegamanX {
	decimal converterDamage;
	public static bool isSkillAmmoWeapon(Weapon weapon) => weapon.canHealAmmo &&
		weapon is not XBuster and not GigaCrush and not HyperNovaStrike and not ForceNovaStrike and not HyperCharge;

	public void applySkillWeaponBonuses(bool spawning = false) {
		int bonus = player.xSkills.has(XSkill.EnergyUp) ? 4 : 0;
		float rechargeMultiplier = 1 - player.xSkills.energySaverLevel * 0.1f;
		foreach (Weapon weapon in weapons) {
			if (!isSkillAmmoWeapon(weapon)) continue;
			bool full = weapon.ammo >= weapon.maxAmmo;
			weapon.maxAmmo += bonus - weapon.xSkillCapacityBonus;
			weapon.xSkillCapacityBonus = bonus;
			weapon.ammo = spawning && full ? weapon.maxAmmo : Math.Min(weapon.ammo, weapon.maxAmmo);
			weapon.xSkillRechargeMultiplier = rechargeMultiplier;
		}
	}

	public void convertSkillDamage(decimal lostHealth) {
		if (!ownedByLocalPlayer || health <= 0 || lostHealth <= 0 || !player.xSkills.has(XSkill.DConverter)) return;
		converterDamage += lostHealth;
		int ammo = (int)(converterDamage / 8);
		if (ammo == 0) return;
		converterDamage %= 8;
		foreach (Weapon weapon in weapons) {
			if (isSkillAmmoWeapon(weapon)) weapon.addAmmo(ammo, player);
		}
	}
}
