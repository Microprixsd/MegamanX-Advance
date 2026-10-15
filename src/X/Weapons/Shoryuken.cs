namespace MMXOnline;

public class ShoryukenWeapon : Weapon {
	public static ShoryukenWeapon netWeapon = new();

	public ShoryukenWeapon() : base() {
		ammo = 0;
		index = (int)WeaponIds.Shoryuken;
		weaponBarBaseIndex = 32;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 32;
	}
}

public class Shoryuken : CharState {
	public bool grabRequested;
	bool jumpedYet;
	float timeInWall;
	bool isUnderwater;
	Anim? anim;
	float projTime;
	MegamanX? mmx;

	public Shoryuken(bool isUnderwater) : base("shoryuken") {
		this.isUnderwater = isUnderwater;
		superArmor = true;
	}

	public override void update() {
		base.update();

		if (character.isUnderwater() && anim != null) {
			anim.visible = false;
		}
		if (character.sprite.frameIndex == 1) {
			character.sprite.frameIndex = 2;
		}
		if (character.sprite.frameIndex >= 2 && !jumpedYet) {
			jumpedYet = true;
			character.dashedInAir++;
			character.vel.y = -character.getJumpPower() * 1.55f;
			if (character is not RagingChargeX) character.playSound("shoryukenV", sendRpc: true);
		}
		if (character.sprite.frameIndex == 2 && character.currentFrame.POIs.Length > 0) {
			character.move(new Point(character.xDir * 165, 0));
			Point poi = character.currentFrame.POIs[0];
			Point firePos = character.pos.addxy(poi.x * character.xDir, poi.y);
			if (anim == null) {
				anim = new Anim(firePos, "shoryuken", character.xDir, player.getNextActorNetId(), false, sendRpc: true);
			} else {
				anim.changePos(firePos);
			}
		} else if (character.sprite.frameIndex > 2) {
			if (anim != null) {
				anim.destroySelf();
				anim = null;
			}
		}

		if (!isUnderwater) {
			projTime += Global.spf;
			if (projTime > 0.06f) {
				projTime = 0;
				var anim = new Anim(character.getCenterPos(), "shoryuken_fade", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
				anim.vel = new Point(-character.xDir * 50, 25);
			}
		}

		var wallAbove = Global.level.checkTerrainCollisionOnce(character, 0, -10);
		if (wallAbove != null && wallAbove.gameObject is Wall) {
			timeInWall += Global.spf;
			if (timeInWall > 0.2f) {
				character.changeState(character.getFallState());
				return;
			}
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX;
	}

	public override void onExit(CharState? newState) {
		if (anim != null) {
			anim.destroySelf();
			anim = null;
		}
		base.onExit(newState);
		if (mmx != null) mmx.shoryukenCooldownTime = mmx.maxShoryukenCooldownTime;
	}
}

public class ShoryukenMeleeProj : GenericMeleeProj {
	public ShoryukenMeleeProj(Point pos, MegamanX x, bool addToLevel) : base(
		ShoryukenWeapon.netWeapon, pos, ProjIds.Shoryuken, x.player,
		5, Global.superFlinch, 60, owningActor: x, addToLevel: addToLevel
	) { }

	// Characters take damage once, when the grab ends. Other targets take a normal hit.
	public override bool shouldDealDamage(IDamagable target) => target is not Character && base.shouldDealDamage(target);

	public override void onHitDamagable(IDamagable target) {
		if (ownerActor is not MegamanX x || target is not Character victim ||
			x.health <= 0 || victim.health <= 0 || victim.player.alliance == owner.alliance || !victim.canBeGrabbed()) return;
		if (owner.isDefenderFavored || Global.isOffline) {
			if (victim.ownedByLocalPlayer && x.sprite.name == "mmx_shoryuken")
				victim.changeState(new ShoryukenGrabbed(x));
		} else if (ownedByLocalPlayer && x.charState is Shoryuken state && !state.grabRequested) {
			state.grabRequested = true;
			RPC.commandGrabPlayer.sendRpc(x.netId, victim.netId, CommandGrabScenario.Shoryuken, false);
		}
	}
}

public class ShoryukenGrabbed : GenericGrabbedState {
	readonly Character attacker;
	bool finished;

	public ShoryukenGrabbed(Character attacker) : base(attacker, 1, "shoryuken") {
		this.attacker = attacker;
		customUpdate = true;
	}

	public override bool canEnter(Character character) => character.ownedByLocalPlayer &&
		character.health > 0 && character.canBeGrabbed() && character.charState is not ShoryukenGrabbed && base.canEnter(character);

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer || finished) return;
		bool interrupted = attacker.destroyed || attacker.health <= 0;
		if (interrupted || attacker.sprite.name != "mmx_shoryuken" || attacker.frameIndex > 2 || stateTime >= 1 || grabTime <= 0) {
			finish(!interrupted);
			return;
		}
		Collider? hitbox = attacker.getAllColliders().Find(c => c.isAttack());
		if (hitbox == null) {
			finish(true);
			return;
		}
		// Anchor the victim's body to the punch, rather than preserving the
		// vertical gap from the first frame of contact.
		Point victimCenter = character.getGlobalCollider()?.shape.getRect().center() ?? character.getCenterPos();
		Point delta = hitbox.shape.getRect().center().subtract(victimCenter);
		if (Global.level.checkTerrainCollisionOnce(character, delta.x, delta.y)?.gameObject is Wall) {
			finish(true);
			return;
		}
		character.move(delta, useDeltaTime: false);
	}

	void finish(bool hit) {
		finished = true;
		character.changeToIdleOrFall();
		if (!hit || character.health <= 0) return;
		character.applyDamage(5, attacker.player, attacker, ShoryukenWeapon.netWeapon.killFeedIndex, (int)ProjIds.Shoryuken);
		if (character.health > 0) character.setHurt(-attacker.xDir, Global.superFlinch, false);
		character.forceNetUpdateNextFrame = true;
	}
}
