using System;
using Newtonsoft.Json;

namespace MMXOnline;

public sealed class XQuickArmorSet {
	public int helmet, body, arms, legs;
	public int getPart(int part) => Math.Clamp(part switch {
		0 => helmet, 1 => body, 2 => arms, _ => legs
	}, 0, part == 2 ? 5 : 4);
	public void setPart(int part, int armor) {
		armor = Math.Clamp(armor, 0, part == 2 ? 5 : 4);
		switch (part) {
			case 0: helmet = armor; break;
			case 1: body = armor; break;
			case 2: arms = armor; break;
			case 3: legs = armor; break;
		}
	}
	[JsonIgnore] public bool mixed => getPart(0) != getPart(1) ||
		getPart(0) != getPart(2) || getPart(0) != getPart(3);
	[JsonIgnore] public int iconFrame => mixed || getPart(0) == 0 ? 4 : getPart(0) - 1;
	public static XQuickArmorSet[] defaults() => new[] { full(1), full(2), full(3), full(4) };
	static XQuickArmorSet full(int armor) => new() { helmet = armor, body = armor, arms = armor, legs = armor };
	public static XQuickArmorSet get(int index) {
		var sets = Options.main.quickArmorSets;
		if (sets == null || sets.Length != 4) {
			var repaired = defaults();
			if (sets != null) Array.Copy(sets, repaired, Math.Min(sets.Length, 4));
			Options.main.quickArmorSets = sets = repaired;
		}
		return sets[index] ??= full(index + 1);
	}
}
