using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RagingChargeBuster : Weapon {
	public static RagingChargeBuster netWeapon = new(); 

	public RagingChargeBuster() : base() {
		index = (int)WeaponIds.RagingChargeBuster;
		killFeedIndex = 180;
		weaponBarBaseIndex = 70;
		weaponBarIndex = 59;
		weaponSlotIndex = 121;
		shootSounds = new string[] { "buster2", "buster2", "buster2", "buster2" };
		fireRate = 90;
		canHealAmmo = true;
		drawAmmo = true;
		drawCooldown = true;
		allowSmallBar = false;
		drawRoundedDown = true;
		drawGrayOnLowAmmo = true;

		ammoGainMultiplier = 2;
		maxAmmo = 16;
		ammo = maxAmmo;
		canRechargeAmmo = true;
		ammoRechargeRate = 15f;
	}

	public override float getAmmoUsage(int chargeLevel) { return 16; }

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
    private static int getDirection(float angle) {
        float normalized = ((angle % 256) + 256) % 256;
        return ((int)MathF.Round(normalized / 64f)) % 4;
    }

    private static string getProjectileSprite(float angle) {
        int direction = getDirection(angle);
        return direction == 1 || direction == 3
            ? "buster_unpo_vertical"
            : "buster_unpo";
    }

    public RagingBusterProj(
        Actor owner, Point pos, float byteAngle, ushort netProjId,
        bool sendRpc = false, Player? player = null
    ) : base(
        pos,
        getDirection(byteAngle) == 2 ? -1 : 1,
        owner,
        getProjectileSprite(byteAngle),
        netProjId,
        player
    ) {
        weapon = RagingChargeBuster.netWeapon;
        damager.damage = 3;
        damager.flinch = Global.halfFlinch;
        fadeSprite = "buster3_fade";
        fadeOnAutoDestroy = true;
        reflectable = false;
        maxTime = 0.5f;
        projId = (int)ProjIds.BusterUnpo;

        int direction = getDirection(byteAngle);

        // UP Sprite
        yDir = direction == 1 ? -1 : 1;

        // xDir and yDir direction
        this.byteAngle = 0;

        vel = direction switch {
            0 => new Point(350, 0),
            1 => new Point(0, 350),
            2 => new Point(-350, 0),
            _ => new Point(0, -350)
        };

        if (sendRpc) {
            rpcCreateByteAngle(
                pos, owner, ownerPlayer, netProjId, direction * 64
            );
        }
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new RagingBusterProj(
            arg.owner, arg.pos, arg.byteAngle, arg.netId,
            player: arg.player
        );
    }
}

public class AbsorbWeapon : Weapon {
	public Projectile absorbedProj;
	public AbsorbWeapon(Projectile otherProj) {
		index = (int)WeaponIds.UPParry;
		weaponSlotIndex = 118;
		killFeedIndex = 168;
		this.absorbedProj = otherProj;
		drawAmmo = false;
	}
}

public class XUPParry : Weapon {
	public static XUPParry netWeapon = new XUPParry();

	public XUPParry() : base() {
		fireRate = 45;
		index = (int)WeaponIds.UPParry;
		killFeedIndex = 184;
	}
}

public class XUPPunch : Weapon
{
	public static XUPPunch netWeapon = new();

	public XUPPunch() : base()
	{
		fireRate = 20;
		index = (int)WeaponIds.UPPunch;
		killFeedIndex = 167;
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class XUPKickCharge : Weapon
{
	public static XUPKickCharge netWeapon = new();

	public XUPKickCharge() : base()
	{
		fireRate = 45;
		index = (int)WeaponIds.UPKickCharge;
		killFeedIndex = 182;
		//damager = new Damager(player, 3, Global.defFlinch, 0.5f);
	}
}

public class XUPUnlimitedCrush : Weapon {
	public static XUPUnlimitedCrush netWeapon = new();

	public XUPUnlimitedCrush() : base() {
		fireRate = 45;
		index = (int)WeaponIds.UnlimitedCrush;
		killFeedIndex = 183;
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

	public class XUPPunchCharged : Weapon {
		public static XUPPunchCharged netWeapon = new();
		public XUPPunchCharged() : base() {
			fireRate = 45;
			index = (int)WeaponIds.UPPunchCharged;
			killFeedIndex = 167;
		}
	}
}
