# Registro de la integración

**Actualización tras reanudar:** el código fue integrado y compila con 0 errores. Pasaron 16 grupos de comprobaciones automáticas y `git diff --check`. La guardia del fork se conserva para Zero y Tenshouha se completó también en Punchy Zero, según las últimas respuestas del usuario. No quedan preguntas de personalización pendientes.

El estado actual, los cambios, los respaldos y las pruebas en partida pendientes están en [MERGE-RESULTADO.md](MERGE-RESULTADO.md). **El resto de este archivo es el registro histórico de la pausa, anterior a esos cambios; no debe usarse como lista de tareas vigente.**

## Registro histórico de la pausa

Fecha: 2026-09-17. **El merge está incompleto.** No se ha hecho commit ni se ha probado en partida.

## Objetivo y fuentes

- Fork: `src/` y `srcnew/` de este repositorio.
- Original facilitado por el usuario: `MegamanX-Online-CE-main/src/` y su `srcnew/`.
- Prioridad: Vile, Sigma y Axl deben funcionar como en esa copia del original, conservando las personalizaciones acordadas del fork.
- Comparación inicial: 92 archivos distintos en `src`, 12 en `srcnew` (incluye archivos exclusivos del fork). Los archivos propios de Vile ya coinciden; hay diferencias en código compartido.
- No se descargó otra versión. Hay referencias Git locales `upstream/main` y `upstream/temp-merge`, pero no se deben confundir con la copia suministrada.

## Decisiones del usuario

Conservar:

- UP / Unlimited Potential, armas de X (incluidas las de `srcnew`), cambios de chips y movimiento/altura de Zero.
- Tenshouha, **corrigiendo su selección invertida con Rakuhouha** (corrección todavía pendiente).
- Daño de Zero del fork: Rising Fang 1, Hyouretsuzan 3, disparos cargados 3/4, Rekkoha 4, y los demás ajustes del fork.
- Tiempos del fork: espera de 18 frames del corte aéreo, 45 del ataque de dash, frame 5 para enlazar el segundo sablazo, y los demás tiempos propios.
- Barras de vida/energía horizontales.
- Soporte de red para vida/monedas mayores de 255, corrigiendo la sincronización.

El usuario pidió revisar por separado otros cambios encontrados antes de decidir. **Todavía falta consultar** los cambios restantes de Punchy Zero y de controles de guardia de Zero. No interpretar el consentimiento sobre daño/tiempos como autorización para reemplazarlos íntegramente.

## Respaldo y cambios previos

Antes de editar se creó `obj/merge-backup-20260917-130219.zip`, con `src`, `srcnew` y `MMXOD.csproj` completos. Conservar este respaldo; contiene las ediciones locales anteriores al merge.

El estado parcial al pausar también se respalda en `obj/merge-paused-20260917.zip`, con `src`, `srcnew`, `MMXOD.csproj` y este documento. Los ZIP están en una carpeta ignorada por Git: conservarlos localmente.

Ya había cambios sin commit en:

- `src/GameMode/GameMode.cs`
- `src/Player.cs`
- `src/X/Weapons/XBuster.cs`
- `src/X/X.cs`

No descartarlos ni restaurar esos archivos desde HEAD. El proyecto original y su ZIP ya estaban sin seguimiento en Git.

## Integrado hasta la pausa

- `MMXOD.csproj`: excluye la carpeta del original de compilación/recursos para evitar clases duplicadas por la inclusión recursiva del SDK.
- Copiados del original: `Characters/Axl.cs`, `CharStates/AxlStates.cs`; armas `AxlBullet`, `AxlWeapon`, `BlastLauncher`, `DNACore`, `FlameBurner`, `IceGattling`, `Raygun`, `SpiralMagnum`; `Sigma/ViralSigma.cs`, `WolfSigma.cs`, `WolfSigmaStates.cs`; Mavericks `FlameMammoth`, `LaunchOctopus`, `MaverickState`, `WireSponge`, `FakeZero/FakeZeroProjs`; y `DrawWrappers.cs`, `LevelData.cs`, `Control.cs`.
- `Player.cs`: soporte de Wolf Sigma en aparición, A-Trans local/remoto y armas de transformación; A-Trans de Punchy Zero usa `PunchyZeroMeleeWeapon` como el original. Se mantienen las personalizaciones anteriores de X/UP/tanques.
- `Character.cs`: lectura de moneda en bytes 5–6, alianza 7, flags 8, máscara 9, ID 10, estados opcionales desde 11, en concordancia con el emisor de 16 bits. Carga inicial original de 30 frames para personajes distintos de X/Zero y sus variantes; 45 para estos últimos. Se integró la salida de A-Trans mediante arriba + Special2 y se retiraron los cambios de arma de ese bloque siguiendo el original.
- `CharState.cs`: inicio de dash original para los demás personajes; se conserva dash inmediato para X/Zero y variantes o cuando instantDash está configurado.
- `Weapons/Weapon.cs`: recarga pasiva del fork en frames cuando `canRechargeAmmo`; recarga en segundos para el relleno de Hyper Axl, como el original.
- `GameMode/GameMode.cs`: indicador de vida gris del original manteniendo barras horizontales y ediciones anteriores.

## Pendiente al retomar

1. Revisar el diff actual y validar las adaptaciones compartidas; aún no están verificadas en juego.
2. Corregir la selección Rakuhouha/Tenshouha en Zero y revisar Punchy Zero. Los switches actuales usan 3 para Rakuhouha y el valor por defecto para Tenshouha, mientras el menú de Zero usa 0 para Rakuhouha y 3 para Tenshouha. `ZeroGigaType.Tenshouha` vale 5: distinguir índices del menú de IDs internos. Revisar también límites/íconos del menú y loadout de Punchy Zero antes de editarlo.
3. Consultar cambios restantes: clases adicionales de Giga Attacks de Punchy Zero (aparentemente sin referencias fuera de su definición), guardia de Zero por pulsación frente a mantener tecla, e interacción de guardia con A-Trans. Hay un arreglo original de velocidad de Buster Zero negro (`Physics.WalkSpeed * 1.15`) pendiente de revisar.
4. Revisar diferencias restantes del original, especialmente AI (original desactiva un bloque de jump zones) y código compartido. No copiar archivos enteros que borrarían UP/X/Zero, armas X4, Force Armor, Hypercharge, RPC o IDs propios.
5. Verificar sincronización real de los datos ampliados, estados opcionales y datos añadidos por subclases. Hay además una discrepancia preexistente de acidTime (emisor multiplica por 20, receptor divide por 30) tanto en el fork como en el original; no se cambió en esta pausa.
6. Probar selección de armas de Axl, Copy Shot/munición de tienda, transformación Hyper Axl, Wolf/Viral Sigma y Mavericks; movimiento de Vile; regresiones de X/UP/chips/Zero/tanques.
7. Revisar recursos de `bin/runenv/assets` y probar partidas locales y multijugador. La compilación sola no demuestra equivalencia de comportamiento. Mantener el checksum del fork mientras no se haya validado otro conjunto de assets.
8. Documentar resultado final y limitaciones. No presentar el merge parcial como terminado.

## Comprobaciones

- .NET SDK disponible: 10.0.112.
- Compilación inicial (tras excluir la copia original): 0 errores, 663 advertencias preexistentes; log `obj/merge-baseline-build.log`.
- Compilación al pausar: **0 errores y 663 advertencias**, 11.80 segundos; log `obj/merge-paused-build.log`. La cantidad de advertencias coincide con la inicial, pero no se compararon individualmente.
- Comando: `dotnet build MMXOD.csproj --no-restore -v:q -p:OutputPath=bin/merge-check/`.
- Salida separada en `bin/merge-check/`; no se ejecutó el juego ni se reemplazó su ejecutable habitual.

Para retomar: leer este documento, consultar el estado Git y continuar desde los pendientes; no reiniciar la integración ni repetir preguntas ya resueltas.
