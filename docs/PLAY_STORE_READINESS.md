# LouderMe — Google Play submission readiness

**Revisado:** 2026-11-12. **Situación declarada por el propietario:** todavía **no** tiene Google Play Console. Microsoft Partner Center sí existe, pero pertenece a otro canal y no permite publicar en Google Play.

## Identidad actual comprobada en fuente

- App: **LouderMe**; editor: **Michel's Lab**.
- `applicationId`: `com.michelslab.louderme`.
- Android Direct estable publicada: `0.1.14` / `versionCode 15`, firma Direct ya configurada en los release workflows existentes.
- `compileSdk = 36`, `targetSdk = 36`, `minSdk = 29`.
- `play` flavor: actualización administrada por Google Play y SIN permiso `REQUEST_INSTALL_PACKAGES`.
- `sideload` flavor: actualizador HTTPS + PackageInstaller y `REQUEST_INSTALL_PACKAGES` solo para ese canal. No incluir funciones Direct en el paquete Play.
- `developer` flavor: `com.michelslab.louderme.developer` aislado; no corresponde al upload comercial.

## Avances técnicos de esta Operación Granada

- [x] Código Android vigente implementa Boost de **100 a 300%**, EQ local y Play In-App Updates.
- [x] El proyecto diferencia el paquete Play de Direct/Developer y mantiene las declaraciones Android 36.
- [x] Se creó el workflow `.github/workflows/play-store-aab-preflight.yml` para compilar `bundlePlayRelease` y ejecutar pruebas. **No marcar como validado hasta que su CI pase.**
- [x] Verificador `tools/verify_play_bundle.py`: exige bundle real (manifest/resources/dex), ID estable, versión/SDK de fuente, separación de permisos y firma detectada cuando se solicita.
- [x] Firma opcional con cuatro secretos **específicos de upload Play**, sin reutilizar claves de Direct ni incrustar contraseñas. Si faltan, el paso solicitado falla; no genera claves ficticias.
- [x] Artefactos de preflight conservados en Actions con SHA-256 y SHA del código. **No se crea release ni se envía a Google**.

## Requisitos pendientes fuera de GitHub / distribución

- [ ] Registrar Google Play Console (cuenta personal o de organización) y verificar su identidad.
- [ ] Elegir/cerrar identidad de **Play App Signing** y registrar una **clave de carga Play** de confianza. Si se pretende actualizar instalaciones Direct desde Play sin desinstalación, estudiar primero la continuidad de la **clave de firma de la app**: una upload key distinta por sí sola no asegura compatibilidad entre canales.
- [ ] Crear la ficha de aplicación con `com.michelslab.louderme`, países, categoría, disponibilidad y ficha visual (ícono, screenshots, descripción, feature graphic).
- [ ] Publicar una **política de privacidad** definitiva mediante URL accesible, en consonancia con la implementación y el formulario **Data Safety**; `docs/PRIVACY_POLICY.md` es todavía un borrador.
- [ ] Completar cuestionarios de contenido, publicidad, audiencias y clasificación por edades conforme a lo realmente disponible.
- [ ] Declarar en Play Console el servicio foreground `specialUse` usado para audio iniciado por usuario, con justificación y demostración visual. Este tipo de servicio queda sujeto a revisión de la política correspondiente.
- [ ] Subir un **AAB realmente firmado** con upload key válida; validar Play App Signing y realizar una instalación desde un track interno.
- [ ] Probar EQ/Boost, permisos/notificaciones, audio de otras aplicaciones permitido por Android, inicio/parada de servicio y actualizaciones **desde Play Store** en un equipo compatible.
- [ ] Cumplir cualquier requisito de pruebas cerradas y verificación de dispositivo aplicable al tipo y antigüedad de la cuenta, y solicitar acceso a producción cuando Play Console lo habilite.

## Fronteras de evidencia

La confirmación del propietario de que LouderMe **Direct v0.1.14** funciona acredita aceptación funcional de esa versión. **No** implica que Google haya aprobado la variante `play` ni que audio session-0 afecte universalmente toda aplicación y fabricante. No afirmar un resultado de Store antes de recibirlo. No publicar un AAB sin firma ni usar el keystore público Direct automáticamente.

**Siguiente actividad GitHub:** ejecutar ambos workflows de preflight, corregir fallos de compilación/paquete y conservar logs. El firmado comercial y la publicación oficial se realizarán únicamente cuando existan la cuenta y la configuración correspondiente.

La estrategia Windows nativa sin APO se documenta por separado en [STORE_DISTRIBUTION_NATIVE_ROUTE_2026-11-12.md](STORE_DISTRIBUTION_NATIVE_ROUTE_2026-11-12.md).
