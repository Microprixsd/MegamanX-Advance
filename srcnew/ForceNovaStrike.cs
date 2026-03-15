using System;

namespace MMXOnline;

public class ForceNovaStrike : Weapon {
	public const float ammoUsage = 16;
	public static ForceNovaStrike netWeapon = new();

	public ForceNovaStrike() {
		fireRate = 90;
		index = (int)WeaponIds.ForceNovaStrike;
		weaponBarBaseIndex = 42;
		weaponBarIndex = 36;
		weaponSlotIndex = 95;
		killFeedIndex = 104;
		ammo = 0f;
		maxAmmo = 32;
		shootSounds = new string[] { "", "", "", "" };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		useForceHelmetBuff = false;
	}

	public override void shoot(Character character, int[] args) {
		base.shoot(character, args);

		character.changeState(new ForceNovaStrikeStart(), true);
	}


	public override float getAmmoUsage(int chargeLevel) {
		return ammoUsage;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return player.character?.flag == null && ammo >= ammoUsage;
	}
}


public class ForceNovaStrikeStart : CharState {
	public ForceNovaStrikeStart() : base("nova_strike_start") {
		superArmor = true;
		enterSound = "land";
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) character.changeState(new ForceNovaStrikeState());
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.xPushVel = character.xDir * 3;
		character.vel.y = -character.getJumpPower() * 0.5f;
	}
}

public class ForceNovaStrikeState : CharState {
	private int leftOrRight = 1;

	public ForceNovaStrikeState() : base("nova_strike") {
		pushImmune = true;
		superArmor = true;
		useGravity = false;
		enterSound = "novaStrikeX4";
	}

	public override void update() {
		base.update();

		if (
			character.flag != null || stateTime > 0.6f || 
			!character.tryMove(new Point(character.xDir * 350 * leftOrRight, 0), out _)
		) {
			character.changeToIdleOrFall();
			
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel.y = 0;
		character.stopCharge();
		character.frameIndex = 4;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.yDir = 1;
	}
}
