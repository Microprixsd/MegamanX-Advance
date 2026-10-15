# Integración del original en MegamanX-Advance

Estado: **integración de código realizada y comprobaciones automáticas aprobadas; pendientes las pruebas en partida**. No se hizo commit ni se ejecutó el juego.

Fuente: la copia local `MegamanX-Online-CE-main` suministrada por el usuario. No se descargó una versión distinta.

## Decisiones conservadas

- X: UP, armas y balance del fork, armas X4 de `srcnew`, Force Armor, chips, Hypercharge y tanques compartidos con UP.
- Zero: movimiento, altura, daño, tiempos de ataques y guardia del fork. Su selección de Tenshouha se corrigió tanto al aparecer como al terminar el modo Hyper; la validación conserva su ID interno 5.
- Punchy Zero: Tenshouha habilitado en el cuarto espacio del menú, con icono, descripción, guardado y selección/restauración correctos. Sus índices de menú 0–3 se mantienen separados de los IDs internos de Zero. Se conservaron las clases adicionales de Giga Attacks del fork.
- Axl transformado en Zero: controles de guardia del original, manteniendo los propios de Zero normal.
- HUD horizontal y valores de vida/monedas de 16 bits.
- Las cuatro ediciones locales anteriores al merge se preservaron. Se compararon 64 archivos de X, UP, `srcnew` y ataques de Zero con el respaldo inicial, sin diferencias inesperadas. `Zero.cs` se revisó aparte por sus correcciones autorizadas de selección y guardia.

## Cambios integrados

- Axl: fábricas de los siete tipos de munición, Copy Shot y munición comprada, transformación Hyper, zoom/disparo, armas y A-Trans del original.
- Sigma: identificación de Viral Sigma; aparición y transformación en Wolf Sigma; armas y correcciones de sus Mavericks.
- Vile: sus archivos propios ya coincidían con el original. Se adaptaron dash, carga, AI y defensa trasera MK2 en el código compartido.
- Red: índices correctos para vida, vida máxima, monedas, alianza, flags y estados opcionales; duración del ácido consistente al enviar/recibir; tipo de munición de Axl enviado como ID, sin convertirlo a un ángulo.
- Recarga de X en frames y recarga de Hyper Axl en segundos.
- El ácido de Toxic Seahorse usa el ritmo original, mientras Acid Burst de X conserva el ritmo del fork. El ritmo acompaña al estado al transformar al personaje.
- Los golpes bloqueados por la defensa trasera de Vile y los balanceos lentos de Morph Moth Cocoon no provocan flinch con daño cero. Lightning Web conserva su efecto deliberado de flinch sin daño.
- AI: el bloque de saltos forzados desactivado por el original también queda desactivado para Vile, Axl y Sigma; X/Zero conservan el del fork.
- Buster Zero negro: corrección original de multiplicación de velocidad. Punchy Zero: unidades correctas en la patada, inmunidad de estado al iniciar Hyper e icono de arma para A-Trans.
- HUD: vida gris adaptada al sprite horizontal; se mantienen las barras y stocks del fork.
- Dibujo de polígonos y mapas espejados: correcciones del original.
- La copia del proyecto original queda excluida de la compilación del fork para evitar definiciones duplicadas.

Se detectó un sonido ausente en los recursos locales: `chargeJumpSMB2`, usado al cargar un salto de Maverick. Se usa `charge_start` como alternativa cuando falta, para evitar la excepción; si el sonido original se agrega después, se utilizará automáticamente.

## Verificación

- Compilación con .NET SDK 10.0.112: **0 errores, 663 advertencias**. La cantidad coincide con la compilación inicial; no se corrigieron advertencias ajenas al merge.
- **16 grupos de comprobaciones aprobados**, invocando métodos de la DLL real: límites de 16 bits, 128 combinaciones de flags y 128 de estados opcionales, vehículos, datos adicionales de Vile/Sigma/X/Zero/Axl, siete municiones de Axl, selección y validación de Tenshouha, recarga, ritmo/inmunidad al ácido y selección de Punchy Zero tras serialización Protobuf.
- `git diff --check`: aprobado.
- Recursos: existen los sprites e iconos utilizados por Tenshouha y por las variantes de munición de Axl. Punchy Zero puede usar las animaciones de Zero mediante su mecanismo existente de fallback.

Comandos desde la raíz:

```powershell
dotnet build MMXOD.csproj --no-restore -v:q -p:OutputPath=bin/merge-check/
dotnet run --project obj/merge-tests/MergeChecks.csproj --no-restore -v:q --no-launch-profile
```

Salida compilada: `bin/merge-check/MMXOD.exe`. Para ejecutar el juego se necesita el directorio de trabajo `bin/runenv`, que contiene los assets. No se cambió el ejecutable habitual de `bin/anycpu`.

Logs: `obj/merge-resumed-build.log` y `obj/merge-tests/after-network-fixes.log`. El código y las instrucciones de las comprobaciones están en `obj/merge-tests`; son archivos locales ignorados por Git.

## Pruebas pendientes en partida

### Corrección posterior: «Failed initial connection» al crear sala

Se reprodujo `SocketException (10057)` en `LANIPHelper.GetLocalIPAddress`, concretamente en `socket.Disconnect(true)`. El servidor llama a ese método durante su arranque P2P; la excepción interrumpe el arranque antes de atender las conexiones. Este código ya estaba tanto en el fork como en el original, sin modificaciones durante el merge.

Se retiraron la desconexión explícita y el `Dispose` redundante: el bloque `using` ya cierra el socket UDP. Después de corregirlo, la detección de IP pasó y una conexión Lidgren aislada por loopback se aprobó en 101 ms. Compilación: 0 errores, 663 advertencias. La prueba está en `obj/p2p-check` y no conecta al servidor público ni a la sala del usuario. La compilación corregida está en `bin/merge-check`; el proceso que estaba abierto en `bin/anycpu` debe cerrarse y recompilarse antes de volver a probar desde el IDE. El ZIP `merge-integrated-20260917.zip` es anterior a esta corrección.

| Área | Casos a revisar |
| --- | --- |
| Vile | Formas MK1/MK2/MK5, dash, Ride Armor, impactos por detrás con MK2. |
| Sigma | Formas normales/Hyper, Wolf/Viral, aparición remota, Mavericks en control/AI, ácido de Toxic Seahorse. |
| Axl | Siete municiones, Copy Shot, Hyper, zoom, empuje de armas, A-Trans y salida mediante arriba + Special2. |
| X/UP | Armas X1–X4, Force Armor, chips, cargas/stock, Hypercharge, transformación UP y tanques. |
| Zero/Punchy | Rakuhouha y Tenshouha seleccionados por separado, fin de Hyper, guardia, patada, daño y movimiento conservados. |
| HUD/red | Barras horizontales, vida gris, vida/monedas mayores de 255 y dos clientes con esta misma compilación. |

Las comprobaciones automáticas no ejercitan una conexión multijugador real, entradas del jugador, renderizado ni colisiones completas. La copia del original no incluye assets, por lo que no se puede certificar que sus animaciones o hitboxes sean idénticos a los recursos locales. No se modificó el checksum de assets del fork.

## Corrección posterior: recurso ausente del HUD horizontal

La excepción por `hud_weapon_base_new` se debía a recursos desactualizados en `bin/anycpu/assets`. Los recursos completos del fork estaban en `bin/runenv/assets`, directorio de trabajo configurado para el proyecto. Se sincronizaron los 14 archivos diferentes o ausentes desde `runenv` hacia `anycpu`, incluidas las definiciones de barras horizontales, `newIcons.png` y el sprite de Force Nova Strike. No se cambiaron las referencias del código ni el diseño del HUD.

Se verificó que los 14 archivos copiados coinciden por hash con el origen y que las hojas de sprites de las definiciones comprobadas existen. Los archivos reemplazados y el manifiesto de cambios están en `obj/hud-assets-backup-20260917-192425`. Es necesario reiniciar el juego para recargar los recursos; este arreglo de assets no requiere recompilar. Falta comprobar el renderizado en partida. Estos recursos locales están bajo `bin` y no forman parte del ZIP de código integrado.

## Respaldos

- Antes de integrar: `obj/merge-backup-20260917-130219.zip`.
- En la pausa: `obj/merge-paused-20260917.zip`.
- Código integrado y documentos: `obj/merge-integrated-20260917.zip`.

Conservar `obj` si se necesitan los respaldos y las comprobaciones. Para continuar, partir de este documento; las decisiones de personalización ya están resueltas.
