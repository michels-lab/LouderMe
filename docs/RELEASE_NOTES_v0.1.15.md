# LouderMe Android Direct v0.1.15 — Boost 300% conservado al reiniciar

**Stable Direct APK for in-person acceptance.** Public package `com.michelslab.louderme`; versionName `0.1.15` and **versionCode `16`**. Replaces Direct v0.1.14 (versionCode 15). No Developer Edition or RC.

## Correcciones integradas

- **300% persistente:** antes, dos caminos distintos leían 275% o 300% como **250%** después de reconstruir el servicio o reiniciar el teléfono. Ahora `BoostLevelPolicy` conserva el nivel elegido en `AudioBoostStateStore`, el arranque opcional, la Activity, el servicio y el motor session-0; los decibelios mostrados se calculan desde el porcentaje restaurado.
- **Inicio al encender:** restablece hasta 300% **solo cuando se activó previamente** la opción de inicio y existía un servicio de Boost activo; no enciende el amplificador automáticamente sin opt-in.
- Mantiene volumen multimedia 0–100% independiente, Boost 100–300%, botones rápidos 275/300, ecualizador de siete bandas y notificación.
- Regresiones JVM para 100–300%, niveles anómalos, ganancia digital, 275/300 tras restauración y estados OFF/ERROR que no deben reanudar el amplificador.

## Publicación segura y pruebas con el usuario

Se debe compilar, probar y firmar **la APK final de lanzamiento** con el mismo certificado permanente Direct usado desde v0.1.11; comprobar `versionCode 16`, SHA-256, firma e instalación de esa APK en emulador, y documentar las capturas de Home/About antes de declarar publicado. La publicación unificada con Windows solo puede ocurrir después de verificar los seis archivos y ambas etiquetas de origen.

Después de instalarla sobre 0.1.14 en el Galaxy, probar 275 y 300% con música, volver a abrir la app, reactivar el servicio, reiniciar con la opción de autoarranque activada y confirmar que el nivel persistió. **Anotar si se oye realmente la diferencia en una app de audio externa**; Android `session-0` es una ruta de compatibilidad dependiente del fabricante y no garantiza aumento físico de volumen en todos los teléfonos ni en todas las apps. El objetivo 300% representa aproximadamente +9.54 dB digitales, no tres veces el volumen percibido ni audio libre de distorsión. Subir el volumen gradualmente.

Google Play Console/producción no se publica en este lanzamiento. El canal Android Direct mantiene su actualización automática desde el manifiesto verificado de LouderME-Releases.
