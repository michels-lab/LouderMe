# LouderMe Android v0.1.13 — estabilidad del canal Direct

**Distribución:** Michel's Lab Direct / APK sideload, versión pública `0.1.13` (`versionCode 14`), actualización desde `0.1.12` con la **misma identidad de firma estable**. La versión Play sigue siendo una distribución distinta; esta publicación no representa aprobación por Google Play.

## Cambios desde v0.1.12

- Se integraron las correcciones de continuidad del audio y gestión del ciclo de vida de la aplicación, incluida la recuperación optativa al cambiar rutas de salida o al regresar a primer plano. Los casos anteriores de reinicio, arranque y cambio de dispositivo cuentan con contratos automatizados; el comportamiento audible de cada teléfono sigue siendo una validación física independiente.
- Mejoras de estabilidad y comprobaciones de compatibilidad entre Android y el motor DSP; se conservan los controles Boost/EQ, identidad oficial LouderMe y diseño de About.
- Las compilaciones Play, Direct y Developer se prueban por separado. Developer Edition se instala con `com.michelslab.louderme.developer` y mantiene sus datos aislados; **no** se publica como APK pública en este release.
- El pipeline ahora verifica dinámicamente el `versionCode` de Direct al comprobar la versión especial de Developer: corrige el antiguo literal `13`, que bloqueaba nuevas actualizaciones aunque Gradle estuviera bien versionado.

## Integridad y distribución

El release estable únicamente puede publicarse después de pasar tests de Gradle, permisos por flavor, instalación Home/About en emulador, persistencia del Developer instalado, **firma original del APK Direct**, checksum SHA-256 e instalación/captura visual del **APK final firmado**. Los binarios existentes nunca se sobrescriben. El publicador unificado solo actualiza `louderme/latest.json` tras verificar el SHA de la versión fuente, los checksums y la coincidencia con los assets públicos.

## Límites declarados

No se certifica mejora de volumen acústico o audio de llamadas en Galaxy S26 Ultra ni en otros modelos mediante CI/emulador. No se considera completado el canal de Google Play ni se afirma soporte universal de audio de terceros. No hay servicio en la nube, grabación de audio, nueva cuenta ni acceso privilegiado añadido. **No se publica RC ni beta.**
