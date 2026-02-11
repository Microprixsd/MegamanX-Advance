using System;
using System.Collections.Generic;

namespace MMXOnline;

public class GroundHunter : Weapon {

	public static GroundHunter netWeapon = new GroundHunter();

	public GroundHunter() : base() {
		index = (int)WeaponIds.GroundHunter;
		displayName = "Ground Hunter";
		fireRate = 30;
		switchCooldown = 30;
		weaponSlotIndex = 127;
		weaponBarIndex = 65;
		weaponBarBaseIndex = 76;
		shootSounds = new string[] { "busterX4", "busterX4", "busterX4", "buster2X4" };
		weaknessIndex = (int)WeaponIds.FrostTower;
		/* damage = "2/1-1";
		hitcooldown = "0/0.5-0";
		Flinch = "0";
		FlinchCD = hitcooldown;
		effect = "Press DOWN to change direction. C: Press UP or DOWN to spawn more projectiles."; */
		
		ammoDisplayScale = 1;
		maxAmmo = 16;
		ammo = maxAmmo;
	}
	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3 && ammo >= 6) {
			return 6;
		}
		return 1;
	}
	public override void update() {
		base.update();
    	if (ammo < maxAmmo) {
        	rechargeAmmo(2);
    	}
	}

	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		bool down = player.input.isHeld(Control.Down, player);

		if (chargeLevel >= 3 && ammo >= 6) {
			new GroundHunterChargedProj(mmx, pos, xDir, player.getNextActorNetId(), true, player);
		} else {
			new GroundHunterProj(mmx, pos, xDir, player.getNextActorNetId(), true, player) { downPressed = down };
		}
		
		rechargeCooldown = 1;
	}
}


public class GroundHunterAnim : Anim {
	
	GroundHunterProj gh;

	public GroundHunterAnim(
		Point pos, int xDir, Player player, GroundHunterProj gh
	) : base(
		pos, "ground_hunter_sparks", xDir, 
		player.getNextActorNetId(), false, true
	) {
		this.gh = gh;
	}

	public override void update() {
		base.update();

		if (gh == null || gh.destroyed) {
			destroySelf();
			return;
		}

		changePos(gh.pos);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!gh.destroyed) gh.sparks = null!;
	}
}


public class GroundHunterProj : Projectile {

	const float projSpeed = 200;
	public bool downPressed;
	bool down;
	Player? player;
	bool groundedOnce;
	public Anim? sparks;

	public GroundHunterProj(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "ground_hunter_proj", netId, player
	) {
		weapon = GroundHunter.netWeapon;
		projId = (int)ProjIds.GroundHunter;
		wallCrawlSpeed = projSpeed;
		maxDistance = 250;
		useGravity = false;
		vel.y = 25;
		fadeSprite = "ground_hunter_fade";
		fadeOnAutoDestroy = true;
		canBeLocal = false;

		vel.x = projSpeed * xDir;
		damager.damage = 2;

		if (ownedByLocalPlayer) this.player = player;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GroundHunterProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		//gravityModifier = Helpers.lerp(gravityModifier, 1, (Global.speedMul * 1.5f) / 60);
		
		updateWallCrawl();
		if (sparks != null) {
			sparks.changePos(pos);
		}

		if (downPressed && !down && !groundedOnce) {
			down = true;
			changeSprite("ground_hunter_fall", false);
			stopMoving();
			vel.y = Physics.MaxFallSpeed;
			moveDistance -= 125;
		}
		
		if (deltaPos.y > 0 && !down && groundedOnce) {
			down = true;
			changeSprite("ground_hunter_fall", false);
			if (sparks != null) {
				sparks.destroySelf();
				sparks = null!;
			}
		}
		else if (deltaPos.y < 0) destroySelf();
		else if (down && deltaPos.y == 0 && !downPressed) {
			down = false;
			changeSprite("ground_hunter_proj", false);
			
			if (sparks == null) {
				sparks = new GroundHunterAnim(
					pos, xDir, damager.owner, this
				);
			}
		}

		downPressed = player?.input.isPressed(Control.Down, player) == true;
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		vel.x = 0;
		vel.y = 0;
		useGravity = false;
		setupWallCrawl(new Point(xDir, 1));
		updateWallCrawl();
		groundedOnce = true;

		if (other.isSideWallHit() && !down) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		//sparks?.destroySelf();
	}
}


public class GroundHunterChargedProj : Projectile {

	bool fired;
	Player? player;
	Character? chr = null;
	int shootCount;
	int shootCooldown;

	public GroundHunterChargedProj(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "ground_hunter_charged_proj", netId, player
	) {
		weapon = GroundHunter.netWeapon;
		projId = (int)ProjIds.GroundHunterCharged;
		maxTime = 1f;
		destroyOnHit = false;
		fadeSprite = "ground_hunter_fade";
		fadeOnAutoDestroy = true;
		canBeLocal = false;

		vel.x = 300 * xDir;
		damager.damage = 2;
		damager.hitCooldown = 30;

		if (ownedByLocalPlayer) {
			this.player = player;
			chr = owner as Character;
		} 

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GroundHunterChargedProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer || player == null || chr == null) return;

		if (shootCooldown > 0) shootCooldown--;

		if (player?.input.getYDir(player) != 0 && !fired) {
			fired = true;
			time = 0;
		}

		else if (fired && shootCooldown <= 0 && shootCount < 5) {
			shootCooldown = 4;
			shootCount++;
			
			new GroundHunterSmallProj(chr, pos, xDir, 1, player?.getNextActorNetId(), true, player);
			new GroundHunterSmallProj(chr, pos, xDir, 2, player?.getNextActorNetId(), true, player);
		}

		else if (shootCount >= 5) destroySelf();
	}
}


public class GroundHunterSmallProj : Projectile {
	
	public GroundHunterSmallProj(
		Actor owner, Point pos, int xDir, int type, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "ground_hunter_small_proj", netId, player
	) {
		weapon = GroundHunter.netWeapon;
		projId = (int)ProjIds.GroundHunterSmall;
		maxTime = 0.33f;
		fadeSprite = "ground_hunter_fade";
		fadeOnAutoDestroy = true;
		if (type == 2) yScale *= -1;
		vel.y = -yScale * 300;

		damager.damage = 1;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GroundHunterSmallProj(
			arg.owner, arg.pos, arg.xDir, 
			arg.extraData[0], arg.netId, player: arg.player
		);
	}
}
