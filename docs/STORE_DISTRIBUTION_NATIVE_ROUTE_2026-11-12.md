# LouderMe — preparación de tiendas sin dependencia futura de Equalizer APO

**Fecha:** 2026-11-12  
**Dirección técnica confirmada por el propietario:** no seguir desarrollando LouderMe alrededor de Equalizer APO. Construir integración de audio Windows nativa con DSP propio. Conservar el **EXE estable v0.1.19** basado en APO solo como compatibilidad histórica mientras el nuevo motor independiente se vuelve realmente utilizable.

## 1. Estado confirmado y rutas separadas

- **Android Direct v0.1.14 + Desktop v0.1.19:** versión pública ya publicada y aceptada funcionalmente por el propietario. No se reemplaza ni se modifica el updater de esa versión.
- **Google Play:** existen `play`, `sideload` y `developer` flavors. El nuevo workflow `play-store-aab-preflight.yml` compila un **AAB real** de la variante Play, valida package/version/target SDK/segregación de permisos y ofrece firma voluntaria mediante secretos dedicados a la *clave de carga Play*. Nunca publica ni envía un bundle a Play Console automáticamente. **Play Console aún no está creada** (confirmado por el propietario); alta, Play App Signing, ficha, privacidad/Data Safety, revisión FGS `specialUse`, pruebas y lanzamiento continúan por realizar. No meter en el bundle final la firma Direct por accidente.
- **Microsoft Store:** el propietario **sí tiene Partner Center**. `ms-store-msix-preflight.yml` puede crear un **MSIX completo de GUI WinForms** con versión 4-partes, imágenes oficiales, `runFullTrust`, integridad del payload y separación de actualizaciones entre EXE y MSIX. Utiliza un nombre deliberadamente marcado `Preflight` mientras no se faciliten los valores exactos **Identity/Name** y **Identity/Publisher** obtenidos de la ficha propia de Partner Center. El MSIX resultante **NO está certificado ni autorizado para publicarse**.
- **Bloqueo del producto Microsoft Store sin APO:** el Boost/EQ global en la rama pública Windows aún depende de Equalizer APO. El motor C++ propio y los controles hasta 300% existen, pero el dispositivo de audio virtual de Windows y la ruta general de otras aplicaciones hacia PCM procesado no están integrados/firmados. [Issue #48](https://github.com/michels-lab/LouderMe/issues/48); [issue #46](https://github.com/michels-lab/LouderMe/issues/46).

## 2. Condición de publicación Windows (requisito de producto)

**No presentar a Microsoft Store como «LouderMe amplificador global» el MSIX de preflight si solo funciona el shell gráfico y la dependencia APO.** Antes de solicitar la certificación:

1. Integrar una ruta de audio de Windows **independiente de Equalizer APO**; preferiblemente controlador de render virtual + WASAPI bridge + DSP propio, o arquitectura independiente que cumpla honestamente las funciones anunciadas.
2. Validar con un equipo real que audio de una **segunda aplicación** llega a la ruta, el Boost/EQ cambia la salida audible, y volumen base/restore/errores/cambio de dispositivo no generan fallos. Las pruebas sintéticas de CI y una ejecución de `--check-apo-contract` no bastan.
3. Verificar firma/instalación/desinstalación del componente de audio y su ciclo de actualización. **La firma gratuita del MSIX de Microsoft Store no firma un driver kernel.** Para distribución real de drivers kernel modernos, Microsoft Hardware Developer Program requiere un certificado **EV**, registro en el programa y aprobación/firma del driver por Microsoft. La ruta exacta de certificación y su costo son un asunto separado del registro gratuito de la tienda.
4. Solo entonces vincular la identidad de Partner Center, generar paquete candidato, certificar instalador/driver conforme a las políticas aplicables, probar MSIX en hardware y enviar la descripción/capturas comerciales correspondientes.

**La compilación MSIX es trabajo de empaquetado anticipado, no una orden para seguir implementando APO**. No crear un «fallback APO Store» de forma permanente sin autorización del propietario.

## 3. Google Play: bloques controlables y externos

**Automatizado sin cuenta:** build release AAB `bundlePlayRelease`, Gradle unit tests, validación de versión/flavor, inexistencia de `REQUEST_INSTALL_PACKAGES` en common manifest, firma de carga solo si se configuraron secretos `LOUDERME_PLAY_UPLOAD_*`, checksum y artifact privado a GitHub Actions con retención corta. No autogenerar keystore silenciosamente, no incluir credenciales en git, no registrar un packageId alternativo.

**Necesita acciones del propietario/plataforma:** registrar Google Play Console; confirmar ficha del paquete `com.michelslab.louderme`, enrolamiento de Play App Signing y clave de carga correspondiente; completar página pública de política de privacidad, Data Safety, ficha/íconos/capturas, declaraciones de foreground service y revisión por Google, testers/internal track y producción. Los requisitos de prueba para cuentas personales nuevas pueden depender del tipo de cuenta y las políticas vigentes. No afirmar que la versión Direct disponible en GitHub es la AAB publicada por Google.

## 4. Pruebas y límites de lo automatizado

- Ambos workflows deben aprobar compilación, validación de archivos reales y pruebas unitarias antes de considerarse completos.
- Un MSIX **sin firma** y con identidad `Preflight` se puede construir/verificar, pero no instalar o distribuir como paquete Microsoft Store final. La instalación real de MSIX/efectos de audio en el hardware del propietario todavía es indispensable.
- Un AAB **sin firma** sirve para detectar errores de compilación y metadatos; no es subible a producción. Un AAB firmado solo es candidato de upload; no demuestra aprobación de Google.
- Estas rutas **no crean releases** ni sobrescriben los instaladores estables v0.1.14/v0.1.19. Sin RC.

Referencias oficiales: [Microsoft Windows packaging](https://learn.microsoft.com/windows/apps/package-and-deploy/packaging/), [Hardware Developer registration](https://learn.microsoft.com/windows-hardware/drivers/dashboard/register-for-the-hardware-program), [Driver code-signing requirements](https://learn.microsoft.com/windows-hardware/drivers/dashboard/code-signing-reqs).
