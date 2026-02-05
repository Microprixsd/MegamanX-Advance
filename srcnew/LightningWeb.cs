using System;
using System.Collections.Generic;

namespace MMXOnline;

public class LightningWeb : Weapon {

	public static LightningWeb netWeapon = new ();
	public LightningWeb() {
		shootSounds = new string[] { "busterX4", "busterX4", "busterX4", "busterX4" };
		displayName = "Lightning Web";
		fireRate = 60;
		index = (int)WeaponIds.LightningWeb;
		weaponBarBaseIndex = 72;
		weaponBarIndex = 61;
		weaponSlotIndex = 123;
		//killFeedIndex = 181;
		switchCooldown = 30;
		weaknessIndex = (int)WeaponIds.TwinSlasher;
		/* damage = "1/1";
		hitcooldown = "0.75";
		Flinch = "6/26";
		FlinchCD = hitcooldown;
		effect = "Can be used as a wall. C: Creates a network of nine webs."; */

		ammoDisplayScale = 1;
		maxAmmo = 16;
		ammo = maxAmmo;
	}


	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3 && ammo >=6) { return 6; }
		return 2;
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

		if (chargeLevel < 3 || chargeLevel >= 3 && ammo < 6) {
			new LightningWebProj(mmx, pos, xDir, player.getNextActorNetId(), true, player);
		} else {
			if (ammo >= 6) {
				new LightningWebProjCharged(mmx, pos, xDir, player.getNextActorNetId(), true, player);
			}
		}
	}
}

public class LightningWebProj : Projectile {
	public Character? character = null;

	public LightningWebProj(
		Actor owner, Point pos, int xDir, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "lightningweb_proj", netId, player
	) {
		weapon = LightningWeb.netWeapon;
		maxTime = 0.25f;
		projId = (int)ProjIds.LightningWebProj;
		damager.damage = 1;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 45;
		vel.x = xDir * 250;
		destroyOnHit = false;

		if (ownedByLocalPlayer) {
			character = player?.character;
		}
		
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {return;}

		if (character?.player.input.isPressed(Control.Shoot, character.player) == true) {
			destroySelf();
		}
	}
	public override void onDestroy() {
		base.onDestroy();

		if (ownedByLocalPlayer && character != null) {
			new LightningWebProjWeb(character, pos, xDir, damager.owner.getNextActorNetId(), rpc: true, damager.owner);
		}
	}
}


public class LightningWebProjWeb : Projectile {

	Wall? wall;
	public LightningWebProjWeb(
		Actor owner, Point pos, int xDir, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "lightningweb_proj_web", netId, player
	) {
		weapon = LightningWeb.netWeapon;
		maxTime = 1f;
		projId = (int)ProjIds.LightningWeb;
		setIndestructableProperties();
		fadeSprite = "lightningweb_webexausth";
		fadeOnAutoDestroy = true;
		playSound("lightningWebProjWeb", sendRpc: true);
		isStatic = true;

		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 60;
		damager.damage = 0;
		
		if (ownedByLocalPlayer) {
			if (collider != null) {
				collider.isClimbable = true;
				collider.wallOnly = false;

				var rect = collider.shape.getRect().getPoints();
				wall = new Wall("Collision Shape", new List<Point>()
				{
					rect[0].add(new Point(0, 0)),
					rect[1].add(new Point(0, 0)),
					rect[2].add(new Point(0, 0)),
					rect[3].add(new Point(0, 0)),
				});

				Global.level.addGameObject(wall);
			}

			if (player?.character != null) zIndex = player.character.zIndex - 10;
		}
		
		
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProjWeb(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (wall != null) Global.level.removeGameObject(wall);
	}
}


public class LightningWebProjCharged : Projectile {
	public Character? character = null;

	public LightningWebProjCharged(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "lightningweb_proj", netId, player
	) {
		weapon = LightningWeb.netWeapon;
		maxTime = 0.5f;
		projId = (int)ProjIds.LightningWebChargedProj;

		vel.x = xDir * 250;
		damager.damage = 0;
	
		if (ownedByLocalPlayer) {
			character = player?.character;
		}
		
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProjCharged(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {return;}

		if (character?.player.input.isPressed(Control.Shoot, character.player) == true) {
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer && character != null) {
			new LightningWebProjWebCharged(
				character, pos, xDir, 0, damager.owner.getNextActorNetId(), true, damager.owner);
		}
	}
}

public class LightningWebProjWebCharged : Projectile {
	private float lastHitTime;
	private int type;
	bool fired;
	float moveTime;
	Player? player;
	Character? character = null;

	public LightningWebProjWebCharged(
		Actor owner, Point pos, int xDir, int type, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "lightningweb_proj_charged", netId, player
	) {
		weapon = LightningWeb.netWeapon;
		maxTime = type == 0 ? 1.5f : 1;
		projId = (int)ProjIds.LightningWebCharged;
		fadeSprite = "lightningweb_proj_chargedexausth";
		fadeOnAutoDestroy = true;
		if (type == 0) playSound("lightningWebProjWeb", sendRpc: true);
		destroyOnHit = false;
		this.type = type;
		

		damager.damage = 1;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
		
		if (ownedByLocalPlayer) {
			this.player = player;
			if (player?.character != null) zIndex = player.character.zIndex + 10;
			character = owner as Character;
		}
		
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProjWebCharged(
			arg.owner, arg.pos, arg.xDir, 
			arg.extraData[0], arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();

		if (type == 0) {
			if (time >= maxTime / 3 && !fired) {
				fired = true;
				playSound("lightningWebProjWeb", sendRpc: true);
				for (int i = 0; i < 8; i++) {
					if (ownedByLocalPlayer && character != null) {
						new LightningWebProjWebCharged(
							character, pos, xDir, 
							i + 1, damager.owner.getNextActorNetId(), true, damager.owner
							) { frameIndex = frameIndex, frameTime = frameTime };
					}
				}
			}
		} else {
			if (moveTime < 16  || time >= maxTime - (Global.spf * 20)) {
				move(Point.createFromByteAngle((type - 1) * 32) * 240);
			}
			moveTime++;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		lastHitTime = 0.2f;
		if (damagable is Character chr && chr.ownedByLocalPlayer && !chr.isSlowImmune()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 50f);
			chr.slowdownTime = 0.125f;
		}
		base.onHitDamagable(damagable);
	}
}
