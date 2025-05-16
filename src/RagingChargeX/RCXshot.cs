using MMXOnline;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using static MMXOnline.XUnpoProjBase;
using System.Diagnostics.CodeAnalysis;
using static MMXOnline.AssassinBulletTrailAnim;
using static MMXOnline.PlayershotDownAnim;
public class RCXupshot : CharState {
	public bool shoot;
	public bool grounded;
	public EstadoCargaHandler cargaHandler;
	private float elapsedTime; // Temporizador interno

	public RCXupshot(Player player, EstadoCargaHandler cargaHandler) : base("mmx_unpo_up_shot") {
		this.shoot = false;
		this.cargaHandler = cargaHandler;
		this.elapsedTime = 0f; // Inicializar el temporizador
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

		// Cambiar el sprite del jugador al entrar en el estado, solo si es diferente
		if (character != null) {
			string spriteName = character.grounded ? "mmx_unpo_up_shot" : "mmx_unpo_up_air_shot";
			if (character.sprite.name != spriteName) {
				character.changeSprite(spriteName, resetFrame: true);
			}
		}
	}

	public override void update() {
		base.update();

		// Incrementar el temporizador
		elapsedTime += Global.spf;

		// Obtén el estado de carga actual
		EstadoCarga estadoCarga = cargaHandler.estadoCarga;

		// Ejecuta la lógica de disparo solo si el frame es suficiente, no se ha disparado y el estado de carga no es "InicioCarga"
		if (character.frameIndex >= 0 && !shoot && estadoCarga >= EstadoCarga.CargaAlta) {
			shoot = true; // Marca que ya se disparó

			// Crea el proyectil correspondiente al estado de carga
			switch (estadoCarga) {
				case EstadoCarga.CargaAlta:
					character.vel.y = 25f;
					new XUnpoProjFuerte(
						character,
						character.getShootPos(DireccionDisparo.Arriba),
						1,
						player.getNextActorNetId(),
						DireccionDisparo.Arriba,
						sendRpc: true
					);
					character.playSound("buster2X3", true);
					break;

				case EstadoCarga.CargaMaxima:
					character.vel.y = 50f;
					new XUnpoProjMaximo(
						character,
						character.getShootPos(DireccionDisparo.Arriba),
						1,
						player.getNextActorNetId(),
						DireccionDisparo.Arriba,
						sendRpc: true
					);
					character.playSound("plasmaShot", true);
					break;
			}

			// Reinicia la carga después de disparar
			cargaHandler.ReiniciarCarga();
		}

		// Si la animación terminó y han pasado al menos 0.5 segundos, cambia al estado idle o fall
		if (character.isAnimOver() && elapsedTime >= 0.3f) {
			character.changeToIdleOrFall();
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);

		// Cambiar el sprite del jugador al salir del estado, solo si es diferente
		if (character != null && character.sprite.name != "mmx_idle") {
			character.changeSprite("mmx_idle", resetFrame: true); // Cambia al sprite de idle o el que corresponda
		}
	}
}
public class RCXDownShot : CharState {
	public bool shoot;
	public bool grounded;
	public EstadoCargaHandler cargaHandler;
	private float elapsedTime; // Temporizador interno

	public RCXDownShot(Player player, EstadoCargaHandler cargaHandler) : base("mmx_unpo_down_shot") {
		this.shoot = false;
		this.cargaHandler = cargaHandler;
		this.elapsedTime = 0f; // Inicializar el temporizador
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

		// Cambiar el sprite del jugador al entrar en el estado
		if (character != null) {
			string spriteName = "mmx_unpo_down_shot";
			if (character.sprite.name != spriteName) {
				character.changeSprite(spriteName, resetFrame: false);
			}

			// Instanciar la animación de disparo hacia abajo
			new PlayershotDownAnim(
				character.pos,
				character,
				spriteName,
				netId: character.player.getNextActorNetId(),
				sendRpc: true,
				ownedByLocalPlayer: character.ownedByLocalPlayer
			);
		}
	}

	public override void update() {
		base.update();

		// Incrementar el temporizador
		elapsedTime += Global.spf;

		// Obtén el estado de carga actual
		EstadoCarga estadoCarga = cargaHandler.estadoCarga;

		// Ejecuta la lógica de disparo solo si el frame es suficiente, no se ha disparado y el estado de carga no es "InicioCarga"
		if (character.frameIndex >= 0 && !shoot && estadoCarga >= EstadoCarga.CargaAlta) {
			shoot = true; // Marca que ya se disparó

			// Crea el proyectil correspondiente al estado de carga
			switch (estadoCarga) {
				case EstadoCarga.CargaAlta:
					character.vel.y = -200f;
					new XUnpoProjFuerte(
						character,
						character.getShootPos(DireccionDisparo.Abajo),
						1,
						player.getNextActorNetId(),
						DireccionDisparo.Abajo,
						sendRpc: true
					);
					character.playSound("buster2X3", true);
					break;

				case EstadoCarga.CargaMaxima:
					character.vel.y = -250f;
					new XUnpoProjMaximo(
						character,
						character.getShootPos(DireccionDisparo.Abajo),
						1,
						player.getNextActorNetId(),
						DireccionDisparo.Abajo,
						sendRpc: true
					);
					character.playSound("plasmaShot", true);
					break;
			}

			// Reinicia la carga después de disparar
			cargaHandler.ReiniciarCarga();
		}

		// Si la animación terminó y han pasado al menos 0.5 segundos, cambia al estado idle o fall
		if (character.isAnimOver() && elapsedTime >= 0.5f) {
			character.changeToIdleOrFall();
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);

		// Cambiar el sprite del jugador al salir del estado, solo si es diferente
		if (character != null && character.sprite.name != "mmx_idle") {
			character.changeSprite("mmx_idle", resetFrame: true); // Cambia al sprite de idle o el que corresponda
		}
	}
}
