using System;
using System.Linq;
using Lidgren.Network;

namespace MMXOnline;

public partial class Player {
	public const float hyperModeCapacity = 16 * 21;
	public const float hyperModeDuration = 20;
	public float hyperModeCharge { get; private set; }
	public float hyperModeTime { get; private set; }
	public bool hyperModeReady => hyperModeCharge >= hyperModeCapacity && hyperModeTime <= 0;

	public void awardHyperDamage(float damage, bool sendRpc = false) {
		if (realCharNum is not (0 or 1) || !float.IsFinite(damage) || damage <= 0 || damage >= Damager.ohkoDamage ||
			Global.level.gameMode.isOver) return;
		if (!ownedByLocalPlayer) {
			if (sendRpc) RPC.hyperModeDamage.sendRpc(this, damage);
			return;
		}
		float chargeRate = Global.level.server.customMatchSettings?.hyperModeChargeRate ?? 1f;
		chargeRate = float.IsFinite(chargeRate) ? Math.Clamp(chargeRate, 0.25f, 10f) : 1f;
		if (realCharNum == 1) {
			awardZeroHyperDamage(damage, chargeRate);
			return;
		}
		if (hyperModeTime > 0) return;
		hyperModeCharge = Math.Min(hyperModeCapacity, hyperModeCharge + damage * 48 * chargeRate / Math.Max(1, getMaxHealth()));
	}

	public void refillHyperModeOnKill() {
		if (ownedByLocalPlayer && realCharNum == 1 && character is Zero zero &&
			zero.health > 0 && zero.hyperModeTimer > 0 && !Global.level.gameMode.isOver) {
			zero.hyperModeTimer = Math.Min(Zero.maxBlackZeroTime, zero.hyperModeTimer + Zero.maxBlackZeroTime / 3f);
			return;
		}
		if (!ownedByLocalPlayer || realCharNum != 0 || hyperModeTime <= 0 ||
			character is not MegamanX x || x.health <= 0 || Global.level.gameMode.isOver) return;
		hyperModeTime = Math.Min(hyperModeDuration, hyperModeTime + hyperModeDuration * 0.5f);
	}

	public bool activateHyperMode() {
		if (!ownedByLocalPlayer || !hyperModeReady || character is not MegamanX x || x.health <= 0 ||
			Global.level.gameMode.isOver) return false;
		hyperModeCharge = 0;
		hyperModeTime = hyperModeDuration;
		x.hasUltimateArmor = true;
		removeForceNovaStrike();
		if (!weapons.Any(w => w is HyperNovaStrike)) weapons.Add(new HyperNovaStrike());
		x.forceNetUpdateNextFrame = true;
		return true;
	}

	void updateHyperMode() {
		if (!ownedByLocalPlayer || hyperModeTime <= 0) return;
		hyperModeTime = Math.Max(0, hyperModeTime - Global.spf);
		if (character is not MegamanX x || realCharNum != 0) return;
		if (hyperModeTime > 0) {
			// The timer belongs to the player and keeps running through death/respawn.
			if (!x.hasUltimateArmor) {
				x.hasUltimateArmor = true;
				removeForceNovaStrike();
				if (!weapons.Any(w => w is HyperNovaStrike)) weapons.Add(new HyperNovaStrike());
				x.forceNetUpdateNextFrame = true;
			}
			return;
		}
		x.hasUltimateArmor = false;
		removeNovaStrike();
		if (x.chestArmor == ArmorId.Force) addForceNovaStrike();
		x.forceNetUpdateNextFrame = true;
	}

	public void drawHyperMode(float x, float y) {
		if (!Global.sprites.TryGetValue("hyper_icons", out var icons)) return;
		bool active = hyperModeTime > 0;
		float remaining = hyperModeTime;
		bool ready = realCharNum == 1 ? zeroHyperModeReady : hyperModeReady;
		float chargeRatio = realCharNum == 1
			? zeroHyperModeCharge / zeroHyperModeCapacity : hyperModeCharge / hyperModeCapacity;
		float duration = realCharNum == 1 ? Zero.maxBlackZeroTime / 60f : hyperModeDuration;
		if (character is Zero zero) {
			active = zero.hypermodeActive() || zero.charState is HyperZeroStart;
			remaining = zero.hyperModeTimer > 0 ? zero.hyperModeTimer / 60f : active ? duration : 0;
		}
		remaining = Math.Clamp(remaining, 0, duration);
		int frame = active ? 1 + (int)((duration - remaining) / 0.066f) % 5 : 0;
		icons.drawToHUD(frame, x, y, active || ready ? 1 : 0.35f);
		float ratio = active ? remaining / duration : chargeRatio;
		int percent = active ? (int)MathF.Ceiling(ratio * 100) : (int)MathF.Floor(ratio * 100);
		Fonts.drawText(FontType.Yellow, $"{percent}%", x + 3, y + 11, Alignment.Center);
	}
}

public partial class Character {
	bool hyperModeKillCredited;

	void creditHyperModeKill(Player killer) {
		if (hyperModeKillCredited || !killer.ownedByLocalPlayer || killer.alliance == player.alliance) return;
		hyperModeKillCredited = true;
		killer.refillHyperModeOnKill();
	}
}

public class RPCHyperModeDamage : RPC {
	public RPCHyperModeDamage() { netDeliveryMethod = NetDeliveryMethod.ReliableOrdered; }
	public override void invoke(params byte[] args) {
		if (args.Length != 5) return;
		Player? player = Global.level.getPlayerById(args[0]);
		if (player?.ownedByLocalPlayer == true) player.awardHyperDamage(BitConverter.ToSingle(args, 1));
	}
	public void sendRpc(Player player, float damage) {
		byte[] data = new byte[5];
		data[0] = (byte)player.id;
		BitConverter.TryWriteBytes(data.AsSpan(1), damage);
		Global.serverClient?.rpc(this, data);
	}
}
