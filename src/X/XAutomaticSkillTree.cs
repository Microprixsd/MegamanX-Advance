namespace MMXOnline;

public partial class Player {
	float automaticSkillTreeCooldown;

	void updateAutomaticSkillTree() {
		if (!ownedByLocalPlayer || !isMainPlayer || isAI || !Options.main.automaticSkillTree ||
			Menu.inMenu || realCharNum != 0 || character is not MegamanX x || x.health <= 0) return;
		Helpers.decrementTime(ref automaticSkillTreeCooldown);
		if (automaticSkillTreeCooldown > 0) return;
		automaticSkillTreeCooldown = 0.25f;
		var route = Options.main.xSkillTreeRoute;
		if (route == null) return;
		foreach (string id in route) {
			if (string.IsNullOrEmpty(id) || XSkillProgress.getCost(id) == 0) break;
			if (xSkills.isPurchased(this, id)) continue;
			// Do not skip a blocked or unaffordable node: preserve the configured order.
			if (!xSkills.purchase(this, id)) break;
		}
	}
}
