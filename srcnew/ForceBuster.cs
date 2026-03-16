using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BusterStockProj : Projectile {
	public BusterStockProj(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "buster_unpo", netId, player
	) {
		weapon = XBuster.netWeapon;
		fadeSprite = "buster3_fade";
		fadeOnAutoDestroy = true;
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterForceStock;

		vel.x = 350 * xDir;
		damager.damage = 2;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterStockProj(
			arg.owner, arg.pos, arg.xDir, arg.netId
		);
	}
}


public class BusterForcePlasmaProj : Projectile {

	public BusterForcePlasmaProj(
		Actor owner, Point pos, int xDir, ushort? netId,
		 bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "buster_plasma", netId, player
	) {
		weapon = XBuster.netWeapon;
		fadeSprite = "buster4_x3_muzzle";
		fadeOnAutoDestroy = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterForcePlasma;

		vel.x = xDir * 360;
		damager.damage = 4;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 15;
		
		xScale = 0.75f;
		yScale = 0.75f;
		releasePlasma = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterForcePlasmaProj(
			arg.owner, arg.pos, arg.xDir, arg.netId
		);
	}
}


public class BusterForcePlasmaHit : Projectile {
	public int type = 0;
	public float xDest = 0;
	public Actor actorOwner = null!;

	public BusterForcePlasmaHit(
		int type, Actor owner, Point pos, int xDir, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "buster_plasma_hit", netId, player
	) {
		weapon = XBuster.netWeapon;
		zIndex -= 10;
		//fadeSprite = "buster_plasma_hit_exhaust";
		fadeOnAutoDestroy = true;
		maxTime = 2f;
		projId = (int)ProjIds.BusterForcePlasmaHit;
		destroyOnHit = false;

		vel.x = xDir * 10;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 45;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
		netcodeOverride = NetcodeModel.FavorDefender;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		// Hunter
		if (type == 1) {
			maxTime = 6;
			vel.x *= 1.5f;
		}
		// Various
		if (type == 2) {
			maxTime = 2.5f;
			vel.x *= 3;
		}
		// Slicer
		if (type == 3) {
			maxTime = 1;
			xDest = pos.x + (xDir * 30);
			vel.x = 0f;
			vel.y = -500f;
			useGravity = true;
			damager.flinch = Global.halfFlinch;
		}
		// Splasher
		if (type == 4) {
			maxTime = 4;
			vel.x = 0;
			vel.y = 0;
			if (player?.character != null) {
				actorOwner = player.character;
			}
		}
		// Gravity Well, Lightning Web, Frost Tower
		if (type == 5 || type == 6) {
			maxTime = 4;
			vel.x = 0;
			vel.y = 0;
		}
		// Double Cyclone
		if (type == 7) {
			vel.x *= 48;
		}
		this.type = type;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
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
		// Gravity/Web one.
		if (type == 5 || type == 6) {
			if (time < 1 && type == 5) {
				move(new Point(0, -60));
			} else {
				followTarget();
			}
		}
		//Double Cyclone one.
		if (type == 7) {
			if (Math.Abs(vel.x) > 30) vel.x -= xDir * Global.speedMul * 10; 
		}
	}

	public void followOwner() {
		if (actorOwner != null) {
			float targetPosX = (40 * -actorOwner.xDir + actorOwner.pos.x);
			float targetPosY = (-15 + actorOwner.pos.y + (2 - (Global.time % 2)));
			float moveSpeed = 1 * 60;

			// X axis follow.
			if (pos.x < targetPosX) {
				move(new Point(moveSpeed, 0));
				if (pos.x > targetPosX) { changePos(targetPosX, pos.y); }
			} else if (pos.x > targetPosX) {
				move(new Point(-moveSpeed, 0));
				if (pos.x < targetPosX) { changePos(targetPosX, pos.y); }
			}
			// Y axis follow.
			if (pos.y < targetPosY) {
				move(new Point(0, moveSpeed));
				if (pos.y > targetPosY) { changePos(pos.x, targetPosY); }
			} else if (pos.y > targetPosY) {
				move(new Point(0, -moveSpeed));
				if (pos.y < targetPosY) { changePos(pos.x, targetPosY); }
			}
		}
	}

	public void followTarget() {
		Actor? closestEnemy = Global.level.getClosestTarget(
			new Point (pos.x, pos.y),
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
			if (pos.x > enemyPos.x) { changePos(enemyPos.x, pos.y); }
		} else if (pos.x > enemyPos.x) {
			move(new Point(-moveSpeed, 0));
			if (pos.x < enemyPos.x) { changePos(enemyPos.x, pos.y); }
		}
		// Y axis follow.
		if (pos.y < enemyPos.y) {
			move(new Point(0, moveSpeed * 0.125f));
			if (pos.y > enemyPos.y) { changePos(pos.x, enemyPos.y); }
		} else if (pos.y > enemyPos.y) {
			move(new Point(0, -moveSpeed));
			if (pos.y < enemyPos.y) { changePos(pos.x, enemyPos.y); }
		}
	}

	public override List<ShaderWrapper>? getShaders() {
		var shaders = new List<ShaderWrapper>();

		ShaderWrapper plasmaShader = Helpers.cloneShaderSafe("plasmaPalette");

		plasmaShader.SetUniform("palette", type);
		plasmaShader.SetUniform("paletteTexture", Global.textures["buster_plasma_hit_palette"]);
		shaders.Add(plasmaShader);
	
		if (shaders.Count > 0) {
			return shaders;
		} else {
			return base.getShaders();
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterForcePlasmaHit(
			arg.extraData[0], arg.owner, arg.pos, 
			arg.xDir, arg.netId
		);
	}
}

public class ForceBuster3Proj : Projectile {

	public ForceBuster3Proj(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "buster3_x4", netId, player)
	{
		weapon = XBuster.netWeapon;
		maxTime = 0.8f;
		fadeSprite = "buster2_fade";
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.BusterForce3;

		vel.x = xDir * 240;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public override void update() {
		base.update();
	
		vel.x += Global.spf * xDir * 550f;
		if (MathF.Abs(vel.x) > 300f) vel.x = 300 * xDir;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ForceBuster3Proj(
			arg.owner, arg.pos, arg.xDir, arg.netId
		);
	}
}
