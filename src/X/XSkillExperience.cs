using System;
using Lidgren.Network;

namespace MMXOnline;

public enum XSkillActivity : byte { Damage, Defense, Support, Objective }

public sealed partial class XSkillProgress {
	// MM7OD thresholds: 20 damage, 24 defense, 16 support, 30 map EXP.
	// A common denominator preserves fractional progress without rounding each event.
	public const decimal experiencePerLevel = 960;
	readonly int[] levels = { 1, 1, 1, 1 };
	readonly decimal[] experience = new decimal[4];
	public int getLevel(XSkillActivity activity) => levels[(int)activity];
	public decimal getExperiencePercent(XSkillActivity activity) => experience[(int)activity] * 100 / experiencePerLevel;

	public int addExperience(XSkillActivity activity, float amount) {
		if (!float.IsFinite(amount) || amount <= 0 || amount > 65535) return 0;
		decimal multiplier = activity switch {
			XSkillActivity.Damage => 48, XSkillActivity.Defense => 40,
			XSkillActivity.Support => 60, XSkillActivity.Objective => 32, _ => 0
		};
		if (multiplier == 0) return 0;
		int index = (int)activity;
		experience[index] += (decimal)amount * multiplier;
		int gained = (int)(experience[index] / experiencePerLevel);
		experience[index] %= experiencePerLevel;
		levels[index] += gained;
		int awarded = Math.Min(gained, maxPoints - points);
		points += awarded;
		return awarded;
	}
}

public partial class Player {
	public void awardSkillExperience(XSkillActivity activity, float amount, bool sendRpc = false) {
		if (realCharNum != 0 || character != null && character is not MegamanX ||
			!float.IsFinite(amount) || amount <= 0 || amount > 65535 || Global.level.gameMode.isOver) return;
		if (!ownedByLocalPlayer) {
			if (sendRpc) RPC.skillExperience.sendRpc(this, activity, amount);
			return;
		}
		int gained = xSkills.addExperience(activity, amount);
		if (gained > 0) character?.addDamageText($"+{gained} SP", (int)FontType.Yellow);
	}
}

public partial class Character {
	public int lastSkillObjectiveFrame = -1;
	public float skillStatusFrames => frozenTime + paralyzedTime + crystalizedTime +
		60 * (burnTime + acidTime + oilTime + virusTime + slowdownTime);

	public void creditSkillStatus(Player attacker, float addedFrames) {
		if (ownedByLocalPlayer && health > 0 && attacker != player && attacker != Player.stagePlayer &&
			attacker.alliance != player.alliance && addedFrames > 0) {
			attacker.awardSkillExperience(XSkillActivity.Support, addedFrames / 30f, true);
		}
	}
}

public class RPCSkillExperience : RPC {
	public RPCSkillExperience() { netDeliveryMethod = NetDeliveryMethod.ReliableOrdered; }
	public override void invoke(params byte[] args) {
		if (args.Length != 6 || args[1] > (byte)XSkillActivity.Objective) return;
		Player? player = Global.level.getPlayerById(args[0]);
		if (player?.ownedByLocalPlayer != true) return;
		player.awardSkillExperience((XSkillActivity)args[1], BitConverter.ToSingle(args, 2));
	}
	public void sendRpc(Player player, XSkillActivity activity, float amount) {
		byte[] data = new byte[6];
		data[0] = (byte)player.id;
		data[1] = (byte)activity;
		BitConverter.TryWriteBytes(data.AsSpan(2), amount);
		Global.serverClient?.rpc(this, data);
	}
}
