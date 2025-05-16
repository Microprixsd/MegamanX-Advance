using MMXOnline;
using System;
using System.Collections.Generic;
using SFML.Graphics;
using System.Diagnostics;
namespace MMXOnline;
public class RagingChargeX : Character {
	public int shotCount;
	public float punchCooldown;
	public float saberCooldown;
	public float parryCooldown;
	public float maxParryCooldown = 30;
	public bool doSelfDamage;
	public float selfDamageCooldown;
	public float selfDamageMaxCooldown = 120;
	public Projectile? absorbedProj;
	public RagingChargeBuster ragingBuster;
	public EstadoCargaHandler cargaHandler;
	public float bustercooldown;
	public float busterupcooldown;
	public float busterdowncooldown;

	// Temporizador para la animación de disparo (se aplica cuando se muestra una animación con "_shoot")
	private float animacionRestaurarTimer = 0.5f;
	// Se compara por tipo para detectar cambios reales de estado.
	private CharState oldstate;
	private bool enAnimacionDisparo = false;

	public override string getSprite(string spriteName) {
		frameSpeed = 1;
		return "mmx_" + spriteName;
	}

	public RagingChargeX(Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true)
		: base(player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn) {
		charId = CharIds.RagingChargeX;
		cargaHandler = EstadoCargaHandler.GetInstance();
		selfDamageCooldown = selfDamageMaxCooldown * 4;
	}

	// Cambia la animación sin reiniciar el contador de frames.
	// Ahora ya NO reinicia el timer, dejándolo al valor que ya tenga.
	public void cambiarAnimacion(string nuevoSprite, float duracion) {
		if (!string.IsNullOrEmpty(nuevoSprite) && sprite != null && sprite.name != nuevoSprite) {
			changeSprite(nuevoSprite, resetFrame: false);
			// Aquí ya no se reinicia el timer:
			// animacionRestaurarTimer = duracion;
		}
	}

	// TRANSICIONES DE DISPARO (usando pattern matching)
	private string getTransitionStartShootSprite(CharState charState) {
		if (charState == null || sprite == null)
			return sprite?.name ?? "mmx_idle";
		return charState switch {
			Fall _ => "mmx_fall_start_shoot",
			Jump _ => "mmx_jump_start_shoot",
			_ => sprite.name
		};
	}
	private string getTransitionEndShootSprite(CharState charState) {
		if (charState == null || sprite == null)
			return sprite?.name ?? "mmx_idle";
		return charState switch {
			Dash _ => "mmx_dash_end_shoot",
			_ => sprite.name
		};
	}

	// TRANSICIONES NORMALES (usando pattern matching)
	private string getTransitionStartSprite(CharState charState) {
		if (charState == null || sprite == null)
			return sprite?.name ?? "mmx_idle";
		return charState switch {
			Fall _ => "mmx_fall_start",
			Jump _ => "mmx_jump_start",
			Crouch _ => "mmx_crouch_start",
			_ => sprite.name
		};
	}
	private string getTransitionEndSprite(CharState charState) {
		if (charState == null || sprite == null)
			return sprite?.name ?? "mmx_idle";
		return charState switch {
			Dash _ => "mmx_dash_end",
			_ => sprite.name
		};
	}

	// Devuelve la animación "normal" (sin el sufijo de disparo) correspondiente al estado.
	private string getAnimacionPorEstado(CharState charState) {
		if (sprite == null)
			return "mmx_idle";
		return charState switch {
			Idle _ => "mmx_weak",
			Run _ => "mmx_run",
			WallKick _ => "mmx_wall_kick",
			WallSlide _ => "mmx_wall_slide",
			Fall _ => "mmx_fall",
			Hover _ => "mmx_hover",
			Jump _ => "mmx_jump",
			Dash _ => "mmx_dash",
			Crouch _ => "mmx_crouch",
			_ => sprite.name
		};
	}

	// A partir de la animación base, devuelve la versión con el sufijo "_shoot".
	private string getAnimacionDisparo(string spriteName) {
		Dictionary<string, string> estadosDisparo = new Dictionary<string, string>() {
			{ "mmx_weak", "mmx_shoot" },
			{ "mmx_run", "mmx_run_shoot" },
			{ "mmx_wall_kick", "mmx_wall_kick_shoot" },
			{ "mmx_wall_slide", "mmx_wall_slide_shoot" },
			{ "mmx_fall", "mmx_fall_shoot" },
			{ "mmx_fall_start", "mmx_fall_start_shoot" },
			{ "mmx_hover", "mmx_hover_shoot" },
			{ "mmx_jump", "mmx_jump_shoot" },
			{ "mmx_jump_start", "mmx_jump_start_shoot" },
			{ "mmx_land", "mmx_land_shoot" },
			{ "mmx_dash", "mmx_dash_shoot" },
			{ "mmx_dash_end", "mmx_dash_end_shoot" },
			{ "mmx_crouch_start", "mmx_crouch_start_shoot" },
			{ "mmx_crouch", "mmx_crouch_shoot" },
		};
		return estadosDisparo.TryGetValue(spriteName, out string animacionDisparo) ? animacionDisparo : spriteName;
	}

	public override void update() {
		base.update();

		// Reducir cooldowns.
		bustercooldown = Math.Max(0, bustercooldown - 1f);
		busterupcooldown = Math.Max(0, busterupcooldown - 1f);
		busterdowncooldown = Math.Max(0, busterdowncooldown - 1f);
		punchCooldown = Math.Max(0, punchCooldown - 1f);
		saberCooldown = Math.Max(0, saberCooldown - 1f);
		parryCooldown = Math.Max(0, parryCooldown - 1f);

		// Si se terminó la animación de dash (sin disparo) se invoca la transición para cerrar el dash.
		if (sprite != null && sprite.name == "mmx_dash_end") {
			changeToIdleOrFall();
		}

		// Manejamos el modo disparo. NOTA: Asegúrate de que enAnimacionDisparo se active únicamente
		// cuando realmente se dispare; por ejemplo, al llamar al método disparar().
		if (enAnimacionDisparo && sprite != null) {
			// Obtenemos la animación base normal para el estado actual y luego la versión disparo.
			string baseAnim = getAnimacionPorEstado(charState);
			string animacionDisparo = getAnimacionDisparo(baseAnim);
			// Si el sprite actual no tiene la versión disparo, lo cambiamos.
			if (sprite.name != animacionDisparo) {
				cambiarAnimacion(animacionDisparo, 0.5f);
			}
			// Solo se espera 0.5s para la animación de disparo, luego se vuelve a la normal.
			if (sprite.name.Contains("_shoot")) {
				animacionRestaurarTimer -= Global.spf;
				if (animacionRestaurarTimer <= 0) {
					string animacionNormal = getAnimacionPorEstado(charState);
					if (!string.IsNullOrEmpty(animacionNormal) && sprite.name != animacionNormal) {
						cambiarAnimacion(animacionNormal, 0.0f);
					}
					enAnimacionDisparo = false;
					animacionRestaurarTimer = 0.5f; // Reiniciar el timer para la próxima vez.
				}
			}
		} else {
			// Si no estamos en modo disparo, verificamos cambios de estado reales para aplicar transiciones.
			if (charState?.GetType() != oldstate?.GetType()) {
				bool shootMode = false;
				// Solo activar el modo disparo si el nuevo estado no es el de salto o caída.
				if (!(charState is Jump || charState is Fall)) {
					string nuevaAnimacionShoot = getTransitionStartShootSprite(charState);
					if (!string.IsNullOrEmpty(nuevaAnimacionShoot) && sprite != null && nuevaAnimacionShoot != sprite.name) {
						shootMode = true;
					}
				}
				enAnimacionDisparo = shootMode;
				// Aplicamos las transiciones correspondientes:
				float duracionShoot = 0.2f;
				string transitionSpriteEnd = shootMode ? getTransitionEndShootSprite(oldstate) : getTransitionEndSprite(oldstate);
				if (!string.IsNullOrEmpty(transitionSpriteEnd) && sprite != null && transitionSpriteEnd != sprite.name) {
					cambiarAnimacion(transitionSpriteEnd, duracionShoot);
				}
				string transitionSpriteStart = shootMode ? getTransitionStartShootSprite(charState) : getTransitionStartSprite(charState);
				if (!string.IsNullOrEmpty(transitionSpriteStart) && sprite != null && transitionSpriteStart != sprite.name) {
					cambiarAnimacion(transitionSpriteStart, duracionShoot);
				}
				oldstate = charState;
			}

		}
	}

	public void disparar() {
		DireccionDisparo direccion = DireccionDisparo.Frente;
		EstadoCarga estadoCarga = cargaHandler.estadoCarga;
		string animacionDisparo = getAnimacionDisparo(sprite.name);
		string animacionStartShoot = getTransitionStartShootSprite(charState);
		string animacionEndShoot = getTransitionEndShootSprite(oldstate);
		// Activamos el modo disparo.
		enAnimacionDisparo = true;
		// Seleccionamos la animación de disparo o sus transiciones, según corresponda.
		if (!string.IsNullOrEmpty(animacionStartShoot) && animacionStartShoot != sprite.name) {
			cambiarAnimacion(animacionStartShoot, 0.5f);
			enAnimacionDisparo = true;
		} else if (!string.IsNullOrEmpty(animacionEndShoot) && animacionEndShoot != sprite.name) {
			cambiarAnimacion(animacionEndShoot, 0.5f);
			enAnimacionDisparo = true;
		} else if (!string.IsNullOrEmpty(animacionDisparo) && animacionDisparo != sprite.name) {
			cambiarAnimacion(animacionDisparo, 0.5f);
			enAnimacionDisparo = true;
		} else {
			enAnimacionDisparo = false;
		}

		if (estadoCarga == EstadoCarga.CargaAlta) {
			new XUnpoProjBase.XUnpoProjFuerte(this, getShootPos(direccion), xDir, player.getNextActorNetId(), direccion, sendRpc: true);
			playSound("buster2X3", true);
		} else if (estadoCarga == EstadoCarga.CargaMaxima) {
			new XUnpoProjBase.XUnpoProjMaximo(this, getShootPos(direccion), xDir, player.getNextActorNetId(), direccion, sendRpc: true);
			playSound("plasmaShot", true);
		}
		cargaHandler.ReiniciarCarga();
	}



	public override bool attackCtrl() {
		CharState charState;
		EstadoCarga estadoCarga = cargaHandler.estadoCarga;
		if (player.input.isPressed(Control.WeaponRight, player) && parryCooldown == 0) {
			parryCooldown = 4;
			enterParry();
			return true;
		}

		// Disparo cargado en diferentes direcciones
		if (player.input.isPressed(Control.Shoot, player)) {
			if (estadoCarga == EstadoCarga.CargaAlta) {
				if (player.input.isHeld(Control.Up, player) && busterupcooldown == 0) {
					busterupcooldown = 120f;
					changeState(new RCXupshot(player, cargaHandler));
					return true;
				}
				if (player.input.isHeld(Control.Down, player) && busterdowncooldown == 0 && !grounded) {
					busterdowncooldown = 120f;
					changeState(new RCXDownShot(player, cargaHandler));
					return true;
				}
				if (bustercooldown == 0) {
					bustercooldown = 120f;
					disparar();
					return true;
				}
			}
		}
		else {
			if (estadoCarga == EstadoCarga.CargaMaxima && !player.input.isHeld(Control.Shoot, player)) {

				if (player.input.isHeld(Control.Up, player)) {
					changeState(new RCXupshot(player, cargaHandler));
				} else if (player.input.isHeld(Control.Down, player) && !grounded) {
					changeState(new RCXDownShot(player, cargaHandler));
				} else {
					disparar();
					return true;
				}
			}
		}
			// Incrementar carga si se mantiene presionado Shoot
			if (player.input.isHeld(Control.Shoot, player)) {
				cargaHandler.IncrementarCarga(player, this);
				return true;
			}

			// Especiales
			if (player.input.isPressed(Control.Special1, player)) {
				if (saberCooldown == 0 && player.input.isHeld(Control.Down, player)) {
					saberCooldown = 180f;
					changeState(new X6SaberState(grounded), true);
					return true;
				}
				if (punchCooldown == 0) {
					punchCooldown = 120f;
					changeState(new XUPPunchState(grounded), true);
					return true;
				}
			}

			return base.attackCtrl();
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

	public override bool isNonDamageStatusImmune() {
		return true;
	}

	public bool isDecayImmune() {
		return charState is XUPGrabState or XUPParryMeleeState or XUPParryProjState or Hurt or GenericStun or VileMK2Grabbed or GenericGrabbedState;
	}
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
	} }

	public enum MeleeIds {
		None = -1,
		DashGrab,
		ParryBlock,
		Punch,
		ZSaber,
	}
