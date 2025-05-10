using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MMXOnline.XUnpoProjBase;
using static RCXupshot;

namespace MMXOnline;
public class RagingChargeX : Character {
	public int shotCount;
	public float punchCooldown;
	public float saberCooldown;
	public float Parrycooldowm;
	public bool doSelfDamage;
	public float selfDamageCooldown;
	public float selfDamageMaxCooldown = 120;
	public Projectile? absorbedProj;
	public RagingChargeBuster ragingBuster;
	public float busterupcooldown;
	public float busterdowncooldown;
	public override void update() {
		base.update();
		// Reducir el saberCooldown gradualmente hasta llegar a 0
		if (saberCooldown > 0) {
			saberCooldown -= Global.spf; // Reduce el cooldown en segundos
			if (saberCooldown < 0) {
				saberCooldown = 0; // Asegurarse de que no sea negativo
			}
		}

		// Reducir el punchCooldown gradualmente hasta llegar a 0
		if (punchCooldown > 0) {
			punchCooldown -= Global.spf; // Reduce el cooldown en segundos
			if (punchCooldown < 0) {
				punchCooldown = 0; // Asegurarse de que no sea negativo
			}
		}
		if (busterupcooldown > 0) {
			busterupcooldown -= Global.spf; // Reduce el cooldown en segundos
			if (busterupcooldown < 0) {
				busterupcooldown = 0; // Asegurarse de que no sea negativo
			}
		}
		if (busterdowncooldown > 0) {
			busterdowncooldown -= Global.spf; // Reduce el cooldown en segundos
			if (busterdowncooldown < 0) {
				busterdowncooldown = 0; // Asegurarse de que no sea negativo
			}
		}
		if (Parrycooldowm > 0) {
			Parrycooldowm -= Global.spf; // Reduce el cooldown en segundos
			if (Parrycooldowm < 0) {
				Parrycooldowm = 0; // Asegurarse de que no sea negativo
			}
		}
	}
        // Otras actualizaciones específicas de RagingChargeX
        // ...
	public override string getSprite(string spriteName) {
		return "mmx_" + spriteName;
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
	// Agregar una instancia de EstadoCargaHandler
	private EstadoCargaHandler estadoCargaHandler;

	public RagingChargeX(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.RagingChargeX;

		// Inicializar EstadoCargaHandler
		estadoCargaHandler = new EstadoCargaHandler();

		// Start with 5s spawn leitency.
		selfDamageCooldown = selfDamageMaxCooldown * 4;

		// For easy HUD display we add it to weapon list.
		ragingBuster = new RagingChargeBuster();
		weapons.Add(ragingBuster);
		altSoundId = AltSoundIds.X1;
	}

	public override bool attackCtrl() {
		// 🔥 Prioridad 1: Acciones sin carga
		if (player.input.isPressed(Control.Special1, player) && saberCooldown <= 0 && player.input.isHeld(Control.Down, player)) {
			saberCooldown = 1; // Reinicia el cooldown del sable
			changeState(new X6SaberState(grounded), true);
			return true;
		}else if (player.input.isPressed(Control.Special1, player) && punchCooldown <= 0) {
			punchCooldown = 1; // Establece un cooldown para los golpes
			changeState(new XUPPunchState(grounded), true);
			return true;
		}
		if (player.input.isWeaponLeftOrRightPressed(player) && Parrycooldowm == 0 ) {
			Parrycooldowm = 2;
			enterParry();
			return true;
		}

		// 🔥 Prioridad 2: Disparos cargados
		if (player.input.isPressed(Control.Shoot, player) && estadoCargaHandler.estadoCarga == EstadoCarga.CargaAlta) {
			if (player.input.isHeld(Control.Up, player) && busterupcooldown ==  0) {
				// Cambia al estado de disparo hacia arriba
				busterupcooldown = 1;
				changeState(new RCXupshot(player, estadoCargaHandler), true);
				return true;
			} else if (player.input.isHeld(Control.Down, player) && !grounded && busterdowncooldown == 0) {
				// Cambia al estado de disparo hacia abajo
				busterdowncooldown = 1;
				changeState(new RCXDownShot(player, estadoCargaHandler), true);
				return true;
			}
		}

		// 🔥 Prioridad 3: Disparos cargados liberados
		if (!player.input.isHeld(Control.Shoot, player) && estadoCargaHandler.estadoCarga > EstadoCarga.CargaAlta) {
			if (player.input.isHeld(Control.Up, player)) {
				// Cambia al estado de disparo hacia arriba
				changeState(new RCXupshot(player, estadoCargaHandler), true);
				return true;
			} else if (player.input.isHeld(Control.Down, player) && !grounded) {
				// Cambia al estado de disparo hacia abajo
				changeState(new RCXDownShot(player, estadoCargaHandler), true);
				return true;
			}
		}

		// 🔥 Prioridad 4: Incrementar carga
		if (player.input.isHeld(Control.Shoot, player)) {
			
			estadoCargaHandler.IncrementarCarga(this); // Pasa la instancia actual de Character
			return true; // Evita que otras acciones interfieran mientras se carga
		}

		// Si ninguna acción fue realizada, llama al método base
		return base.attackCtrl();
	}


	// Si ninguna acción fue realizada, llama al método base
	public override bool isNonDamageStatusImmune() {
		return true;
	}

	public bool isDecayImmune() {
		return (
			charState is XUPGrabState
			or XUPParryMeleeState
			or XUPParryProjState
			or Hurt
			or GenericStun
			or VileMK2Grabbed
			or GenericGrabbedState
		);
	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"mmx_beam_saber2" or "mmx_beam_saber_air2" => MeleeIds.ZSaber,
			"mmx_unpo_grab_dash" => MeleeIds.DashGrab,
			"mmx_unpo_punch" or "mmx_unpo_air_punch" => MeleeIds.Punch,
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
				3, Global.defFlinch, 30, addToLevel: addToLevel
			),
			(int)MeleeIds.ZSaber => new GenericMeleeProj(
				ZXSaber.netWeapon, projPos, ProjIds.X6Saber, player,
				3, Global.halfFlinch, 30, addToLevel: addToLevel
			),
			_ => null
		};
		return proj;
	}
}
	public enum MeleeIds {
		None = -1,
		DashGrab,
		ParryBlock,
		Punch,
		ZSaber,
	}
