using MMXOnline;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SFML.Graphics;

public class EstadoCargaHandler {
	public EstadoCarga estadoCarga { get; private set; }
	private Dictionary<EstadoCarga, ShaderWrapper> shadersPorEstado;
	private Dictionary<EstadoCarga, string> sonidosPorEstado;
	private Stopwatch temporizador;
	private LoopingSound? sonidoActual; // Referencia al sonido en curso
	private ShaderWrapper? shaderPrioritario; // Shader prioritario

	public EstadoCargaHandler() {
		estadoCarga = EstadoCarga.CargaAlta;

		// Inicializa los shaders para cada estado
		shadersPorEstado = new Dictionary<EstadoCarga, ShaderWrapper> {
			{ EstadoCarga.CargaMaxima, Player.XGreenC }, // Shader para carga máxima
            { EstadoCarga.CargaAlta, Player.XBlueC }    // Shader para carga alta
        };

		// Inicializa los sonidos para cada estado
		sonidosPorEstado = new Dictionary<EstadoCarga, string> {
			{ EstadoCarga.CargaMaxima, "charge_start" },
			{ EstadoCarga.CargaAlta, "charge_loop" }
		};

		// Inicializa el temporizador
		temporizador = new Stopwatch();
		temporizador.Start();
	}

	public void IncrementarCarga(Character character) {
		// Verifica si han pasado al menos 1500 milisegundos
		if (temporizador.ElapsedMilliseconds >= 1500) {
			// Incrementa el estado de carga si no ha alcanzado el máximo
			if (estadoCarga < EstadoCarga.CargaMaxima) {
				estadoCarga++;
				Console.WriteLine($"Estado de carga incrementado a: {estadoCarga}");
				AplicarShaderYSonido(character); // Aplica el shader y reproduce el sonido
			}

			// Reinicia el temporizador
			temporizador.Restart();
		}
	}

	public void ReiniciarCarga() {
		estadoCarga = EstadoCarga.CargaAlta;
		temporizador.Restart(); // Reinicia el temporizador al reiniciar la carga
		DetenerSonido(); // Detiene el sonido en curso
		Console.WriteLine("Estado de carga reiniciado a CargaAlta.");
	}

	public void EstablecerShaderPrioritario(ShaderWrapper shader) {
		shaderPrioritario = shader;
		Console.WriteLine($"Shader prioritario establecido: {shader}");
	}

	public void LimpiarShaderPrioritario() {
		shaderPrioritario = null;
		Console.WriteLine("Shader prioritario eliminado.");
	}

	private void AplicarShaderYSonido(Character character) {
		// Obtiene la lista de shaders para el estado de carga actual
		List<ShaderWrapper> chargeShaders = GetChargeShaders();

		// Si hay un shader prioritario, lo aplica primero
		if (shaderPrioritario != null) {
			chargeShaders.Clear(); // Limpia cualquier shader basado en la carga
			chargeShaders.Add(shaderPrioritario); // Aplica el shader prioritario
			Console.WriteLine($"Shader prioritario aplicado: {shaderPrioritario}");
		}

		// Aplica los shaders al jugador
		var shaderWrappers = character.getShaders();
		if (shaderWrappers != null) {
			shaderWrappers.Clear(); // Limpia los shaders actuales
			shaderWrappers.AddRange(chargeShaders); // Asigna los nuevos shaders
			Console.WriteLine($"Shaders aplicados al jugador: {string.Join(", ", chargeShaders)}");
		} else {
			Console.WriteLine("No se pudo obtener la lista de shaders del jugador.");
		}

		// Reproduce el sonido correspondiente al estado de carga
		if (sonidosPorEstado.TryGetValue(estadoCarga, out var sonido)) {
			ReproducirSonido(character, sonido);
		} else {
			Console.WriteLine($"No se encontró un sonido para el estado de carga: {estadoCarga}");
		}
	}

	private void ReproducirSonido(Character character, string sonido) {
		// Lógica para reproducir el sonido
		if (!string.IsNullOrEmpty(sonido)) {
			DetenerSonido(); // Detiene cualquier sonido en curso antes de reproducir uno nuevo
			sonidoActual = new LoopingSound(sonido, sonido, character); // Crea un nuevo sonido en bucle
			sonidoActual.play();
			Console.WriteLine($"Sonido {sonido} reproducido para el estado de carga.");
		} else {
			Console.WriteLine("El sonido proporcionado está vacío o es nulo.");
		}
	}

	private void DetenerSonido() {
		// Detiene el sonido en curso si existe
		if (sonidoActual != null) {
			sonidoActual.stopRev(0); // Detiene el sonido suavemente
			sonidoActual = null;
			Console.WriteLine("Sonido detenido.");
		}
	}

	public List<ShaderWrapper> GetChargeShaders() {
		List<ShaderWrapper> chargeShaders = new();
		if (shadersPorEstado.TryGetValue(estadoCarga, out var shader)) {
			chargeShaders.Add(shader);
		}

		// Ejemplo de lógica adicional para agregar más shaders según condiciones
		if (estadoCarga == EstadoCarga.CargaMaxima) {
			chargeShaders.Add(Player.XOrangeC); // Agrega un shader adicional para CargaMaxima
		}

		return chargeShaders;
	}
}

