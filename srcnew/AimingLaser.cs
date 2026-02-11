using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class AimingLaser : Weapon {

	public static AimingLaser netWeapon = new AimingLaser();

	public AimingLaser() : base() {
		displayName = "Aiming Laser";
		index = (int)WeaponIds.AimingLaser;
		fireRate = 60;
		switchCooldown = 30;
		weaponSlotIndex = 128;
        weaponBarBaseIndex = 77;
        weaponBarIndex = 66;
		shootSounds = new string[] {"","","",""};
		weaknessIndex = (int)WeaponIds.SoulBody;
		/* damage = "1";
		hitcooldown = "0.3";
		Flinch = "0";
		FlinchCD = "0";
		effect = "Focuses scanned enemies."; */
		ammoDisplayScale = 1;
		maxAmmo = 16;
		ammo = maxAmmo;
	}

	public override void update() {
		base.update();
    	if (ammo < maxAmmo) {
        	rechargeAmmo(2);
    	}
	}
	public override bool canShoot(int chargeLevel, Player player) {
		MegamanX? mmx = player.character as MegamanX;

		if (chargeLevel < 3) return base.canShoot(chargeLevel, player) && mmx?.aLaserTargets.Count > 0;

		return base.canShoot(chargeLevel, player);
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3 && ammo >= 6) {
			return 6;
		}
		return 0;
	}
	
	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = player.character as MegamanX ?? throw new NullReferenceException();
		
		if (chargeLevel < 3 || chargeLevel >= 3 && ammo < 6) {
			int type = 0;

			foreach(var targ in mmx.aLaserTargets) {
				if (targ.pos.distanceTo(pos) <= 320) {
					new AimingLaserProj(mmx, pos, xDir, type, player.getNextActorNetId(), targ, true, player);
					addAmmo(-1, player);
					type++;
				}
			}
			rechargeCooldown = 0.5f;
		} else if (chargeLevel >= 3 && ammo >= 6) {
			float angle = xDir > 0 ? 0 : 128;
			new AimingLaserChargedProj(mmx, pos, xDir, angle, player.getNextActorNetId(), true, player);

			mmx.aLaserTargets.Clear();
		}

		mmx.aLaserCursor?.destroySelf();
		mmx.aLaserCursor = null!;
		rechargeCooldown = 1;
	}
}


public class AimingLaserTargetAnim : Anim {
	Character? chara;
	MegamanX? owner;

	public AimingLaserTargetAnim(
		Actor owner, Point pos, int xDir, ushort? netId, Character chara
	) : base (
		pos, "aiming_laser_cursor", xDir, netId, false, true
	) {
		this.chara = chara;
		this.owner = owner as MegamanX;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer || chara == null) return;

		changePos(chara.getCenterPos());

		if (sprite.loopCount >= 5) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		
		owner?.aLaserTargetAnim = null!;
	}
}


public class AimingLaserHud : Anim {

	MegamanX mmx = null!;
	Player player;
	float ang = -64;
	float finalAng;
	const float distance = 64;
	
	public AimingLaserHud(
		Point pos, int xDir, ushort? netId, Player player, int frame
	) : base(
		pos, "aiming_laser_hud", xDir, netId, false
	) {
		frameSpeed = 0;
		frameIndex = frame;
		this.player = player;
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.aLaserHud = this;
		ang = ang + (frame * 12.8f);

		finalAng = xDir > 0 ? ang : -ang + 128;
		float posX = mmx.getCenterPos().x + (distance * Helpers.cosb(finalAng));
		float posY = mmx.getCenterPos().y + (distance * Helpers.sinb(finalAng));
		changePos(new Point(posX, posY));
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (mmx.destroyed || mmx.charState is Die) {
			destroySelf();
			return;
		}

		if (player.weapon is not AimingLaser) destroySelf();

		xDir = mmx.getShootXDir();
		finalAng = xDir > 0 ? ang : -ang + 128;

		float posX = mmx.getCenterPos().x + (distance * Helpers.cosb(finalAng));
		float posY = mmx.getCenterPos().y + (distance * Helpers.sinb(finalAng));

		changePos(new Point(posX, posY));
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserHud = null!;
	}
}


public class AimingLaserCursor : Projectile {
	MegamanX mmx = null!;
	Player? player;
	float ogAngle = 0;
	float laserAngle;
	const float laserDistance = 64;

	public AimingLaserCursor(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? player = null 
	) : base (
		pos, xDir, owner, "aiming_laser_cursor", netId, player
	) {
		weapon = AimingLaser.netWeapon;
		this.player = player;
		mmx = owner as MegamanX ?? throw new NullReferenceException();
		mmx.aLaserCursor = this;
		setIndestructableProperties();

		laserAngle = xDir > 0 ? 0 : 128;
		changePos(mmx.getCenterPos().add(Point.createFromByteAngle(laserAngle).times(laserDistance)));

	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer || mmx == null) return;

		if (player?.weapon is not AimingLaser || mmx?.alive == false) destroySelf();

		int dirY = player?.input.getYDir(player) ?? 0;
		if (dirY != 0) ogAngle += dirY * 8;

		if (ogAngle < -64) ogAngle = -64;
		if (ogAngle > 64) ogAngle = 64;

		laserAngle = mmx?.getShootXDir() > 0 ? ogAngle : -ogAngle + 128;

		float posX = mmx?.getCenterPos().x + (laserDistance * Helpers.cosb(laserAngle)) ?? 0;
		float posY = mmx?.getCenterPos().y + (laserDistance * Helpers.sinb(laserAngle)) ?? 0;

		changePos(new Point(posX, posY));
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		var chr = other.gameObject as Character;

		if (chr != null && 
			mmx.aLaserTargets.Count < 3 &&
			chr.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {

				if (!mmx.aLaserTargets.Any(c => c == chr)) {
					mmx.aLaserTargets.Add(chr);
					//chr.addALaserAttacker(mmx);
					if (mmx.aLaserTargetAnim == null) {
						mmx.aLaserTargetAnim = new AimingLaserTargetAnim(
							mmx, chr.getCenterPos(), 1, null, chr
						);
					}
					playSound("axlTarget");
				}

		}
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserCursor = null!;
	}
}


public class AimingLaserProj : Projectile {
	
	Point endPos;
	Character target = null!;
	MegamanX mmx;
	int type;
	float angDif = 5;
	float length;
	float l;
	float ang;

	public AimingLaserProj(
		Actor owner, Point pos, int xDir, int type, ushort? netId, 
		Character target = null!, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "empty", netId, player
	) {
		weapon = AimingLaser.netWeapon;
		projId = (int)ProjIds.AimingLaser;
		destroyOnHit = false;
		maxTime = 1f;
		setIndestructableProperties();
		this.target = target;
		this.type = type;
		//endPos = target.pos. ?? target.getCenterPos();
		mmx = owner as MegamanX ?? throw new NullReferenceException();
		//mmx.aLaserProj = this;
		mmx.aLasers.Add(this);

		damager.damage = 1;
		damager.hitCooldown = 20;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type  };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}

		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new AimingLaserProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], 
			arg.netId, player: arg.player
		);
	}

	public override void onStart() {
		base.onStart();

		changePos(mmx.getShootPos());
		endPos = target.getCenterPos();
		setEndPos(endPos);
		length = pos.distanceTo(endPos);
		l = MathF.Max(length - 24, 0);
		ang = pos.directionTo(endPos).byteAngle;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer || target == null) return;

		if (target.charState is Die || mmx.player.weapon is not AimingLaser) {
			//mmx.aLaserTargets.Remove(target);
			destroySelf();
		} 
	}

	public override void postUpdate() {
		base.postUpdate();
		if (target == null || target.destroyed) return;

		changePos(mmx.getShootPos());
		endPos = target.getCenterPos();
		setEndPos(endPos);
		length = pos.distanceTo(endPos);
		l = MathF.Max(length - 24, 0);
		ang = pos.directionTo(endPos).byteAngle;
	}


	/* public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;

		var damagable = other.gameObject as Character;
		if (damagable == null || damagable != target) return;

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) && 
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {

				playSound("axlTarget");
				damagable.applyDamage(1, mmx.player, this, (int)WeaponIds.AimingLaser, projId);
				
			}
		} 
	} */

	public void setEndPos(Point end) {
		this.endPos = end;

		if (!ownedByLocalPlayer) {
			changePos(mmx.getShootPos());
			endPos = end;
			//setEndPos(endPos);
			length = pos.distanceTo(endPos);
			l = MathF.Max(length - 24, 0);
			ang = pos.directionTo(endPos).byteAngle;
		} 

		globalCollider = new Collider(getPoints(), true, null!, false, false, 0, Point.zero);
	}

	List<Point> getPoints() {
		Point pointA = pos.add(Point.createFromByteAngle(ang - angDif).times(l));
		Point pointB = pos.add(Point.createFromByteAngle(ang + angDif).times(l));

		return new List<Point>() {
			pos,
			pointA,
			endPos,
			pointB
		};
	} 

	public override void render(float x, float y) {
		base.render(x,y);
		if (destroyed) return;
		
		var colors = new List<Color>()
		{
			new Color(39, 255, 39, 255),
			new Color(255, 39, 42, 255),
			new Color(251, 255, 39, 255),
		};

		DrawWrappers.DrawPolygon(getPoints(), colors[type], true, ZIndex.Actor);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.AddRange(BitConverter.GetBytes(endPos.x));
		customData.AddRange(BitConverter.GetBytes(endPos.y));

		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		float endX = BitConverter.ToSingle(data[0..4], 0);
		float endY = BitConverter.ToSingle(data[4..8], 0);

		setEndPos(new Point(endX, endY));
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserTargets.Remove(target);
		mmx.aLasers.Remove(this);
	}
}


public class AimingLaserChargedProj : Projectile {

	MegamanX mmx = null!;
	float ang = 0;
	float finalAng;
	float length = 0;
	Player? player;
	Point endPos;
	Point shootDir;
	float l;
	float angDif = 15;
	float[] angs = new float[5];

	public AimingLaserChargedProj(
		Actor owner, Point pos, int xDir,float byteAngle, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "empty", netId, player
	) {
		weapon = AimingLaser.netWeapon;
		projId = (int)ProjIds.AimingLaserCharged;
		maxTime = 3f;
		setIndestructableProperties();

		if (ownedByLocalPlayer) {
			mmx = owner as MegamanX ?? throw new NullReferenceException();
			mmx.aLaserChargedProj = this;
			this.byteAngle = byteAngle;
			this.player = player;
		}
		
		shootDir = new Point(xDir, 0);
		endPos = pos.add(shootDir.normalize().times(length));

		damager.damage = 1;
		damager.hitCooldown = 18;

		setEndPos(endPos);

		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
		}

		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new AimingLaserChargedProj(
			arg.owner, arg.pos, arg.xDir, 
			arg.byteAngle, arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();
		if (length < 128) length += 2;
		l = MathF.Max(length * 0.75f, 0);

		int dirY = damager.owner.input.getYDir(damager.owner);
		if (dirY != 0) ang += dirY * 8;

		if (ang < -64) ang = -64;
		if (ang > 64) ang = 64;

		byteAngle = mmx.getShootXDir() > 0 ? ang : -ang + 128;

		if (!ownedByLocalPlayer) return;
		if (damager.owner.weapon is not AimingLaser) destroySelf();
	}

	public override void postUpdate() {
		base.postUpdate();
		//if (byteAngle == null) return;
		if (!ownedByLocalPlayer) return;

		for (int i = 3; i >= 0; i--) {
			angs[i + 1] = angs[i];
		}
		angs[0] = byteAngle;

		changePos(mmx.getShootPos());
		endPos = pos.add(Point.createFromByteAngle(byteAngle) * length);
		setEndPos(endPos);
	}

	public void setEndPos(Point endPos) {
		this.endPos = endPos;

		globalCollider = new Collider(getPoints(angs[0]), true, null!, false, false, HitboxFlag.Hitbox, Point.zero);
	}

	List<Point> getPoints(float ang) {
		//if (byteAngle == null) return new List<Point>();
		Point pointA = pos.add(Point.createFromByteAngle(ang - angDif).times(l));
		Point pointB = pos.add(Point.createFromByteAngle(ang + angDif).times(l));

		return new List<Point>() {
			pos,
			pointA,
			endPos,
			pointB
		};
	} 

	public override void render(float x, float y) {
		base.render(x,y);

		for (int i = 0; i < angs.Length; i++) {
			Color color = new(39, 255, 230, (byte)(255 - (i * 51)));
			DrawWrappers.DrawPolygon(getPoints(angs[i]), color, true, ZIndex.Actor);
		}
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.AddRange(BitConverter.GetBytes(endPos.x));
		customData.AddRange(BitConverter.GetBytes(endPos.y));

		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		float endX = BitConverter.ToSingle(data[0..4], 0);
		float endY = BitConverter.ToSingle(data[4..8], 0);

		setEndPos(new Point(endX, endY));
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserChargedProj = null!;
	}
}
