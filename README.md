# LouderMe

## Operación Granada — reparación visual de Windows (2026-10-24)

El [PR #107](https://github.com/michels-lab/LouderMe/pull/107) integró la recuperación del About, logotipo transparente adaptado al tema oscuro y ecualizador de siete bandas más grande e interactivo. [Notas de mantenimiento Windows v0.1.16](desktop/releases/v0.1.16.md). El audio físico sigue pendiente; una gráfica funcional no demuestra audio nativo independiente.

## Granada — restablecer el logotipo oficial de Windows (2026-10-23)

[Auditoría del error visual `auderMe` y de los recursos oficiales](docs/audits/GRANADA_2026-10-23_OFFICIAL_WINDOWS_LOGO.md): el encabezado, Splash, About y la barra de título usarán el branding aprobado, sin ondas ni palabras reconstruidas. **Windows v0.1.15 es una propuesta hasta que se validen el instalador y el release estable**.

## Operación Granada — audio de aplicaciones externas en Windows (2026-10-22)

[Ver investigación y corrección de inicialización nula del puente nativo](docs/audits/GRANADA_2026-10-22_EXTERNAL_AUDIO_NULL_ROUTE.md). Ambos modos de prueba quedan separados en el código. **Aún no hay evidencia de audio físico ni una versión nativa publicada.**

## Operación Granada — recuperación segura del test de audio Windows (2026-10-22)

[Ver auditoría del test de atenuación temporal de −18 dB](docs/audits/GRANADA_2026-10-22_APO_DIAGNOSTIC_RECOVERY.md): el código nuevo recupera al reiniciar solo una prueba identificada con exactitud, preservando ediciones externas. La integración no prueba el audio físico ni actualiza el instalador público.

## Operación Granada — ecualizador nativo, contrato de siete bandas (2026-10-21)

[Ver auditoría de alineación de frecuencias entre Android/Windows y el DSP experimental](docs/audits/GRANADA_2026-10-21_NATIVE_EQ_PARITY.md). **No modifica la versión pública** ni demuestra audio físico del driver virtual.

## Operación Granada — auditoría de aplicación y plugins (2026-10-21)

[Ver auditoría y bitácora propias de este producto](docs/GRANADA_APP_PLUGIN_AUDIT_2026-10-21.md): evidencia de GitHub Actions, conectores ChatGPT aplicables, servicios existentes, costo cero adicional por defecto, pendientes reales y separación estricta de los demás productos. La documentación no prueba QA físico ni publica una versión.

**Make everything louder.**

LouderMe is an Android audio utility by **Michel's Lab** for increasing quiet media output, shaping sound with a real equalizer, and exposing the state of the Android audio engine instead of hiding it behind fake controls.

## Current public release bundle

**Android v0.1.12 + Windows Desktop v0.1.14** (latest verified public unified bundle; GitHub release and updater metadata checked 2026-10-23). Both Windows Setup/Portable and the unchanged Android APK are available with verified SHA-256 sidecars in [the official unified release](https://github.com/michels-lab/LouderME-Releases/releases/tag/louderme-android-v0.1.12-desktop-v0.1.14). This does not certify real acoustic gain.

Michel's Lab publishes the current stable Android and Windows binaries together on one public GitHub release page. Platform versions remain explicit when their release lines differ.

### Global Boost
- 100%, 125%, 150%, 175%, 200%, 225%, 250%.
- Fine 100–250% slider.
- Target signal-gain readout.
- Output-route and engine diagnostics.

### Equalizer
Seven LouderMe target bands:
60 Hz, 150 Hz, 400 Hz, 1 kHz, 2.5 kHz, 6 kHz, 12 kHz.

Presets:
Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones, plus Custom.

## Updates

LouderMe now has two explicit distribution flavors.

### Google Play
The `play` flavor:
- uses Google Play In-App Updates;
- checks automatically;
- exposes Check/Install actions;
- does **not** request `REQUEST_INSTALL_PACKAGES`.

### Michel's Lab direct / sideload
The `sideload` flavor:
- checks a public Michel's Lab update manifest automatically;
- downloads a newer APK automatically;
- validates SHA-256;
- validates package ID and versionCode;
- validates that the APK is signed by the same LouderMe sideload release key;
- then hands installation to Android.

Android still requires system/user confirmation for APK installation.

Public feed:
`michels-lab/LouderME-Releases/louderme/latest.json` — current LouderMe Direct source accepts **only** the dedicated product feed. Previously installed old versions may still contain earlier fallback logic.

The LouderMe source is maintained separately from the **public binary-release repository** `michels-lab/LouderME-Releases`. Do not confuse the source repo's GitHub releases with the customer-facing unified release.

## One-time signing transition

Versions through v0.1.3 were GitHub CI debug APKs. Hosted CI debug certificates are not a durable update identity.

v0.1.11 established the current permanent Michel's Lab Direct signer (SHA-256 certificate starts `4a5bb9d9`), different from prior v0.1.10 and earlier. Versions v0.1.11 and later must retain this signer for in-place Direct updates. Earlier signers cannot update into this line in place.

For Direct installations at v0.1.10 or earlier, back up settings, uninstall the old package (this may remove app-local data), then install v0.1.11 or later. Users already on v0.1.11 should be able to update to v0.1.12 with the SAME certificate if the signed release passes CI.

## Visual system

LouderMe follows the current IG Cleaner Pro family language:
near-black workspace, layered blue-black surfaces, cyan/blue operational accents, gold highlights, compact status badges and dashboard cards.

## Identity
- Product: `LouderMe`
- Studio: **Michel's Lab**
- Android package: `com.michelslab.louderme`
- Current verified public Android version: `0.1.12` (versionCode `13`)
- Current verified public Windows Desktop version: `0.1.13`
- Developer: Michel Duarte / Michel Armando Duarte Flores
- License: Proprietary — All Rights Reserved


## Direct auto-updates

LouderMe now has two update channels:

- **Google Play** builds use Play In-App Updates.
- **Michel's Lab Direct** builds use a public manifest and stable signed APKs while keeping source private.

Direct updates verify SHA-256, package identity, version and signing identity before Android receives the package.

v0.1.11 is the current permanent-signing baseline for Direct updates. Versions signed with an older key cannot upgrade in place to this certificate. Future releases retain the v0.1.11 signing identity.

Android still shows the required system installation confirmation.


## Unified public release

The customer-facing public binary channel groups the current stable Android and Windows builds in one GitHub release. Android and Desktop keep separate updater manifests and real platform version numbers, but both manifests point to the same release page.

When one platform is updated, the public publisher carries forward the validated current binaries of the other platform so the release remains complete.

## Verified October 12, 2026 release

- [Latest unified public Android v0.1.12 + Windows v0.1.13 download](https://github.com/michels-lab/LouderME-Releases/releases/tag/louderme-android-v0.1.12-desktop-v0.1.13) — six APK/Setup/Portable and checksum assets verified by GitHub release metadata; runtime tests remain separate.
- [Release-publisher CI](https://github.com/michels-lab/LouderME-Releases/actions/runs/38231448218) passed; source commit for both tags: `ed0bd6da54c022ac05eba0b3760361f4bce056ff`.
- Android Direct APK SHA-256: `0947e7f6b21f4c5beb10ebebe914d3420586504aa0bac7de42d1a565625a68a7`.
- Windows Setup SHA-256: `9bc5def136fc35d01317e9892d599f6aefe5701758fafbf20941c0b459707501`.
- Windows Portable SHA-256: `b18953af1fbd5d023a828c37eb8b691b1ee88297e91fa9ab6e684de670dcd324`.
- Real-device audible Android/Windows audio behavior, native system-wide driver and Windows Authenticode signing **remain unverified**; CI passes do not constitute physical audio testing.
