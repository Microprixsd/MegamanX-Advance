using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;


public class BusterForcePlasmaProj : Projectile {

	public BusterForcePlasmaProj(
		Point pos, int xDir, Actor owner, Player player, ushort? netId, bool sendRpc = false
	) : base(
		pos, xDir, owner, "buster_plasma", netId, player
	) {
		weapon = XBuster.netWeapon;
		projId = (int)ProjIds.BusterForcePlasmaProj;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;

		vel.x = 360 * xDir;
		fadeSprite = "buster4_x3_muzzle";
		fadeOnAutoDestroy = true;
		maxTime = 0.5f;
		destroyOnHit = true;
		if (sendRpc) {
			rpcCreate(pos, owner, player, netId, xDir);
		}
		xScale = 0.75f;
		yScale = 0.75f;
		if (ownedByLocalPlayer) {
			createPlasma = true;
		}
	}
}

public class BusterForcePlasmaHit : Projectile {
	public int type = 0;
	public float xDest = 0;
	public Actor? ownerChar = null;

	public BusterForcePlasmaHit(
		int type, Actor owner, Point pos, int xDir, ushort netId,
		bool sendRpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "buster_plasma_hit", netId, player
	) {
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 45;
		vel.x = 12 * xDir;
		weapon = XBuster.netWeapon;
		zIndex -= 10;
		//fadeSprite = "buster_plasma_hit_exhaust";
		fadeOnAutoDestroy = true;
		maxTime = 2f;
		projId = (int)ProjIds.BusterForcePlasmaHit;
		destroyOnHit = false;
		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
		netcodeOverride = NetcodeModel.FavorDefender;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		// Hunter
		if (type == 1) {
			maxTime = 6;
			vel.x = 15 * xDir;
		}
		// Various
		if (type == 2) {
			maxTime = 2.5f;
			vel.x = 30 * xDir;
		}
		// Slicer
		if (type == 3) {
			maxTime = 1;
			xDest = pos.x + (xDir * 30);
			vel.x = 0f;
			vel.y = -325f;
			useGravity = true;
			damager.flinch = 13;
		}
		// Splasher
		if (type == 4) {
			maxTime = 4;
			vel.x = 0;
			vel.y = 0;
			if (player?.character != null) {
				ownerChar = player.character;
			}
			canBeLocal = false;
		}
		// Gravity Well
		if (type == 5) {
			maxTime = 4;
			vel.x = 0;
			vel.y = 0;
			canBeLocal = false;
		}
		if (type == 6) {
			maxTime = 0.9f;
			destroyOnHitWall = true;
			vel.x = 225 * xDir;
		}
		this.type = type;
	}

	public override void update() {
		base.update();
		if (!canBeLocal && !ownedByLocalPlayer) {
			return;
		}
		// Slicer one.
		if (type == 3) {
			vel.y += Global.spf * Global.level.gravity;
			if (vel.y < 0) {
				float x = Helpers.lerp(pos.x, xDest, Global.spf * 10f);
				changePos(new Point(x, pos.y));
			}
		}
		// Splasher one.
		if (type == 4) {
			followOwner();
		}
		// Gravity one.
		if (type == 5) {
			if (time < 1) {
				move(new Point(0, -60));
			} else {
				followTarget();
			}
		}
	}

	public void followOwner() {
		if (ownerChar != null) {
			float targetPosX = (40 * -ownerChar.xDir + ownerChar.pos.x);
			float targetPosY = (-15 + ownerChar.pos.y + (2 - (Global.time % 2)));
			float moveSpeed = 1 * 60;

			// X axis follow.
			if (pos.x < targetPosX) {
				move(new Point(moveSpeed, 0));
				if (pos.x > targetPosX) { pos.x = targetPosX; }
			} else if (pos.x > targetPosX) {
				move(new Point(-moveSpeed, 0));
				if (pos.x < targetPosX) { pos.x = targetPosX; }
			}
			// Y axis follow.
			if (pos.y < targetPosY) {
				move(new Point(0, moveSpeed));
				if (pos.y > targetPosY) { pos.y = targetPosY; }
			} else if (pos.y > targetPosY) {
				move(new Point(0, -moveSpeed));
				if (pos.y < targetPosY) { pos.y = targetPosY; }
			}
		}
	}

	public void followTarget() {
		Actor? closestEnemy = Global.level.getClosestTarget(
			new Point(pos.x, pos.y),
			damager.owner.alliance,
			false, 200
		);

		if (closestEnemy == null) {
			return;
		}
		Point enemyPos = closestEnemy.getCenterPos();
		float moveSpeed = 1 * 60;

		// X axis follow.
		if (pos.x < enemyPos.x) {
			move(new Point(moveSpeed, 0));
			if (pos.x > enemyPos.x) { pos.x = enemyPos.x; }
		} else if (pos.x > enemyPos.x) {
			move(new Point(-moveSpeed, 0));
			if (pos.x < enemyPos.x) { pos.x = enemyPos.x; }
		}
		// Y axis follow.
		if (pos.y < enemyPos.y) {
			move(new Point(0, moveSpeed * 0.125f));
			if (pos.y > enemyPos.y) { pos.y = enemyPos.y; }
		} else if (pos.y > enemyPos.y) {
			move(new Point(0, -moveSpeed));
			if (pos.y < enemyPos.y) { pos.y = enemyPos.y; }
		}
	}
}


