using System;

namespace MMXOnline;

public partial class MegamanX {
	int skillLemonCount;
	int skillLemonThreshold;
	bool skillAutoCharge => player.xSkills.has(XSkill.UltimateBuster);
	bool isSkillDashState => charState is Dash or AirDash or LightDash or GigaAirDash or UpDash;
	public float skillRunMultiplier => !isDashing && !isSkillDashState &&
		player.xSkills.has(grounded ? XSkill.Speedster1 : XSkill.Speedster2) ? 1.1f : 1;
	public float skillDashMultiplier => player.xSkills.has(grounded ? XSkill.HyperDash1 : XSkill.HyperDash2) ? 1.07f : 1;
	public float skillSaberCooldown => 60 + (player.xSkills.has(XSkill.ShotEraser) ? 30 : 0) +
		(player.xSkills.has(XSkill.SaberPlus3) ? 30 : 0);

	public bool consumeBusterPlusShot() {
		int threshold = player.xSkills.has(XSkill.BusterPlus3) ? 6 : player.xSkills.has(XSkill.BusterPlus2) ? 8 :
			player.xSkills.has(XSkill.BusterPlus1) ? 10 : 0;
		if (threshold != skillLemonThreshold) { skillLemonCount = 0; skillLemonThreshold = threshold; }
		if (threshold == 0) return false;
		if (++skillLemonCount <= threshold) return false;
		skillLemonCount = 0;
		return true;
	}

	GenericMeleeProj createSkillSaber(Point pos, bool addToLevel) {
		return new GenericMeleeProj(
			ZXSaber.netWeapon, pos,
			player.xSkills.has(XSkill.SaberPlus2) ? ProjIds.X6SaberPiercing : ProjIds.X6Saber, player,
			player.xSkills.has(XSkill.SaberPlus1) ? 3 : 2,
			player.xSkills.has(XSkill.SaberPlus3) ? Global.miniFlinch : 0, 30,
			isDeflectShield: player.xSkills.has(XSkill.ShotEraser),
			addToLevel: addToLevel, isZSaberEffect: true, clashTier: ClashTier.Weak
		);
	}
}
