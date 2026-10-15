using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HyperNovaStrike : Weapon {
	public static HyperNovaStrike netWeapon = new();
	public const float ammoUsage = 0;
	private static readonly List<Type> wallCollisionTypes = new() { typeof(Wall) };

	public HyperNovaStrike() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		shootSounds = new string[] { "", "", "", "" };
		fireRate = 240;
		switchCooldown = 15;
		index = (int)WeaponIds.NovaStrike;
		weaponBarBaseIndex = 42;
		weaponBarIndex = 36;
		weaponSlotIndex = 95;
		killFeedIndex = 104;
		ammo = 16;
		maxAmmo = 16;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		useForceHelmetBuff = false;
	}

	public override void shoot(Character character, int[] args) {
		if (character.flag != null) return;
		if (Options.main.novaStrikeWall && Global.level.raycast(character.pos,
			character.pos.addxy(35 * character.xDir, 0), wallCollisionTypes) != null) return;
		if (Options.main.novaStrikeCeiling && Global.level.raycast(character.pos,
			character.pos.addxy(0, -40), wallCollisionTypes) != null) return;
		if (Options.main.novaStrikeFloor && character.player.input.isHeld(Control.Down, character.player) &&
			Global.level.raycast(character.pos, character.pos.addxy(0, 25), wallCollisionTypes) != null) return;

		if (character.ownedByLocalPlayer) {
			if (character.player.input.isHeld(Control.Up, character.player)) {
				character.changeState(new NovaStrikeStateUpEX(), true);
			} else if (character.player.input.isHeld(Control.Down, character.player)) {
				character.changeState(new NovaStrikeStateDownEX(), true);
			} else {
				character.changeState(new NovaStrikeStateEX(), true);
			}
			character.currentWeapon?.addAmmo(-ammoUsage, character.player);
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return player.character?.flag == null && ammo >= ammoUsage;
	}
}

public class NovaStrikeState : CharState {
	int upOrDown;
	int leftOrRight;
	Anim? Nova;
	public NovaStrikeState(Point inputDir) : base("nova_strike") {
		pushImmune = true;
		invincible = true;
		normalCtrl = false;
		attackCtrl = false;
		useGravity = false;

		if (inputDir.y != 0) upOrDown = (int)inputDir.y;
		else leftOrRight = 1;
	}

	public override void update() {
		base.update();

		if (!once && character.frameIndex >= 4) {
			once = true;
			if (Helpers.randomRange(0, 10) < 10) {
				character.playSound("novaStrikeX4", forcePlay: false, sendRpc: true);
			} else {
				character.playSound("novaStrikeX6", forcePlay: false, sendRpc: true);
			}
		}
		if (Nova != null) {
			Nova.incPos(character.deltaPos);
			if (character.xDir == 1) {
				if (upOrDown == 1) {
					Nova.angle = 90;
				} else if (upOrDown == -1) {
					Nova.angle = 270;
				}
			} else if (character.xDir == -1) {
				if (upOrDown == 1) {
					Nova.angle = 270;
				} else if (upOrDown == -1) {
					Nova.angle = 90;
				}
			}
		}
		if (character.frameIndex >= 4) {
			if (!character.tryMove(new Point(character.xDir * 350 * leftOrRight, 350 * upOrDown), out _) ||
				character.flag != null || stateTime > 0.6f
			) {
				character.changeToIdleOrFall();
				return;
			}
		}
		else if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.stopCharge();
		character.visible = false;
		Nova = new Anim(
			character.getCenterPos().addxy(character.xDir*8, 10),
			"mmx_nova_strike", character.xDir, player.getNextActorNetId(), false, sendRpc: true
		);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.yDir = 1;
		character.visible = true;
		Nova?.destroySelf();
	}

}

public class NovaStrikeStateEX : CharState {
	public NovaStrikeStateEX() : base("nova_strike") {
		pushImmune = true;
		invincible = true;
		useDashJumpSpeed = true;
		normalCtrl = false;
		attackCtrl = false;
	}

	public override void update() {
		base.update();

		if (!once && character.frameIndex >= 4) {
			once = true;
			character.playSound("NovaStrikeX4", sendRpc: true);
			if (Helpers.randomRange(0, 1) == 0) {
				character.playSound("mvsci_ultimatestrike", forcePlay: false, sendRpc: true);
			} 
		}
		if (character.frameIndex <= 2) {
			character.move(new Point(125 * character.xDir, -95));
			character.unstickFromGround();
		}
		if (character.frameIndex >= 4) {
			character.move(new Point(350 * character.xDir, 0));
		}
		CollideData? collideData = Global.level.checkTerrainCollisionOnce(character, character.xDir, 0);
		if (collideData != null && collideData.isSideWallHit() && character.ownedByLocalPlayer) {
			character.changeToIdleOrFall();
		}
		if (stateTime > 36f / 60f) {
			character.changeToIdleOrFall();
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
public class NovaStrikeStateUpEX : CharState {
	public NovaStrikeStateUpEX() : base("nova_strike_up") {
		pushImmune = true;
		invincible = true;
		useDashJumpSpeed = true;
		normalCtrl = false;
		attackCtrl = false;
	}

	public override void update() {
		base.update();
		if (!once && character.frameIndex >= 4) {
			once = true;
			character.playSound("NovaStrikeX4", sendRpc: true);
			if (Helpers.randomRange(0, 1) == 0) {
				character.playSound("mvsci_ultimatestrike", forcePlay: false, sendRpc: true);
			} 
		}
		if (character.frameIndex <= 3) {
			character.move(new Point(0, -25));
			character.unstickFromGround();
		}
		if (character.frameIndex >= 4) {
			character.move(new Point(0, -350));
		}
		var hitWall = Global.level.raycast(character.pos, character.pos.addxy(0, -40), new List<Type>() { typeof(Wall) });
		if (hitWall != null) {
			character.changeToIdleOrFall();
		}
		if (stateTime > 36f / 60f) {
			character.changeToIdleOrFall();
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
public class NovaStrikeStateDownEX : CharState {
	public NovaStrikeStateDownEX() : base("nova_strike_down") {
		pushImmune = true;
		invincible = true;
		useDashJumpSpeed = true;
		normalCtrl = false;
		attackCtrl = false;
	}

	public override void update() {
		base.update();
		if (!once && character.frameIndex >= 4) {
			once = true;
			character.playSound("NovaStrikeX4", sendRpc: true);
			if (Helpers.randomRange(0, 1) == 0) {
				character.playSound("mvsci_ultimatestrike", forcePlay: false, sendRpc: true);
			} 
		}
		if (character.frameIndex <= 3) {
			character.move(new Point(0, 25));
			character.unstickFromGround();
		}
		if (character.frameIndex >= 4) {
			character.move(new Point(0, 350));
		}
		var hitWall = Global.level.raycast(character.pos,character.pos.addxy(0, 10),new List<Type>() { typeof(Wall) });
		if (hitWall != null) {
			character.changeToIdleOrFall();
		}
		if (stateTime > 36f / 60f) {
			character.changeToIdleOrFall();
		}
	}
	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}
