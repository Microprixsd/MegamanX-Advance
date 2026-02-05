using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class SoulBody : Weapon {

	public static SoulBody netWeapon = new SoulBody();
	public SoulBody() : base() {
		index = (int)WeaponIds.SoulBody;
		displayName = "Soul Body";
		fireRate = 60;
		switchCooldown = 30;
		weaponSlotIndex = 125;
        weaponBarBaseIndex = 74;
        weaponBarIndex = 63;
		shootSounds = new string[] {"buster2X4","buster2X4","buster2X4","buster2X4"};
		weaknessIndex = (int)WeaponIds.LightningWeb;

		ammoDisplayScale = 1;
		maxAmmo = 16;
		ammo = maxAmmo;	
		/* damage = "1/3";
		hitcooldown = "0.5/0.75";
		Flinch = "0/13";
		FlinchCD = hitcooldown;
		effect = "Deals damage on contact. C: Spawns 5 holograms that track enemies."; */
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

	public override bool canShoot(int chargeLevel, Player player) {
		MegamanX? mmx = player.character as MegamanX;

		return base.canShoot(chargeLevel, player) && mmx?.sBodyHologram == null
			&& mmx?.sBodyClone == null;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.pos;
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel >= 3 && ammo >= 6) {
			character.changeState(new ControlClone(), true);
		} else
		if (chargeLevel < 3 || chargeLevel >= 3 && ammo < 6) {
			new SoulBodyHologram(character, pos, xDir, player.getNextActorNetId(), true, player);
		}
	}
}


public class SoulBodyHologram : Projectile {

	MegamanX mmx = null!;
	float distance;
	const float maxDist = 96;
	int frameCount;
	public SoulBodyHologram(
		Actor owner, Point pos, int xDir, 
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "empty", netId, player
	) {
		weapon = SoulBody.netWeapon;
		mmx = owner as MegamanX ?? throw new NullReferenceException();
		projId = (int)ProjIds.SoulBodyHologram;
		fadeSprite = "soul_body_fade";
		fadeOnAutoDestroy = true;
		frameSpeed = 0;
		changeSprite(mmx.sprite.name, false);
		frameIndex = mmx.frameIndex;
		mmx.sBodyHologram = this;
		maxTime = 2.5f;
		setIndestructableProperties();
		canBeLocal = false;

		damager.damage = 1;
		damager.hitCooldown = 20;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SoulBodyHologram(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}
	
	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		
		globalCollider = new Collider(new Rect(0,0, 18, 34).getPoints(), 
			true, this, false, false, HitboxFlag.Hitbox, Point.zero);

		if (distance < maxDist) distance += 4;
		else distance = maxDist;

		xDir = mmx.xDir;

		changePos(mmx.pos.addxy(mmx.getShootXDir() * distance, 0));
		changeSprite(mmx.sprite.name, false);
		frameIndex = mmx.frameIndex;
		frameCount++;

		if (time >= maxTime * 0.75f) {
			visible = frameCount % 2 == 0;
		}
	}
	
	public override List<ShaderWrapper>? getShaders() {
		var shaders = new List<ShaderWrapper>();

		ShaderWrapper cloneShader = Helpers.cloneShaderSafe("soulBodyPalette");

		int index = (frameCount / 2) % 7;
		if (index == 0) index++;

		cloneShader.SetUniform("palette", index);
		cloneShader.SetUniform("paletteTexture", Global.textures["soul_body_palette"]);
		shaders.Add(cloneShader);
	
		if (shaders.Count > 0) {
			return shaders;
		} else {
			return base.getShaders();
		}
	} 

	public override void onDestroy() {
		base.onDestroy();
		mmx.sBodyHologram = null!;
	}
}

public class ControlClone : CharState {

	MegamanX mmx = null!;
	bool fired;
	float cloneCooldown;
	int cloneCount;
	float[] altAngles = new float [] {0, 16, 240, 32, 224}; 

	public ControlClone() : base("summon") {
		normalCtrl = false;
		attackCtrl = false;
		useDashJumpSpeed = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		mmx.useGravity = false;
		mmx.stopMoving();
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.shootAnimTime = 0;
		mmx.useGravity = true;
		return;
	}

	public override void update() {
		base.update();

		//x4Update();
		x5Update();
	}

	void x4Update() {
		if (character.isAnimOver() && !fired) {
			new SoulBodyClone(mmx.player, mmx.pos.x, mmx.pos.y, mmx.xDir, false,
			mmx.player.getNextATransNetId(), mmx.ownedByLocalPlayer);
			//mmx.ownedByLocalPlayer = false;

			fired = true;
			return;
		}
	}

	void x5Update() {
		Helpers.decrementFrames(ref cloneCooldown);

		if (cloneCount >= 5 && (cloneCooldown <= 10 || character.isAnimOver())) {
			character.changeToIdleOrFall();
			return;
		} else if (character.isAnimOver() && cloneCooldown <= 0) {
			float ang = altAngles[cloneCount];
			ang = character.xDir == 1 ? ang : -ang + 128;
			Actor? target = Global.level.getClosestTarget(character.getCenterPos(), player.alliance, false, 160);

			if (target != null) ang = character.pos.directionTo(target.getCenterPos()).byteAngle;

			new SoulBodyX5(
            	character, character.pos, character.xDir,
            	player.getNextActorNetId(), cloneCount + 1, ang, true, player
            );

			cloneCount++;
			cloneCooldown = 20;
		}
	}
}


public class SoulBodyX5 : Projectile {

	int color;
	float spd = 360;

	public SoulBodyX5(
		Actor owner, Point pos, int xDir, 
		ushort? netId, int color, float ang, 
		bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "soul_body_x5", netId, player
	) {
		weapon = SoulBody.netWeapon;
		projId = (int)ProjIds.SoulBodyX5;
		maxTime = 0.75f;
		destroyOnHit = false;
		vel = Point.createFromByteAngle(ang).times(spd);
		this.color = color;
		byteAngle = ang;

		damager.damage = 2;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)color, (byte)ang });
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SoulBodyX5(
			arg.owner, arg.pos, arg.xDir, arg.netId, arg.extraData[0],
			 arg.extraData[1], player: arg.player
		);
	}

	public override List<ShaderWrapper>? getShaders() {
		var shaders = new List<ShaderWrapper>();

		ShaderWrapper cloneShader = Helpers.cloneShaderSafe("soulBodyPalette");

		cloneShader.SetUniform("palette", color);
		cloneShader.SetUniform("paletteTexture", Global.textures["soul_body_palette"]);
		shaders.Add(cloneShader);

		if (shaders.Count > 0) {
			return shaders;
		} else {
			return base.getShaders();
		}
	}
}
