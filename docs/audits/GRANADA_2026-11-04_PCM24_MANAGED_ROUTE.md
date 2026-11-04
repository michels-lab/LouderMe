# Operación Granada — 2026-11-04: packed PCM24 en el laboratorio Windows

## Causa raíz

El relé SysVAD fuente del LAB mueve bytes PCM24 correctamente dentro de frames de tres bytes (6 bytes por frame estéreo), pero el receptor C# `PcmFormatAdapter.IsSupported` solamente permitía PCM16/float32. Como consecuencia, un endpoint Windows con `WAVE_FORMAT_PCM` de 24 bits impedía abrir la captura virtual, pasar la verificación independiente de tono y encaminar el DSP, sin que un fallo real del driver lo explicara. La autoprueba anterior incluso exigía que PCM24 fallara: una regresión de compatibilidad mal codificada.

## Implementación

1. `PcmFormatAdapter` acepta PCM24 empaquetado signed little-endian, sign-extends el bit 23 y normaliza por 2^23 al dominio float32, mientras su salida usa cuantización asimétrica ±(2^23). Conserva 3 bytes por muestra y memoria de scratch en callbacks, sin bit-casting PCM32 a float.
2. `VirtualLoopbackProbePcm`, generador interno de 440 Hz, fuente independiente `LouderMeIndependentTone` y `PhysicalRouteTone` pueden crear/decodificar PCM24 respetando `BlockAlign`. El receptor de aplicación independiente sigue sin producir audio propio y no abre un speaker ni DSP.
3. `RoutePreflight` y monitor permiten PCM24 si la ruta real es mono/estéreo compatible; rechazan formatos no implementados como integer PCM32. El CI Windows invoca las mismas autopruebas reales del binario .NET 10.

## Regresión adversarial

- Prueba PCM24 -8388608, cero, 4194304, +8388607: signo, amplitud y efecto de la cuantización del rail positivo ±1 LSB.
- Matrices de formatos y tasas 44.1/48/96k, mono/estéreo/8 canales, conversiones fragmentadas, buffer temporal reutilizado y desalineación de 5 bytes frente a frames de 6.
- Paquete PCM24 de más de 500ms reservado: debe rechazar, no realocar.
- Silencio debe fallar identidad 440Hz; integer PCM32 no puede confundirse con IEEE float32.

## Límite de evidencia

**Candidato:** los cambios de fuente están en una rama y requieren compilación/pruebas Windows del PR antes de integrarse. Un autodiagnóstico exitoso no instala el driver SysVAD, no demuestra que otra aplicación de Windows entregue PCM al loopback de la VM ni que se emita audio audible. Continúan pendientes #48, firma controlada y análisis de rollback. **No release**.
