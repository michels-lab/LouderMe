# Operación Granada — 2026-11-03: loopback float32 sin depender del generador PCM de SysVAD

## Causa funcional confirmada en el código fuente fijado

En `microsoft/Windows-driver-samples` commit `2dc3fd3a0cc84a2933f2194e7ec0871584979071`, `EndpointsCommon/minwavertstream.cpp` inicializa `ToneGenerator::Init(..., m_pWfExt)` en toda captura, incluso en el pin de **loopback de un dispositivo de reproducción**. La implementación original de `ToneGenerator.cpp` devuelve `STATUS_NOT_SUPPORTED` cuando el `WAVEFORMATEX` no es PCM/PCM extensible; en particular, rechaza los formatos `WAVE_FORMAT_IEEE_FLOAT` / `KSDATAFORMAT_SUBTYPE_IEEE_FLOAT` que son habituales en mezclas WASAPI compartidas.

La derivación LouderMe de los PR #136–#138 había reemplazado `WriteBytes` para consumir el PCM real render→loopback, pero **todavía ejecutaba la inicialización del generador antes de registrar el stream**. Desactivar `g_DisableToneGenerator` no corregía este problema: esa bandera afecta a la generación, no a la validación de formato de `ToneGenerator::Init`. Así, un endpoint virtual podía compilar pero rechazar la apertura de su loopback float32 antes de leer un solo frame del relé real.

## Corrección

- Parche exacto de `prepare_sysvad.py`: la inicialización del generador sintético solo se permite para pines de captura **que no son loopback**. En LouderMe, los endpoints host de captura ya están ocultos y el único receptor habilitado es el loopback de reproducción.
- Para el loopback, `ntStatus` conserva su resultado previo; el driver registra el stream sin pedir al generador de demostración que soporte el tipo de muestra. La captura sigue ejecutando `FillLouderMeLoopback` sobre el buffer WaveRT del dispositivo virtual, con las protecciones de formato, DRM y frames de los PR previos.
- Tests `test_prepare_sysvad.py` fuerzan el guard `!IsLoopbackPin(Pin_)` *antes* de la única llamada a `ToneGenerator::Init`, comprueban la ruta real de `FillLouderMeLoopback` y exigen que una revisión del upstream no elimine silenciosamente el punto de parche.
- CI debe derivar el código completo desde el commit Microsoft fijado y compilar el miniport WDK x64, además de ejecutar el test host del buffer PCM y las regresiones Android instaladas.

## Alcance

La corrección elimina una causa de `STATUS_NOT_SUPPORTED` en el arranque del loopback float32. **No** demuestra por sí sola que el driver instalado entregue frames PCM de una aplicación separada, ni el DSP global, ni audio acústico. La prueba real aislada con driver de laboratorio autorizado y controles antes/después sigue siendo la barrera para #46/#48 y cualquier release.
