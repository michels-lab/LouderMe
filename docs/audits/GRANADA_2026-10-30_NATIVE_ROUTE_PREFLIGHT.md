# Granada — diagnóstico seguro de ruta nativa Windows (2026-10-30)

**Estado:** código experimental, no release. Issues de referencia: #46, #48 y #74.

## Objetivo

Antes de iniciar la captura de una salida virtual y reenviarla a un dispositivo físico, detectar situaciones en las que la ruta no puede funcionar o puede producir dobles reproducciones. El estado anterior comprobaba algunos endpoints dentro del flujo de ejecución, pero no disponía de un diagnóstico independiente, read-only ni de un contrato de regresión para formatos incompatibles.

## Nueva interfaz de diagnóstico

Después de compilar `native/windows-bridge/LouderMeAudioBridge.csproj`, ejecutar en Windows:

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --list-devices
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --diagnose-route --source-id "<LOUDEME_VIRTUAL_ID>" --output-id "<PHYSICAL_ID>"
```

`--diagnose-route` no requiere `--confirm-experimental` porque **no reproduce, captura, modifica ajustes ni abre rutas de audio**; solo inspecciona metadatos de dispositivos enumerados. Un comando sin selección (`--diagnose-route`) informa si falta el endpoint virtual o qué datos se requieren. La respuesta JSON omite identificadores y nombres de endpoints, y tiene indicadores explícitos `real_audio_verified=false`, `driver_signing_verified=false`, `windows_system_audio_routing_verified=false` y `acoustic_output_verified=false`. Código de salida 0 = metadatos compatibles únicamente para un laboratorio controlado; 3 = diagnóstico no apto; 1 = excepción controlada de parámetros o sistema. No interpretar 0 como audio funcionando.

## Contrato compartido con la ruta real

`RoutePreflight.Assess` valida que las selecciones sean explícitas, IDs distintos/no ambiguos, source activo y rotulado LouderMe Virtual, sink activo y no virtual, formatos PCM16/float32 1–8 canales 8–192 kHz, mapeo de canales y relación de frecuencia compatibles con `PcmFormatAdapter`. La ruta experimental ahora usa el **mismo** validador antes de abrir las capturas y la reproducción. La validación de la identidad del controlador **no certifica firma del driver** ni el contenido del audio de terceros.

La matriz de pruebas sin dispositivos ejecuta selección nula, driver ausente, ID inexistente, ID duplicado, fuente equivocada, sink virtual, endpoint inactivo, ausencia de formato, PCM no compatible, canales incompatibles y tasas fuera de la razón permitida. `--check-route-preflight` se incorpora al workflow Windows real. No instala drivers, no cambia el endpoint predeterminado ni almacena PCM.

## Limitaciones y criterios pendientes

1. Integrar solo si el workflow de Windows compila el puente y ejecuta `--check-route-preflight` sobre el SHA exacto.
2. Un endpoint etiquetado LouderMe Virtual puede estar presente y aun así no estar firmado, instalado de forma confiable o conectado a un ring buffer con PCM válido. El diagnóstico intencionalmente no afirma ninguna de esas cosas.
3. Para cierre de #46/#48/#74 sigue siendo necesaria una VM descartable y un dispositivo real con driver firmado y autorizado: audio de otra aplicación → virtual → DSP → salida WASAPI física, relación de amplitudes, fuente ausente antes/después, desconexión/reinicio, sin dobles reproducciones, y validación audible del propietario.
4. Ningún instalador público/release ni permiso de dispositivo cambia en este bloque.
