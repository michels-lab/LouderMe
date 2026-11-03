# Operación Granada — 2026-11-03: protección de PCM y contrato de formato extensible

## Defectos corregidos

El relé experimental SysVAD derivado de los PR #136/#137 descartaba nuevas muestras cuando la restricción de copia o el control de loopback impedían la transferencia, pero **no invalidaba el PCM que ya estaba en su cola**. Si la restricción cambiaba posteriormente, una captura permitida podía acceder a una época anterior de muestras. Además, el tamaño mínimo de `WAVEFORMATEXTENSIBLE.Format.cbSize` se derivaba de `sizeof(WAVEFORMATEXTENSIBLE) - sizeof(WAVEFORMATEX)`, expresión dependiente del padding del compilador, en lugar de los **22 bytes** exigidos en el formato.

## Cambios

- `prepare_sysvad.py` exige que la publicación proceda del stream de render reconocido. Si el loopback no tiene listener activo, `CopyProtect` está activado o el control de loopback restringe la copia, ejecuta `m_LouderMeRelay.Reset()` antes de retornar.
- La captura virtual sujeta a DRM también vacía la cola antes de entregar silencio. Cuando se restablezca una política permisiva, solo podrá recibir **PCM publicado de nuevo**, no muestras anteriores.
- `LouderMeLabPcmRelay.h` valida `WAVE_FORMAT_EXTENSIBLE` con `cbSize >= 22`, independientemente del relleno ABI de estructuras C++.
- Pruebas de formato: aceptan exactamente 22 bytes y rechazan 20/21. Pruebas de política: verifican que un reset invalida audio previo y que solo vuelve a circular PCM nuevo.
- Prueba concurrente ejecutando el **mismo código C++ del kernel**: 3,000 publicaciones, 3,000 capturas y 600 reseteos en paralelo, comprobando frames de 6 bytes sin roturas, silencio después de la revocación y ninguna asignación dentro de callbacks.
- Se conservan los límites existentes de 65,520 bytes, PCM y float32 reconocido, frame-alignment, cero en underrun, capacidad/bitrate, no grabaciones, sin cambios de dispositivo predeterminado.

## Condición de aceptación

Exigir CI en el SHA exacto: test C++ real, derivación de fuentes SysVAD fijadas, build WDK x64 completo e INF/CAT, junto con CI Android. Una compilación exitosa **no** certifica instalación, firma de producción, salida acústica, audio externo de aplicaciones o Boost global. La ruta completa sigue pendiente de una VM Windows desechable autorizada con un driver de laboratorio confiable. Mantener abiertos #46/#48 y no crear release.
