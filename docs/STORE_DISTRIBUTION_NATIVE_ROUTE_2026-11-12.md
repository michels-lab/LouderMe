## 2026-11-12 — Granada #48: VM-local test catalog signing with original WDK provenance

A real WDK-validated **unsigned** driver ZIP is available from [Actions #43342673254](https://github.com/michels-lab/LouderMe/actions/runs/43342673254). The driver will **never** install on an ordinary PC or be offered as a Store binary directly.

**New functional capability:** `prepare_signed_vm_lab.ps1` takes the original WDK Actions ZIP **and its separate SHA256 sidecar**, a pre-existing **CurrentUser\\My Code Signing certificate** whose subject is explicitly marked `LouderMe LAB`, a real Windows SDK `SignTool.exe`, and an entirely new empty output location **inside a disposable VM**. It verifies original provenance and all per-file digests, extracts precisely three WDK files, signs only the copy of the original catalog with the **already-authorized** local LAB key and verifies trust and catalog membership before returning success. No key generation/import, automatic trust-root installation, Windows boot-signing configuration, endpoint selection, driver installation or physical sound playback occurs. If any step fails, staged output is discarded. **A snapshot and VM certificate provisioning must already exist; these cannot be proved or created by GitHub.**

The signed driver installation test (`run_disposable_lab.ps1`) and its manual, self-hosted **isolated VM only** workflow now require the original unsigned ZIP, matching `.zip.sha256`, expected signer certificate thumbprint and SDK SignTool. Before **any** Devcon install, and again **immediately before** the privileged operation, the runner verifies the exact GitHub WDK source manifest, unchanged `INF` and `SYS` digests, locally trusted signature of the **catalog** and Microsoft's documented `SignTool verify /pa`, `/pa /c CAT INF` and `/pa /c CAT SYS` checks. This closes the gap where `Get-AuthenticodeSignature CAT = Valid` alone did not prove that the approved WDK kernel files were actually covered by the catalog. Microsoft reference: https://learn.microsoft.com/en-us/windows-hardware/drivers/install/verifying-the-signature-of-a-test-signed-catalog-file .

**Example use only after provisioning a trusted test certificate and safe VM snapshot** (no personal laptop, no private keys in GitHub Actions):

```powershell
pwsh -NoProfile -File native/virtual-driver/prepare_signed_vm_lab.ps1 -UnsignedLabZip 'C:\VM-LAB\LouderMe-VirtualRender-LAB-UNSIGNED.zip' -UnsignedLabSha256 'C:\VM-LAB\LouderMe-VirtualRender-LAB-UNSIGNED.zip.sha256' -SignToolExe 'C:\WindowsSDK\bin\x64\signtool.exe' -SigningThumbprint '40_HEX_CHARS_OF_VM_LAB_TEST_CERT' -OutputDir 'C:\VM-LAB\signed-catalog-driver' -LabConsent I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST
```

After successful local catalog test signing, only the already protected, consented manual workflow may install and remove the lab driver in the VM, using the ZIP, SHA sidecar and same thumbprint. `native-vm-catalog-trust-ci.yml` executes syntax + adversarial verification without driver installation or code signing.

**Limits:** this is **test-catalog signing only**, not a Microsoft-signed production kernel driver. A valid VM catalog does **not** establish audio from a separate app or the physical output. The kernel audio route #48 remains open until separately signed, installed and exercised on actual Windows VM/hardware. The working Windows EXE 0.1.19 retains its APO backend as legacy; future Windows Store will use the independently proved engine. No RC, no release.

---

## 2026-11-12 — Granada: sesión funcional de laboratorio con DSP propio

- **Puente Windows real:** el bridge `native/windows-bridge/Program.cs` ahora emite `NATIVE_PCM_ROUTE_STARTED` únicamente **después de iniciar los streams WASAPI**. Para sesiones `--interactive-controls`, recibe `stop` por stdin y cierra limpiamente las capturas y el render; un cierre forzoso es solo una última protección por tiempo de espera. Su parser de comandos distingue `stop` exacto de otras acciones, y la señal de inicio **no demuestra audio de aplicaciones externas ni sonoridad**.
- **GUI Microsoft Store:** nueva ventana `NativeRouteLabForm` detrás de `Native audio route · controlled test`. Enumera solo dispositivos de reproducción activos, exige una **fuente cuyo nombre sea LouderMe Virtual** y una **salida física distinta**, seleccionadas explícitamente. Abre una prevalidación `--diagnose-route --source-id ... --output-id ...` y rechaza metadatos inconsistentes o declaraciones falsas de audio/driver firmado.
- Si la prevalidación de metadatos resulta compatible, la sesión lanza el bridge real `LouderMeAudioBridge.exe` **a 100%**, sin autoreconnect, sin asignar dispositivo predeterminado y con confirmación de inicio visible. Controla el DSP mediante `boost 100..300`, `eq off` y siete bandas EQ; por encima de 150% requiere confirmación adicional. En caso de desconexión, el bridge sigue usando sus guardas de identidad de dispositivo, formato de PCM y roles predeterminados.
- El botón **Stop and release audio**, el cierre del panel y un timeout controlado intentan primero `stop` limpio y después terminan únicamente el proceso hijo propiedad del panel si el puente no responde; no usan comandos de kernel/driver ni modificaciones de registro/APO.
- Las barras Boost/EQ de la pantalla **principal** del MSIX siguen bloqueadas hasta validar de verdad un dispositivo instalado, el motor completo y salida a equipo real; **el panel independiente de laboratorio NO es prueba para publicar en Microsoft Store**. La versión de Windows GitHub sin empaquetar conserva compatibilidad APO y no se modifica.
- CI de Windows/MSIX prueba serialización de controles, selección fraudulenta/duplicada de endpoints, lectura JSON fail-closed y la orden `stop` del bridge. Se sigue requiriendo driver virtual Microsoft firmado, instalación/rollback y prueba completa con otra aplicación Windows antes de promover la ruta nativa a producto.

---

# LouderMe — preparación de tiendas sin dependencia futura de Equalizer APO

**Fecha:** 2026-11-12  
**Dirección técnica confirmada por el propietario:** no seguir desarrollando LouderMe alrededor de Equalizer APO. Construir integración de audio Windows nativa con DSP propio. Conservar el **EXE estable v0.1.19** basado en APO solo como compatibilidad histórica mientras el nuevo motor independiente se vuelve realmente utilizable.

## 1. Estado confirmado y rutas separadas

- **Android Direct v0.1.14 + Desktop v0.1.19:** versión pública ya publicada y aceptada funcionalmente por el propietario. No se reemplaza ni se modifica el updater de esa versión.
- **Google Play:** existen `play`, `sideload` y `developer` flavors. El nuevo workflow `play-store-aab-preflight.yml` compila un **AAB real** de la variante Play, valida package/version/target SDK/segregación de permisos y ofrece firma voluntaria mediante secretos dedicados a la *clave de carga Play*. Nunca publica ni envía un bundle a Play Console automáticamente. **Play Console aún no está creada** (confirmado por el propietario); alta, Play App Signing, ficha, privacidad/Data Safety, revisión FGS `specialUse`, pruebas y lanzamiento continúan por realizar. No meter en el bundle final la firma Direct por accidente.
- **Microsoft Store:** el propietario **sí tiene Partner Center**. `ms-store-msix-preflight.yml` puede crear un **MSIX completo de GUI WinForms** con versión 4-partes, imágenes oficiales, `runFullTrust`, integridad del payload y separación de actualizaciones entre EXE y MSIX. Utiliza un nombre deliberadamente marcado `Preflight` mientras no se faciliten los valores exactos **Identity/Name** y **Identity/Publisher** obtenidos de la ficha propia de Partner Center. El MSIX resultante **NO está certificado ni autorizado para publicarse**.
- **Bloqueo del producto Microsoft Store sin APO:** el Boost/EQ global en la rama pública Windows aún depende de Equalizer APO. El motor C++ propio y los controles hasta 300% existen, pero el dispositivo de audio virtual de Windows y la ruta general de otras aplicaciones hacia PCM procesado no están integrados/firmados. [Issue #48](https://github.com/michels-lab/LouderMe/issues/48); [issue #46](https://github.com/michels-lab/LouderMe/issues/46).

## 2026-11-12 — Avance tangible de ruta nativa en MSIX (sin APO)

- `desktop/src/LouderMeDesktop/NativeRouteReadiness.cs` invoca el puente **propio** empaquetado en `native/LouderMeAudioBridge.exe` con `--diagnose-route` (sin IDs, reproducción, captura ni elevación). Tiene límite de 8 segundos, cancela/termina el proceso si se bloquea, no presenta IDs de dispositivo ni convierte una inspección de metadatos en evidencia de sonido real.
- La GUI Microsoft Store ofrece **Check native engine (read-only)**. Oculta todos los botones de instalación/Configurador/Connect de Equalizer APO y deja **Boost/EQ deshabilitados** mientras el motor independiente no esté integrado. `ApplyProcessing` de MSIX **no escribe configuración APO** ni presenta valores desactivados como amplificación aplicada.
- El build de preflight MSIX compila el **puente NAudio de Windows existente** y el **motor C++ propio** (`louderme_audio_c.dll`) y los incluye en `native/`, ejecuta contratos read-only, **pruebas C++ de PCM/ABI e interop real C++ ↔ .NET** y valida ambos archivos dentro del paquete antes de archivarlo. Este proceso todavía **NO incluye driver firmado, instalación de virtual render, audio PCM físico ni DSP activo bajo la GUI**.
- Regresiones de `--check-store-contract` rechazan respuestas JSON falsas con banderas de `driver_signing_verified`/`real_audio_verified` y cualquier ruta `STATIC_PREFLIGHT_READY` sin selección explícita. El GUI siempre mantiene Boost/EQ deshabilitados hasta una integración posterior comprobada.
- No se modifica el backend APO del **EXE tradicional**; la migración se aplica únicamente al canal MSIX. **No se publicará en Microsoft Store** esta GUI hasta resolver #48/#46 y pruebas reales sin APO.

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
