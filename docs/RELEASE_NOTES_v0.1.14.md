# LouderMe Android Direct v0.1.14 — Global Boost hasta 300%

**Published stable Michel's Lab Direct APK** upgrading Android v0.1.13 (public versionCode 14) to **v0.1.14, versionCode 15**. The final APK retained the permanent Direct signing identity introduced in v0.1.11. The [official unified release](https://github.com/michels-lab/LouderME-Releases/releases/tag/louderme-android-v0.1.14-desktop-v0.1.19) and its checksum-verified public update feed are available.

## What's new

- Global Boost quick buttons now reach **275% and 300%**. The fine slider offers 100–300% in 1%-increments; Android UI, main activity and foreground audio service accept the full range.
- The actual session-0 audio engine previously clamped 300% selections down to 250%. Its request handling is now aligned with the UI and reports the requested target rather than a secretly lowered value.
- Notification Boost up/down actions step through the new upper presets and reject values above the valid range. The service preserves the selected percentage through shutdown/recovery when supported.
- Unit tests check 300% conversion to approximately +9.54 dB / 954 millibels, plus 300/301 boundaries and preset traversal.
- Keeps the seven-band EQ, device media volume 0–100%, Direct updater, official visual identity and separate Developer Edition package.

## Required source release gates

Pass independent Play/Direct/Developer Gradle tests and APK builds; verify package IDs and flavor permissions; install and capture real Home/About in an emulator; verify installed Developer isolation; sign the final Direct APK using the **same permanent signer**; verify SHA-256/package ID/versionCode and install/capture the final signed artifact before a source release can be declared published. Do not overwrite v0.1.13 or use a debug signing identity for Direct.

Android's public global session-0 effects are vendor dependent and may not affect other apps on every phone. An attached effect does **not** certify audible 300% gain, universal compatibility, call-audio support, or distortion-free playback on the user's Galaxy or other hardware; a real-device audio A/B is still required. Positive gain can cause loud peaks; increase gradually. Google Play production approval is separate. No RC/beta.

The unified LouderME-Releases publisher may update the public updater feed only after it has verified signed APK, Windows Setup and Portable, both source tags referencing the same source commit, and all six checksum assets.


## Verificación posterior al lanzamiento — 2026-11-12

- **Confirmación del propietario:** «ya comprobé que todo funciona». Se registra su aceptación funcional de LouderMe con Boost hasta **300%** en las versiones estables de esta operación, incluida Android Direct v0.1.14. No se reportaron fallos nuevos en esta revisión.
- **Evidencia de entrega:** [pipeline Android 43315685300](https://github.com/michels-lab/LouderMe/actions/runs/43315685300) correcto; compilación de sabores, firma Direct permanente e instalación del APK final firmado, UI Home/About y Developer. Publicador unificado [43326490451](https://github.com/michels-lab/LouderME-Releases/actions/runs/43326490451) correcto.
- **Límite de la afirmación:** confirmación de uso aportada por el propietario, no sonometría ni mediciones A/B de señal, ni garantía de todas las rutas de audio de terceros. El canal Google Play sigue fuera de esta publicación. [Acta](docs/audits/GRANADA_2026-11-12_OWNER_ACCEPTANCE_LOUDERME_300PCT.md).
