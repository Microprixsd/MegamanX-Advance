using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SoulBodyClone : MegamanX {
	MegamanX owner = null!;
	float distance = 0;
	const float maxDist = 64;
	bool canMoveClone = false;
	float lifeTime = 300;
	bool plasma;

	public SoulBodyClone(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.SoulBodyClone;
		owner = player.character as MegamanX ?? throw new NullReferenceException();
		owner.sBodyClone = this;
		player.sClone = this;
		pos = owner.pos;
		changeState(new Idle(), true);
		base.player = player;

		weapons = [new XBuster()];
		weaponSlot = 0;

		maxHealth = 8;
		health = maxHealth;
	}

	public override void update() {
		base.update();
		if (distance < maxDist) distance += 4;
		else {
			distance = maxDist;
			canMoveClone = true;
		} 

		if (!canMoveClone) changePos(owner.pos.addxy(owner.getShootXDir() * distance, 0));
		if (canMoveClone) Helpers.decrementFrames(ref lifeTime);

		if (lifeTime <= 0 || health <= 0) destroySelf();
	}

	public override bool canCharge() {
		return false;
	}

	public override bool canChangeWeapons() {
		return false;
	}

	/*public override void applyDamage(float fDamage, Player? attacker, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;
		decimal damage = decimal.Parse(fDamage.ToString());
		MegamanX? mmx = this as MegamanX;

		if (damage > 0 && actor != null && attacker != null && health > 0) {
			health -= fDamage;
			//playSound("hit", sendRpc: true);

			base.applyDamage(fDamage, attacker, actor, weaponIndex, projId);
			return;
		}
	} */

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;
		int index = (int)WeaponIds.SoulBody;
		palette = player.xPaletteShader;

		palette?.SetUniform("palette", index);

		if (palette != null) {
			shaders.Add(palette);
		}
		
		return shaders;
	}

	public override void onDestroy() {
		base.onDestroy();

		player.character = owner;
		owner.sBodyClone = null!;
		player.sClone = null!;
		owner.changeToIdleOrFall();
	}
}
