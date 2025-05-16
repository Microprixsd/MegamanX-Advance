using MMXOnline;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class EstadoCargaHandler {
	public EstadoCarga estadoCarga { get; private set; }
	private Dictionary<EstadoCarga, ShaderWrapper> shadersPorEstado;
	private Dictionary<EstadoCarga, string> sonidosPorEstado;
	private Stopwatch temporizador;
	private LoopingSound? sonidoActual; // Referencia al sonido en curso
	private ShaderWrapper? shaderPrioritario; // Shader prioritario

	// Instancia única para garantizar que nunca sea null
	private static EstadoCargaHandler? instance;

	public static EstadoCargaHandler GetInstance() {
		if (instance == null) {
			instance = new EstadoCargaHandler();
		}
		return instance;
	}

	private EstadoCargaHandler() {
		estadoCarga = EstadoCarga.CargaAlta;

		shadersPorEstado = new Dictionary<EstadoCarga, ShaderWrapper> {
			{ EstadoCarga.CargaMaxima, Player.XGreenC },
			{ EstadoCarga.CargaAlta, Player.XYellowC }
		};

		sonidosPorEstado = new Dictionary<EstadoCarga, string> {
			{ EstadoCarga.CargaMaxima, "charge_start" },
			{ EstadoCarga.CargaAlta, "" }
		};

		temporizador = new Stopwatch();
		temporizador.Start();
	}

	public void IncrementarCarga(Player player, Character character) {
		if (player == null || character == null) {
			Console.WriteLine("Error: player o character es null en IncrementarCarga()");
			return;
		}

		if (temporizador.ElapsedMilliseconds >= 3000) {
			estadoCarga = (EstadoCarga)Math.Min((int)estadoCarga + 1, (int)EstadoCarga.CargaMaxima);
			AplicarShaderYSonido(player, character);
			temporizador.Restart();
		}
	}

	public void ReiniciarCarga() {
		estadoCarga = EstadoCarga.CargaAlta;
		temporizador.Restart();
		DetenerSonido();
	}

	public void EstablecerShaderPrioritario(ShaderWrapper shader) {
		shaderPrioritario = shader;
	}

	public void LimpiarShaderPrioritario() {
		shaderPrioritario = null;
	}

	private void AplicarShaderYSonido(Player player, Character character) {
		if (character == null) {
			Console.WriteLine("Error: character es null en AplicarShaderYSonido()");
			return;
		}

		List<ShaderWrapper> chargeShaders = GetChargeShaders();

		if (shaderPrioritario != null) {
			chargeShaders.Clear();
			chargeShaders.Add(shaderPrioritario);
		}

		var shaderWrappers = character.getShaders();
		if (shaderWrappers != null) {
			shaderWrappers.Clear();
			shaderWrappers.AddRange(chargeShaders);
		}

		if (sonidosPorEstado.TryGetValue(estadoCarga, out var sonido)) {
			ReproducirSonido(character, sonido);
		}
	}

	private void ReproducirSonido(Character character, string sonido) {
		if (!string.IsNullOrEmpty(sonido)) {
			DetenerSonido();
			sonidoActual = new LoopingSound(sonido, sonido, character);
			sonidoActual.play();
		}
	}

	private void DetenerSonido() {
		if (sonidoActual != null) {
			sonidoActual.stopRev(0);
			sonidoActual = null;
		}
	}

	public List<ShaderWrapper> GetChargeShaders() {
		List<ShaderWrapper> chargeShaders = new();
		if (shadersPorEstado.TryGetValue(estadoCarga, out var shader)) {
			chargeShaders.Add(shader);
		}

		if (estadoCarga == EstadoCarga.CargaMaxima) {
			chargeShaders.Add(Player.XOrangeC);
		}

		return chargeShaders;
	}
}
