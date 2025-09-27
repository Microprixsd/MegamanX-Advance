using System;

namespace MMXOnline;

public class RagingChargeBuster : Weapon {
	public static RagingChargeBuster netWeapon = new(); 

	public RagingChargeBuster() : base() {
		index = (int)WeaponIds.RagingChargeBuster;
		killFeedIndex = 180;
		weaponBarBaseIndex = 70;
		weaponBarIndex = 59;
		weaponSlotIndex = 121;
		shootSounds = new string[] { "stockBuster", "stockBuster", "stockBuster", "stockBuster", "stockBuster" };
		fireRate = 20;
		canHealAmmo = true;
		drawAmmo = false;
		drawCooldown = false;
		allowSmallBar = false;
		drawRoundedDown = true;
		drawGrayOnLowAmmo = true;

		ammoGainMultiplier = 2;
		maxAmmo = 12;
		ammo = maxAmmo;
		drawRoundedDown = true;
		drawGrayOnLowAmmo = true;
	}

	public override float getAmmoUsage(int chargeLevel) { return 0; }

	public void shoot(RagingChargeX character, float byteAngle) {
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		if (xDir == -1) {
			byteAngle *= -1;
			byteAngle += 128;
		}
		byteAngle %= 256;

		character.playSound("plasmaShot", true);
		new RagingBusterProj(character, pos, byteAngle, player.getNextActorNetId(), true);
		new Anim(pos, "buster_unpo_muzzle", 1, null, true) {
			byteAngle = byteAngle
		};

		shootCooldown = fireRate;
	}
}


public class RagingBusterProj : Projectile {
	public RagingBusterProj(
		Actor owner, Point pos, float byteAngle,  ushort netProjId,
		bool sendRpc = false, Player? player = null
	) : base(
		pos, 1, owner, "buster_unpo", netProjId
	) {
		weapon = RagingChargeBuster.netWeapon;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		fadeSprite = "buster3_fade";
		fadeOnAutoDestroy = true;
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterUnpo;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 350;

		if (sendRpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netProjId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RagingBusterProj(
			arg.owner, arg.pos, arg.byteAngle, arg.netId, player: arg.player
		);
	}
}

public class AbsorbWeapon : Weapon {
	public Projectile absorbedProj;
	public AbsorbWeapon(Projectile otherProj) {
		index = (int)WeaponIds.UPParry;
		shootSounds = new string[] { "", "", "", "", "" };
		weaponSlotIndex = 118;
		killFeedIndex = 168;
		this.absorbedProj = otherProj;
		fireRate = 0;
		drawAmmo = false;
	}
	public override void shoot(Character character, int[] args) {
		if (character is not RagingChargeX rcx) return;	
		Player player = character.player;
		character.changeState(new XUPParryProjState(absorbedProj, true, true), true);
		rcx.absorbedProj = null;
		player.weapons.RemoveAll(w => w is AbsorbWeapon);
		int busterIndex = player.weapons.FindIndex(w => w is RagingChargeBuster);
		player.changeWeaponSlot(busterIndex);
	}
}

public class RCXZSaber : Weapon {
	public RCXZSaber() : base() {
		shootSounds = ["", "", "", "", ""];
		index = (int)WeaponIds.ZSaber;
		weaponBarBaseIndex = 21;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 48;
		killFeedIndex = 9;
		drawAmmo = false;
		drawCooldown = false;
	}

	public override void shoot(Character character, int[] args) {
		if (character is not RagingChargeX rcx) {
			return;
		}
		Player player = character.player;
		rcx.changeState(new RCXMaxWaveSaberState(), true);
		player.weapons.RemoveAll(w => w is RCXZSaber);
		int busterIndex = player.weapons.FindIndex(w => w is RagingChargeBuster);
		player.changeWeaponSlot(busterIndex);
	}
}

public class RCXParry : Weapon {
	public static RCXParry netWeapon = new RCXParry();

	public RCXParry() : base() {
		fireRate = 45;
		index = (int)WeaponIds.UPParry;
		killFeedIndex = 168;
	}
}

public class RCXPunch : Weapon
{
	public static RCXPunch netWeapon = new();

	public RCXPunch() : base()
	{
		fireRate = 20;
		index = (int)WeaponIds.UPPunch;
		killFeedIndex = 167;
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class RCXKickCharge : Weapon
{
	public static RCXKickCharge netWeapon = new();

	public RCXKickCharge() : base()
	{
		fireRate = 45;
		index = (int)WeaponIds.KickCharge;
		killFeedIndex = 167;
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class UnlimitedCrush : Weapon {
	public static UnlimitedCrush netWeapon = new();

	public UnlimitedCrush() : base() {
		fireRate = 45;
		index = (int)WeaponIds.UnlimitedCrush;
		killFeedIndex = 167;
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class RCXGrab : Weapon {
	public static RCXGrab netWeapon = new();

	public RCXGrab() : base() {
		fireRate = 45;
		//index = (int)WeaponIds.UPGrab;
		killFeedIndex = 92;
	}

	public class Chargedpunch : Weapon {
		public static Chargedpunch netWeapon = new();
		public Chargedpunch() : base() {
			fireRate = 45;
			index = (int)WeaponIds.Chargedpunch;
			killFeedIndex = 167;
		}
	}
}
