using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class RagingChargeX : Character {
    public float punchCooldown;
	public float saberCooldown;
    public float kickchargeCooldown;
	public float unlimitedcrushCooldown;
	public float parryCooldown;
	public float shootCooldown;
	public float maxParryCooldown = 30;
	public float selfDamageCooldown;
	public float selfDamageMaxCooldown = 120;
	public float secondSoundCooldown;
	public int lastAmmoSound = -1;
	public Projectile? absorbedProj;
	public RagingChargeBuster ragingBuster;
	public float chargePalleteTime;

	bool shootPressed => player.input.isPressed(Control.Shoot, player);
	bool upHeld => player.input.isHeld(Control.Up, player);
	bool specialPressed => player.input.isPressed(Control.Special1, player);

	public RagingChargeX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, int? heartTanks = null, bool isATrans = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, heartTanks, isATrans
	) {
		charId = CharIds.RagingChargeX;

		// Start with 5s spawn leitency.
		selfDamageCooldown = selfDamageMaxCooldown * 4;

		// For easy HUD display we add it to weapon list.
		ragingBuster = new RagingChargeBuster();
		weapons.Add(ragingBuster);
		altSoundId = AltSoundIds.X1;
	}

	public override void preUpdate() {
		base.preUpdate();
		if (!ownedByLocalPlayer) { return; }
		// Cooldowns.
		Helpers.decrementFrames(ref saberCooldown);
		Helpers.decrementFrames(ref parryCooldown);
		Helpers.decrementFrames(ref shootCooldown);
		Helpers.decrementFrames(ref unlimitedcrushCooldown);
		Helpers.decrementFrames(ref kickchargeCooldown);
		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= speedMul;
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
		if (musicSource == null) {
			addMusicSource("introStageBreisX4_JX", getCenterPos(), true);
		}
		// Local-only starts here.
		if (!ownedByLocalPlayer) { return; }
		// Allow cancel normals into parry.
		if (player.input.isWeaponLeftOrRightPressed(player) &&
			parryCooldown == 0 &&
			charState is XUPPunchState or XUPGrabState or X6SaberState
		) {
			enterParry();
		}
		ragingBuster.update();
		ragingBuster.charLinkedUpdate(this, true);
		chargeBusterSound();
		chargeLogic(null);
		player.changeWeaponControls();
	}

	public override bool normalCtrl() {
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		if (player.input.isPressed(Control.WeaponRight, player) && parryCooldown == 0) {
			parryCooldown = 60;
			enterParry();
			return true;
		}
		if (player.input.isPressed(Control.WeaponLeft, player) && unlimitedcrushCooldown == 0) {
			unlimitedcrushCooldown = 150;
			changeState(new XUPUnlimitedCrushState(), true);
			return true;
		}
		if (specialPressed && charState is Dash or AirDash) {
			charState.isGrabbing = true;
			changeSpriteFromName("unpo_grab_dash", true);
			return true;
		}
		// Disparo regular
		if (player.input.isPressed(Control.Shoot, player)) {
			if (ragingBuster.ammo >= ragingBuster.getAmmoUsage(0) && ragingBuster.shootCooldown <= 0) {
				ragingBuster.shootCooldown = 100;
				shoot();
				return true;
			}
		}
		if (saberCooldown <= 0 && specialPressed) {
				saberCooldown = 60;
				if (charState is Crouch) {
					changeState(new XSaberCrouchState(), true);
					return true;
				}
				changeState(new X6SaberState(grounded), true);
				return true;
			}
		// Primera Carga
		if (!player.input.isHeld(Control.Special1, player)) {
			if (chargeLevel == 1) {
				{
					changeState(new XUPPunchState(grounded), true);
					resetCharge();
					return true;
				}
			}
			// Segunda Carga
			if (!player.input.isHeld(Control.Special1, player)) {
				if (chargeLevel == 2) {
					{
						changeState(new XUPChargedPunchState(), true);
						resetCharge();
						return true;
					}
				}
			}
		}
		if (grounded && kickchargeCooldown == 0 && 
            player.input.isHeld(Control.Down, player) && player.input.isHeld(Control.Dash, player)) 
			{
			kickchargeCooldown = 150;
			changeState(new XUPKickChargeState(), true);
			return true;
		}
		return base.attackCtrl();
	}

	// Shoot and set animation if posible.
	public void shoot() {
		if (player.input.isHeld(Control.Up, player)) {
			changeState(new XUPUpShot(), true);
			vel.y = 50f;
			return;
		}
		if (player.input.isHeld(Control.Down, player) && !grounded) {
			changeState(new XUPDownShoot(), true);
			vel.y = -400f;
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
		punchCooldown = 15;
	}
    private void resetCharge() {
		chargeLevel = 0;
		chargeTime = 0;
	}
	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Special1, player);
	}
    private int chargeLevel;
	public override void increaseCharge() {
		chargeTime += Global.speedMul;

		if (isCharging()) {
			if (chargeTime <= 45) {
				chargeLevel = 1; // Nivel de carga básico
			} else if (chargeTime > 90 && chargeTime < 150) {
				chargeLevel = 2; // Nivel maximo
			}
		}
	}

	public void chargeBusterSound() {
		int busterAmmo = (int)ragingBuster.ammo;
		if (isCharging()) {
			if (busterAmmo % 3 == 0 && busterAmmo != lastAmmoSound) {
				if (busterAmmo == 12) {
					playSound("gigaCrushAmmoFull");
				} else if (busterAmmo > 0 && busterAmmo < 12) {
					playSound("gigaCrushAmmoRecharge");
				}
				lastAmmoSound = busterAmmo;
			}
		}
	}

	public override float getDashSpeed() {
		if (sprite.name == "mmx_unpo_grab_dash") {
			return 1.25f * base.getDashSpeed();
		}
		return base.getDashSpeed();
	}

	public override string getSprite(string spriteName) {
		return "mmx_" + spriteName;
	}

	public void enterParry() {
		if (absorbedProj != null) {
			changeState(new XUPParryProjState(absorbedProj, true, false), true);
			player.weapons.RemoveAll(w => w is AbsorbWeapon);
			absorbedProj = null;
			return;
		}
		changeState(new XUPParryStartState(), true);
		return;
	}

	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int chargeType = 0;
			chargeEffect.update(getDisplayChargeLevel(), chargeType);
		}
	}

	public override void addAmmo(float amount) {
		weaponHealAmount += amount;
		ragingBuster.addAmmoHeal(amount);
	}

	public override void addPercentAmmo(float amount) {
		weaponHealAmount += amount * 0.32f;
		ragingBuster.addAmmoPercentHeal(amount);
	}

	public override bool canAddAmmo() {
		return ragingBuster.ammo < ragingBuster.maxAmmo;
	}

	public override bool isNonDamageStatusImmune() {
		if (isATrans) return false;
		return true;
	}

	public enum MeleeIds {
		None = -1,
		DashGrab,
		ParryBlock,
		UnlimitedCrush,
		Punch,
		PunchCharged,
		KickCharge,
		ZSaber,
		MaxZSaber,
	}
	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"mmx_beam_saber2" or "mmx_beam_saber_air2" or "mmx_beam_saber_crouch" => MeleeIds.ZSaber,
			"mmx_unpo_grab_dash" => MeleeIds.DashGrab,
			"mmx_unpo_punch" or "mmx_unpo_air_punch" => MeleeIds.Punch,
			"mmx_unpo_punch2" => MeleeIds.PunchCharged,
			"mmx_unpo_slide" => MeleeIds.KickCharge,
			"mmx_unpo_parry_start" => MeleeIds.ParryBlock,
			"mmx_unpo_gigga" => MeleeIds.UnlimitedCrush,
			"mmx_beam_saber" or "mmx_beam_saber_air" => MeleeIds.MaxZSaber,
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
				XUPParry.netWeapon, projPos, ProjIds.UPParryBlock, player,
				0, 0, 60, addToLevel: addToLevel
			),
			(int)MeleeIds.UnlimitedCrush => new GenericMeleeProj(
				XUPUnlimitedCrush.netWeapon, projPos, ProjIds.UnlimitedCrush, player,
				1, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.Punch => new GenericMeleeProj(
				XUPPunch.netWeapon, projPos, ProjIds.UPPunch, player,
				3, Global.defFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.PunchCharged => new GenericMeleeProj(
	             ZXSaber.netWeapon, projPos, ProjIds.UPPunchCharged, player,
	             3, Global.defFlinch, 20, addToLevel: addToLevel
			),
			(int)MeleeIds.KickCharge => new GenericMeleeProj(
				XUPKickCharge.netWeapon, projPos, ProjIds.UPKickCharge, player,
				3, Global.halfFlinch, 45, isDeflectShield: true, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				3, 0, 30, isZSaberEffect: true, addToLevel: addToLevel
			),
			(int)MeleeIds.MaxZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				3, Global.halfFlinch, 30, addToLevel: addToLevel,
				isZSaberEffect: true
			),
			_ => null
		};
		return proj;
	}

	public override void render(float x, float y) {
		// Charge timers
		if (isCharging()) {
			chargePalleteTime += Global.gameSpeed;
		} else {
			chargePalleteTime = 0;
		}
		base.render(x, y);
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = [];
		ShaderWrapper? palette = null;

		List<ShaderWrapper?> chargePalletes = getChargeShaders() as List<ShaderWrapper?>;
		if (chargePalletes.Count > 0) {
			chargePalletes.Add(null);
			ShaderWrapper? targetChargePallete = chargePalletes[MathInt.Floor(
				(chargePalleteTime % (chargePalletes.Count * 2)) / 2f
			)];
			if (targetChargePallete != null) {
				palette = targetChargePallete;
			}
		}
		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}
		shaders.AddRange(baseShaders);
		return shaders;
	}

	public List<ShaderWrapper> getChargeShaders() {
		if (!isCharging()) {
			return [];
		}
		List<ShaderWrapper> chargePalletes = new();
		int chargeLevel = getDisplayChargeLevel();
		if (chargeLevel > 0) {
			chargePalletes.Add(getDisplayChargeLevel() switch {
				1 => Player.XBlueC,
				2 => Player.XYellowC,
				3 => Player.XPinkC,
				_ => Player.XGreenC,
			});
		}
		return chargePalletes;
	}

	public override int getDisplayChargeLevel() {
		return Helpers.clamp(MathInt.Ceiling(ragingBuster.ammo / ragingBuster.getAmmoUsage(0)), 1, 4);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();
		customData.Add((byte)MathF.Floor(ragingBuster.ammo));
		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];
		// Per-player data.
		ragingBuster.ammo = data[0];
	}
}
