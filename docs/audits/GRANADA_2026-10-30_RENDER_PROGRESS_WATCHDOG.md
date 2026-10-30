# Operación Granada — Windows render-progress watchdog (2026-10-30)

## Problema real y alcance
Issues [#46](https://github.com/michels-lab/LouderMe/issues/46), [#48](https://github.com/michels-lab/LouderMe/issues/48) y [#74](https://github.com/michels-lab/LouderMe/issues/74) continúan abiertos. En el puente experimental, `PlaybackState.Playing` comprueba el estado lógico de reproducción, pero no garantiza que los callbacks de `WasapiOut` sigan consumiendo PCM. La cola puede crecer mientras el render está estancado. El límite de desbordamientos existente protege otra fase, pero no observa directamente la ausencia sostenida de consumo.

## Implementación propuesta
- `native/windows-bridge/RenderProgressWatchdog.cs`: guarda de hilo de control; no bloquea ni asigna buffers desde el callback WASAPI. Combina PCM realmente almacenado en la cola (umbral mínimo 50 ms), las cuentas monótonas de frames encolados y la cuenta de frames de carga útil leídos por el renderer.
- `native/windows-bridge/Program.cs`: observa el progreso cada ~250 ms mientras la ruta experimental está activa. Tras 3 segundos de PCM disponible sin progreso de lectura, lanza `AudioRouteRecoverableException`, liberando los endpoints mediante el `finally` existente. Reintentos solo en las condiciones explícitas y acotadas de `--auto-reconnect --confirm-experimental`.
- Los tramos sin PCM en cola se consideran inactividad legítima; no provocan reconexión automática. Los contadores/tiempos inválidos fallan de forma cerrada.
- `--check-recovery` ahora ejecuta además los tests deterministas de estancamiento, lectura reanudada, cola drenada, inactividad prolongada, colas pequeñas, consumo continuo y reloj/contadores corruptos.

## Limitaciones y criterios
- El contador del renderer solo acredita PCM entregado a NAudio/WASAPI, **no** sonido oído, presión sonora ni firma/instalación de un driver.
- Este watchdog no repara la falta de endpoint virtual instalado ni da por probada la ruta de otras aplicaciones.
- Antes de integrar, CI del **SHA exacto** debe compilar el puente Windows y ejecutar `--check-recovery`; el PR debe registrar el resultado.
- Pruebas adicionales necesarias en una VM aislada con endpoints reales: reproducir una ruta congelada, validar cierre y restauración, cambio de dispositivo, reproducción normal, suspender/reanudar y atenuación sin bucles. No se cambia por defecto el dispositivo de Windows.
- Sin release, versión nueva, cambios Android, modificación del Equalizer APO de usuarios ni autorización para certificar sonido físico.

## Registro de responsabilidad
Código: `native/windows-bridge/RenderProgressWatchdog.cs` y `Program.cs`. Auditoría: este documento y `PROJECT_LOG.md`. Producto aún bloqueado por #46/#48/#74 y los gates de distribución.
