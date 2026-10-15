namespace MMXOnline;

public partial class Character {
	Point pitReturnPos;
	bool touchedHazard;

	public void applyTerrainHazard(bool pit) {
		if (!ownedByLocalPlayer || health <= 0 || charState is Die) return;
		bool onCooldown = !pit && projectileCooldown.TryGetValue("terrainHazard", out float cooldown) && cooldown > 0;
		if (!onCooldown) {
			bool retryWasArmed = retryChipArmed;
			if (!pit) projectileCooldown["terrainHazard"] = 180;
			applyDamage(12, null, null, null, null);
			if (health <= 0 || charState is Die) return;
			if (retryWasArmed && !retryChipArmed) {
				if (pit) {
					dropFlag();
					stopMovingS();
					changePos(pitReturnPos);
					grounded = true;
				}
				forceNetUpdateNextFrame = true;
				return;
			}
			playSound("hurt", sendRpc: true);
			addRenderEffect(RenderEffectType.Hit, 3, 6);
		}
		if (pit) {
			dropFlag();
			stopMovingS();
			changeState(new PitRecovery(), true);
			changePos(pitReturnPos);
			grounded = true;
			forceNetUpdateNextFrame = true;
		} else if (!onCooldown) {
			changeState(new Hurt(-xDir, Global.superFlinch), true);
		}
		// Grant after the hit reaction, and never refresh from contact during the spike cooldown.
		if (!onCooldown && this is MegamanX && player.xSkills.has(XSkill.DoubleBarrier)) {
			invulnTime = System.Math.Max(invulnTime, 0.6f);
			forceNetUpdateNextFrame = true;
		}
	}
}

public class PitRecovery : CharState {
	public PitRecovery() : base("crouch") {
		useGravity = false;
	}

	public override bool canEnter(Character character) => true;

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMovingS();
		character.globalCollider = character.getCrouchingCollider();
	}

	public override void update() {
		base.update();
		if (stateFrames >= 30) character.changeToIdleOrFall();
	}
}
