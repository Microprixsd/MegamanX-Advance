using System;

namespace MMXOnline;

public class RagingChargeX : Character {
	public int shotCount;
	public float punchCooldown;
	public float kickchargeCooldown;
	public float unlimitedcrushCooldown;
	public float saberCooldown;
	public float parryCooldown;
	public float maxParryCooldown = 30;
	public bool doSelfDamage;
	public float selfDamageCooldown;
	public float selfDamageMaxCooldown = 120;
	public Projectile? absorbedProj;
	public RagingChargeBuster ragingBuster;
	private Character? victim;

	public RagingChargeX(Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.RagingChargeX;

		// Set self-damage to 4s by default.
		selfDamageCooldown = 60 * 4;

		// Add raging charge buster to the weapon. (So ammo bar is visible)
		ragingBuster = new RagingChargeBuster();
		weapons.Add(ragingBuster);
	}

	public void ejecutarRCXGrabDash() {
		if (charState is Dash || charState is AirDash) {
			charState.isGrabbing = true;
			charState.superArmor = true; //peakbalance

			// Aplicar la animación `mmx_unpo_grab_dash` y dejar que se ejecute completamente
			if (sprite != null && sprite.name != "mmx_unpo_grab_dash") {
				changeSprite("mmx_unpo_grab_dash", false);
			}

			// Verificar si la hitbox del personaje ha tocado a `victim` mientras la animación está activa
			if (victim != null && victim.sprite != null && sprite.name == "mmx_unpo_grab_dash" && victim.sprite.name.Contains("_grabbed")) {
				changeState(new XUPGrabState(victim));
			}

			// Esperar hasta que la animación termine antes de restaurar los valores
			if (sprite != null && sprite.isAnimOver()) {
				charState.isGrabbing = false;
				charState.superArmor = false;
			}
		}
	}

	public override void preUpdate() {
		base.preUpdate();

		// Cooldowns.
		Helpers.decrementFrames(ref punchCooldown);
		Helpers.decrementFrames(ref saberCooldown);
		Helpers.decrementFrames(ref parryCooldown);
		Helpers.decrementFrames(ref unlimitedcrushCooldown);
		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= Global.speedMul;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name == getSprite(charState.shootSpriteEx)) {
					changeSpriteFromName(charState.defaultSprite, false);
					if (charState is WallSlide) {
						frameIndex = sprite.totalFrameNum - 1;
					}
				}
			}
		}
	}

	public override void update() {
		base.update();

		// Charge.
		chargeLogic(null);
	}

	public override bool attackCtrl() {
		if (player.input.isPressed(Control.WeaponRight, player) && parryCooldown == 0) {
			parryCooldown = 30;
			enterParry();
			return true;
		}
		if (player.input.isPressed(Control.WeaponLeft, player) && unlimitedcrushCooldown == 0) {
			unlimitedcrushCooldown = 60;
			changeState(new UnlimitedCrushState(), true);
			return true;
		}
		// Regular shoot.
		if (player.input.isPressed(Control.Shoot, player) && punchCooldown <= 0) { 
			// Shoot if has ammo,
			if (ragingBuster.ammo >= ragingBuster.getAmmoUsage(0)) {
				// Shoot happens here.
				if (ragingBuster.shootCooldown <= 0) {
					shoot();
					return true;
				}
			}
			// Punch if no ammo.
			else if (punchCooldown == 0) {
				punchCooldown = ragingBuster.fireRate;
				changeState(new XUPPunchState(grounded), true);
				return true;
			}
		}
		if (charState is Dash or AirDash) {
			if (player.input.isHeld(Control.Special1, player)) {
				ejecutarRCXGrabDash();
				return true;
			}
		}
		// Specials.
		if (player.input.isPressed(Control.Special1, player) && saberCooldown == 0) {
			saberCooldown = 60;
			changeState(new X6SaberState(grounded), true);
			return true;
		}
		if (player.input.isHeld(Control.Down, player) && player.input.isPressed(Control.Dash, player) && grounded) {
			changeState(new KickChargeState(), true);
			return true;
		}
		return base.attackCtrl();
	}

	// Shoot and set animation if posible.
	public void shoot() {
		if (player.input.isHeld(Control.Up, player)) {
			changeState(new RcxUpShot(), true);
			return;
		}
		if (player.input.isHeld(Control.Down, player) && !grounded) {
			changeState(new RcxDownShoot(), true);
			return;
		}
		string shootSprite = getSprite(charState.shootSpriteEx);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			shootSprite = grounded ? getSprite("shoot") : getSprite("fall_shoot");
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle && !charState.inTransition()) {
			frameIndex = 0;
			frameTime = 0;
		}
		shootAnimTime = DefaultShootAnimTime;

		shootEx(0);
	}

	public void shootEx(int byteAngle) {
		ragingBuster.shoot(this, byteAngle);
		ragingBuster.addAmmo(-ragingBuster.getAmmoUsage(0), player);
		punchCooldown = ragingBuster.fireRate;
	}

	public void enterParry() {
		if (absorbedProj != null) {
			changeState(new XUPParryProjState(absorbedProj, true, false), true);
			absorbedProj = null;
			return;
		}
		changeState(new XUPParryStartState(), true);
		return;
	}

	public override int getDisplayChargeLevel() {
		return (int)Helpers.clampMin(MathF.Ceiling(ragingBuster.ammo / ragingBuster.getAmmoUsage(0)), 1);
	}

	public override void increaseCharge() {
		chargeTime += Global.speedMul;
		if (isCharging()) {
			ragingBuster.addAmmo(ragingBuster.getAmmoUsage(0) * 0.9f * Global.spf, player);
		}
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override bool isNonDamageStatusImmune() {
		return true;
	}

	public bool isDecayImmune() {
		return charState is XUPGrabState
			or XUPParryMeleeState or XUPParryProjState
			or Hurt or GenericStun
			or VileMK2Grabbed
			or GenericGrabbedState;
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"mmx_beam_saber2" or "mmx_beam_saber_air2" => MeleeIds.ZSaber,
			"mmx_unpo_grab_dash" => MeleeIds.DashGrab,
			"mmx_unpo_punch" or "mmx_unpo_air_punch" => MeleeIds.Punch,
			"mmx_unpo_slide" => MeleeIds.KickCharge,
			"mmx_unpo_gigga" => MeleeIds.UnlimitedCrush,
			"mmx_unpo_parry_start" => MeleeIds.ParryBlock,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.DashGrab => new GenericMeleeProj(
				RCXGrab.netWeapon, projPos, ProjIds.UPGrab, player,
				0, 0, 0, addToLevel: addToLevel
			),
			(int)MeleeIds.ParryBlock => new GenericMeleeProj(
				RCXParry.netWeapon, projPos, ProjIds.UPParryBlock, player,
				0, 0, 60, addToLevel: addToLevel
			),
			(int)MeleeIds.Punch => new GenericMeleeProj(
				RCXPunch.netWeapon, projPos, ProjIds.UPPunch, player,
				3, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.KickCharge => new GenericMeleeProj(
				RCXKickCharge.netWeapon, projPos, ProjIds.KickCharge, player,
				3, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.UnlimitedCrush => new GenericMeleeProj(
				UnlimitedCrush.netWeapon, projPos, ProjIds.UnlimitedCrush, player,
				1, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				2, Global.defFlinch, 30, addToLevel: addToLevel
			),
			_ => null
		};
		return proj;
	}

	public override string getSprite(string spriteName) {
		return "mmx_" + spriteName;
	}
}

public enum MeleeIds {
	None = -1,
	DashGrab,
	ParryBlock,
	Punch,
	ZSaber,
	KickCharge,
	UnlimitedCrush,
}

