## 2026-10-30 — Operación Granada: DSP propio → salida física sin driver virtual (candidato)

- #46/#48: modo Windows experimental `--probe-direct-dsp` para reproducir **solo una señal propia y breve** por un endpoint físico explícitamente seleccionado, procesada realmente con `NativeDsp` C++, y verificar identidad 440 Hz mediante el loopback WASAPI del mismo endpoint. Sin cambiar predeterminados, instalar drivers ni usar Equalizer APO.
- `--check-direct-dsp`: validación C++ DSP real en memoria de 100%/150%, PCM16/float32 44.1/48/96 kHz mono/estéreo, EQ y errores invalidantes; integrado en Windows Native DSP CI. La prueba física aún requiere PC Windows con permiso del usuario y altavoces a nivel seguro.
- Un PASS de loopback físico **no** implica altavoces audibles ni sistema global de otras apps; la ruta completa con driver SysVAD permanece pendiente. Auditoría: [DIRECT_NATIVE_DSP_PHYSICAL_PROBE.md](docs/audits/GRANADA_2026-10-30_DIRECT_NATIVE_DSP_PHYSICAL_PROBE.md).
- Candidato en rama, pendiente CI exact-head/merge. Sin release y sin modificación Android.

---

## 2026-10-30 — Operación Granada: preflight Windows #129 integrado con CI verde

- [PR #129](https://github.com/michels-lab/LouderMe/pull/129) fusionado en `main`, squash `84e5c003c86e87f1820ddcaed81463b0d6dd3ba8`. `RoutePreflight.Assess` comparte las validaciones de endpoints/formatos entre `--diagnose-route` read-only y la admisión del streaming WASAPI experimental.
- [CI Windows/Ubuntu de DSP y bridge exact-head](https://github.com/michels-lab/LouderMe/actions/runs/41140816315) **SUCCESS**, incluido `--check-route-preflight`. El workflow corregido valida por separado el tono sintético y el preflight.
- [Android CI independiente](https://github.com/michels-lab/LouderMe/actions/runs/41140816323): **IN PROGRESS al fusionar**. No equivale a Android instalado certificado.
- Sin driver firmado/instalado, sin PCM real de terceras aplicaciones ni audibilidad verificada: #46/#48 permanecen abiertos y #74 es antecedente cerrado. No release ni versión nueva (Android 0.1.12, Windows 0.1.17). Ver [auditoría](docs/audits/GRANADA_2026-10-30_NATIVE_ROUTE_PREFLIGHT.md).

---

## 2026-10-30 — Operación Granada: diagnóstico read-only previo a ruta nativa Windows (candidato)

- Issues abiertos #46/#48 (antecedente cerrado #74): agregado `RoutePreflight` de metadatos seleccionados y formato PCM, compartido entre `--diagnose-route` (JSON redacted) y la ruta experimental real, **antes** de iniciar callbacks WASAPI.
- Errores específicos para ausencia de endpoint virtual, ID ambiguo/no seleccionado, feedback fuente/sink virtual, endpoint inactivo, formato ilegible/incompatible, mapeo de canales o tasa no soportados. No se finge driver firmado, PCM externo, resultado audible ni SPL.
- Contrato de regresión: `--check-route-preflight` en Windows Native DSP CI; se comprueban casos seguros e inseguros sin hardware. [Auditoría detallada](docs/audits/GRANADA_2026-10-30_NATIVE_ROUTE_PREFLIGHT.md).
- **Pendiente** el CI exact-head y, por separado, la prueba de driver/VM/PC real. Sin instalación, cambio de endpoint, nueva versión, release ni cierre de issues.

---

## 2026-10-30 — Operación Granada: watchdog del render integrado, sin audio físico certificado

- [PR #128](https://github.com/michels-lab/LouderMe/pull/128) integrado en `main` mediante squash `cdd769a0c6750f577afc9edd4f38a17415f072e4`: guard de progreso en ruta experimental Windows, limpieza existente de endpoints ante atasco, pruebas unitarias de inactividad/recuperación/estancamiento. Cambios registrados en `native/windows-bridge/`.
- [Native audio DSP CI del SHA exacto](https://github.com/michels-lab/LouderMe/actions/runs/41139171359) **SUCCESS**: Ubuntu/Windows DSP + Windows managed/native interop, incluido `--check-recovery`.
- [Android CI del SHA exacto](https://github.com/michels-lab/LouderMe/actions/runs/41139170889): compilación/variantes **SUCCESS** al integrar; dos jobs Android instalados de Home/About y Developer Edition seguían **EN EJECUCIÓN**. No declarar validación instalada de Android hasta que terminen.
- Evidencia de código **NO** equivale a driver instalado/firmado ni a Boost/EQ audible o captura externa probada. #46/#48/#74 y release gates permanecen abiertos. No release; versiones públicas Android 0.1.12 + Windows 0.1.17.

---

## 2026-10-30 — Operación Granada: watchdog de lectura del render Windows (candidato)

- Tras auditar #46/#48/#74 y confirmar cero PR abiertos al iniciar, se creó una única línea funcional experimental para vigilar la ruta de reproducción. `RenderProgressWatchdog` detecta 3 s de PCM realmente encolado (>=50 ms) sin lecturas útiles por el renderer de NAudio, sin confundir inactividad legítima con fallo. `Program.cs` devuelve interrupción recuperable y ejecuta su limpieza de endpoints; `--auto-reconnect` permanece estrictamente opt-in y acotado.
- Incluye pruebas deterministas de progreso normal, inicio en silencio, cola pequeña, atasco, recuperación, drenaje y contadores/reloj inválidos ejecutadas desde `--check-recovery` en Windows Native CI.
- Auditoría y límites: [GRANADA_2026-10-30_RENDER_PROGRESS_WATCHDOG.md](docs/audits/GRANADA_2026-10-30_RENDER_PROGRESS_WATCHDOG.md). **Pendiente CI de SHA exacto y pruebas reales con hardware**; no hay driver instalado ni prueba de audio externo audible. Sin release ni cambio Android.

---

## 2026-10-21 — Operación Granada: real no-stimulus causality gate for native Windows external-source route (candidate)

- Follow-up after merged #88 and #91: the independent source physical-audio VM procedure now brackets 100%/150% measurements with **no-stimulus WASAPI capture before and after**. It must announce EXTERNAL_SOURCE_READY, complete a real five-second physical-monitor window, show one valid numeric measurement with physical frames and no dropped packets, and fail specifically because the externally generated 440 Hz input is **absent**. A different error, missing listener, unexpected PASS, stale result or matched440 contamination >0.003 aborts the laboratory instead of claiming source causality.
- Added `native/virtual-driver/NoSourceControlCheck.ps1` pure validator and 11 device-free malicious/error fixtures `test_NoSourceControlCheck.ps1`; integrated into Windows native CI and static driver source contract. The actual VM script uses this exact validator and protects before/after evidence from stale files; the cleanup still removes only its own root-lab device.
- **No virtual driver installed or signed**, no actual audio playback performed in GitHub, no proof yet of separate-app sound on a user's speakers. This is a **source + CI tested acceptance gate**, not new physical output results. Native Windows routing issues #46/#48 and owner approval remain open. Stable public Android/Windows versions unchanged, no release.

---

## 2026-10-21 — Operación Granada: native Windows physical gain proof contract and Perspective Council review (source candidate)

- After PR #88 merged `7ea4f26` with exact-head Windows native, SysVAD package WDK and Android installed CI green, candidate branch `feat/granada-independent-gain-validator-20261021` extracts physical WASAPI loopback 100→150% gain acceptance into pure `native/virtual-driver/IndependentGainCheck.ps1`. The actual VM route now uses this same production-of-evidence validator, not separately hand-coded if statements.
- Standalone negative fixtures `native/virtual-driver/test_IndependentGainCheck.ps1` reject missing source JSON, wrong/ambiguous boost percentages, fake separate application provenance, inflated generic-system-audio/SPL scope claims, nonfinite or string measurements, silence, non-coherent source and out-of-range numeric gain ratios; native Windows CI executes them without audio hardware.
- Added `docs/product-reviews/2026-10-21-native-windows-audio-evidence.md`: ONE ChatGPT multi-lens PDR (Contrarian, User Advocate, Intelligence Architect and trust) challenges driver/signed endpoint claims, alternative own APO path, safety, observable latency and unsupported AI features, with concrete W1–W5 priorities. It is not independent agent execution or production acceptance.
- **Source candidate pending CI**, not an installed/physically signed driver or tested system-audio amplification. Native issues #46/#48 remain OPEN; public versions unchanged, no release.

---

## 2026-10-21 — Operación Granada: separate Windows process PCM → virtual render → native DSP → physical monitor (source candidate)

- [PR #88](https://github.com/michels-lab/LouderMe/pull/88): add `native/independent-tone/` as a truly distinct NAudio Windows executable which emits bounded 440 Hz PCM16/float32 to a selected virtual render device; no DSP, recording, microphone, default changes or driver installation.
- Bridge `--probe-app-route` captures and processes input but synthesizes **NO tone**. It emits `EXTERNAL_SOURCE_READY` once both virtual and physical WASAPI listeners have started, monitors five seconds, requires correctly identified 440 Hz output plus actual virtual capture frame counts and no drops/resets, and forbids automatic probe retry.
- Dedicated disposable-VM PowerShell runner coordinates separate bridge/source process startup/readiness/exit and cleanup by owned PIDs, redacts endpoint IDs and stores only metadata. Existing lab VM install→virtual loopback→internal physical loopback→**external application physical route**→remove-own-root-device gate now requires this independent proof before VM success. It repeats the genuine external-source physical monitor at **100% and 150%** Boost; measured physical 440 Hz amplitudes must have a **1.30–1.70 gain ratio** or the VM test fails. That is not hardware SPL calibration.
- Native Windows CI builds independent source and runs synthetic source tone identity tests, validates lab PS syntax and fail-closed static cross-process contract; driver CI source derivation/WDK gate remains separate. **Physical VM driver installation, signed package, actual non-silent virtual-app PCM, external loudness and generic OS audio are NOT proven by CI; native issues #46/#48 remain open.**
- Source PR candidate; no public release, version bump, driver installation or owner physical test.

---

## 2026-10-21 — Correction: Perspective Council is for ChatGPT, not Copilot

- Owner clarified these perspectives must help the assistant doing Michel's Lab development. The four mistakenly Copilot-specific perspective profiles are removed, replaced by four provider-neutral `.michelslab/agents/*.md` role instructions, readable through GitHub and usable with the master offline prompt pack.
- No independent AI execution, parallel workers, background process, product code edit or release is claimed. Existing unrelated GitHub/Copilot infrastructure stays unchanged.

## 2026-10-21 — Product Perspective Council agent profiles (branch candidate)

- Added **read-first review agents**: independent Contrarian, User Advocate, Intelligence Architect and Council with product-specific instructions. The Council synthesizes overlooked opportunities, alternative designs and measurable experiments; shared source-of-truth is `michels-lab/Michel-Software-Standards/standards/PERSPECTIVE_COUNCIL_STANDARD.md`.
- Applies to Android/Windows device setup, EQ/Boost UI, provider installation and app update channels, route bypass, clipping, actual PCM and physical-acoustic evidence; do not overstate dB SPL. Product-specific reviews are proposals, not implemented features or established test evidence. No live agent execution, CI validation, installed QA, production code changes or release claimed in this change.

## 2026-10-21 — Operación Granada: live, nonblocking native DSP control handoff (candidate)

- Candidate `feat/granada-native-live-dsp-controls-20261021`: add `DspControlMailbox` with immutable copied seven-band EQ command snapshots, Interlocked single-slot latest-wins publication (no command backlog) and callback-only C++ DSP setters. `NativeDsp.ConfigureBoost` adds bounded native setter; EQ guard removes LINQ from callback path. The existing capture PCM callback alone applies validated pending controls **before** processing its buffer. Per-second LAB counters distinguish commands published/applied/actually overwritten (not processed audio losses or audibility), without logging user audio. `--interactive-controls` plus `--auto-reconnect` is rejected, preventing a stale console reader from outliving the route it controls.
- New strictly experimental `--interactive-controls` (local stdin text only) accepts `boost 100..250`, `eq off`, `eq seven dB values`; malformed/NaN/Inf/out-of-range input rejected without modifying DSP state. Requires explicit virtual source, separate physical sink and `--confirm-experimental`; cannot run during `--probe-route` physical measurement. No listening device or default output is changed without user selection.
- Native Windows CI extends `--check-controls`: actual DSP DLL numeric Boost/EQ PCM checks, state immutability, single-consumer correctness, storm coalescing, invalid inputs and concurrent producer/callback PCM finite/bounded samples. Prior native DSP/Windows bridge infrastructure and tested synthetic stream conversion kept intact.
- **Not proven**: a signed/installed LouderMe virtual endpoint; third-party Windows system audio routing into this bridge; user-facing desktop UI live controls; external audible sound changes, measured physical latency/CPU, production driver signing/install/rollback, or hardware tests. No release candidate/public release or stable version bump; #46/#48 remain open.

---

## 2026-10-20 — Operación Granada: Developer notes safe migration between Android signing identities (candidate)

- Added developer-only **Save notes backup** (Android CreateDocument SAF) and **Restore notes backup** (OpenDocument SAF) to preserve owner annotations across necessary uninstall/reinstall between ephemeral debug signer and permanently owner-signed Developer app. No public Play/Direct audio/version/signature/UI changes, no automatic cloud upload.
- Import validates exact Developer product/package identity, <=2 MB JSON, 1–250 entries, unique IDs, screen, locator syntax, verbatim comment lengths, P0/P1/P2, Open/Investigating/Resolved, timestamps; rejects corrupt/foreign backups before modifying local data. Accepts earlier full Developer JSON report schema when correctly identified.
- Displays new/identical/conflicting/stale-locator counts; offers separate no-overwrite vs explicit replace-existing actions. Merge never deletes unrelated existing notes; unknown old locators survive and remain visible in audit; SharedPreferences commits are synchronous.
- Added JVM JSON parser/merge regression tests and expanded actual installed-emulator inspector QA to require both backup/restore entry points. **The picker/file round trip on the owner's phone remains unverified**, and a new signing certificate cannot update the old debug-signed APK in place; user must export before uninstalling.
- Source PR candidate only; no new public release, APK signing, hardware audio test or private signer provisioning.

---

## 2026-10-20 — Operación Granada: durable, independent Android Developer signing QA (source candidate)

- Candidate branch `feat/granada-developer-android-persistent-signed-channel-20261020`: allow a strictly bounded **Developer-only** versionCode override (`-PdeveloperVersionCode=10001+`) while preserving public Android 0.1.12/code13 and existing release signing.
- New owner-opt-in **manual PRIVATE signing workflow TEMPLATE** (stored outside runnable public `.github/workflows/`; must be deployed into a separate private signer repository) requires four dedicated Developer keystore Secrets and an owner-pinned certificate SHA-256 variable. Signing refuses unconfigured/changed signer; no auto key generation, no stable public release or updater feed mutation.
- Build and independently sign Developer APKs N-1 and N, then verify same-signer **in-place** emulator update retains a real annotated Compose section, runs read-only audit and exports reviewed JSON. Only a passing result produces a seven-day signed owner-QA candidate artifact; unverified staging expires earlier.
- Normal Android CI gains unsigned Developer-release compilation plus safety contract regression tests. `docs/DEVELOPER_ANDROID_SIGNED_CHANNEL.md` gives key setup/migration protocol and warns that debug-signed notes cannot be automatically migrated across signer changes.
- **Current status: source PR candidate, not yet CI accepted or merged; Secrets not provisioned by this change, manual signing not run, no APK released, physical Samsung audio not tested.** Native Windows DSP/virtual endpoint #46/#48 and shipping artifact #33 remain separate.

---

## 2026-10-20 — Operación Granada: Android Developer notes UX follow-up (candidate)

- Candidate branch `feat/granada-android-developer-note-filters-20261020`: filter real persisted owner annotations by All/Open/Investigating/Resolved inside the inspector, with visible/total counts and per-note Edit/Locate/Delete unchanged. Exports and automatic audit still include all underlying notes, irrespective of visual filtering.
- Add pure unit regression tests for every status, verbatim comments, timestamps and no accidental data mutation. The installed Developer emulator flow also exercises actual Resolved→All taps, verifies that the note disappears/returns without deletion, and retains a real filtered-screen PNG. No public app/audio/version/release changes.
- Status: **source candidate**; compilation and installed QA required. This feature does NOT claim Android release or owner-device acceptance.

---

## 2026-10-20 — Operación Granada: Developer Android integrated, pre-release quality gates (candidate)

- PR #79 **merged** into `main` at `c3fdce0f12679855e8957f6606c4c5e8e35c69d8` after exact-head Android run `39402929535` (Play/Direct/Developer compile/unit, installed Home/About capture, actual Developer anchored note persisted across restart and reviewed JSON) and Windows run `39402918588` both **SUCCESS**. Windows Developer #78, Android navigation #80/#81 and Windows readiness audit #82 are also integrated. **Emulator evidence is not owner Samsung/Windows acoustic acceptance**.
- Current pre-release branch `fix/granada-release-dev-smoke-gates-20261020` adds fail-closed Windows shipping checks for `--check-developer-contract`, live `--developer-smoke-test`, **installed** Developer shortcut identity and physically installed WinForms auditor. The identical installed binary test is invoked from normal Windows build CI and from next-version release CI via `desktop/tools/check_developer_installed.ps1`; no test duplication. Android stable `release_guard` additionally depends on the real `developer_qa` installed emulator job, so Developer inspection failure cannot silently flow into a newly signed public APK. These checks must pass Windows CI before source merge; they do NOT create new publishing triggers.
- Release preparedness and separate signed Android/Windows artifact gates: `desktop/docs/GRANADA_RELEASE_READINESS_2026-10-20.md`. Android Developer debug APK is not an official persistent signed Developer distribution. Native audio #46/#48, exact shipping artifact #33, owner Developer acceptance #77 and signing continuity #40 stay tracked.
- **Distribution unchanged:** Android public v0.1.12, Desktop public v0.1.13; no version bump, new release notes, signed publication or public updater changes authorized.

---

## 2026-10-20 — Operación Granada: Android Developer Edition independent workstream (candidate)

**Objective:** establish a physically separate installable Android developer clone for actual Home/About inspection, owner-anchored notes, local audit, privacy-reviewed JSON/AI exports. Contract: master \`DEVELOPER_TWIN_AUDIT_STANDARD.md\`, issue #77, \`docs/DEVELOPER_EDITION_ANDROID.md\`.

**Candidate changes in \`feat/granada-20261020-android-developer-twin\`, not a stable release:**
- Third Gradle flavor \`developer\` with package suffix \`.developer\`, distinct launcher label, no Android app-data backup or boot startup. Retain existing Play and Direct flavors, updates, stable key and source versions unchanged. Disable real updater and auto-start from Developer; preserve actual audio engine and customer UI on its isolated variant.
- Modeless Compose inspection overlay tied to dynamically measured bounds on real Home/About controls (including seven quick boost levels and seven EQ bands); consumes selection touches instead of activating Boost/EQ. Anchored notes preserve exact comments/locators/timestamps/severity/status and survive restarts with package-isolated local storage.
- Developer-only Run Audit checks live state invariants without pretending it measured actual gain; unresolved anchors, corruption and hardware limitations surfaced. JSON and AI prompt include complete user-authored observations and offer owner redaction before ACTION_SEND.
- Tests: developer/debug build plus unit contracts; Play/Direct compilation and package identity regression; actual emulator/owner-device verification still separate and NOT CLAIMED.

**Status:** source candidate. CI exact-head, installed/real UI, touch safety, target Samsung physical audio and owner signoff must be confirmed before completion; Android release must not be published from this development request. Windows twin workstream remains PR #78. Independent native audio #46/#48, source-to-final-binary QA #33 remain open.

### GRANADA continuation — 2026-10-20, recovery after Android emulator regression

- Real CI prior to this block: three product flavors compiled and passed unit tests, but installed Developer workflow failed to reach app UI because Pixel Launcher ANR obscured both public and Developer apps (run 39396543973); **no false pass**.
- Independent recovery PR #81 passed real Home/About installed emulator CI and merged as `d2833bc`. The Developer E2E harness now reuses the same exact single-time Pixel Launcher recovery while refusing to treat dismissal as proof of app readiness.
- Developer CI now coinstalls real public and Developer APKs, checks distinct package identities, tests actual note workflow, and retains diagnostic pixels when E2E fails. Launcher class differs from Developer applicationId; launch uses actual qualified component.
- Corrected Compose Home/About activation order so live bounds are not reset after rendering; added hermetic installed QA-harness tests.
- Status: source candidate; latest full installed E2E acceptance and physical owner verification **pending**, no release.


---

## 2026-10-20 — Operación Granada: Windows Developer audio readiness audit (candidate)

**Implemented in separate branch `feat/granada-20261020-windows-audio-readiness-audit`, pending exact-head CI and integration.** Existing read-only Developer inspector now differentiates missing APO installation, inactive include, actual APO file/config mismatch with isolated Developer settings, and a matching file whose audible signal remains unverified. Adds read-only current default Windows multimedia render endpoint availability probe without leaking endpoint names or altering volume/mute. Pure self-test covers four APO status classifications, and pre-existing Windows runtime inspector checks still run. Product audio engine and public app unchanged. Documentation: `docs/WINDOWS_DEVELOPER_AUDIO_READINESS_2026-10-20.md`.

**Unresolved:** real owner PC proof that APO is attached and affects outside-app music or speech, endpoint Bluetooth/wired checks, clipping/quality and native virtual endpoint driver #46/#48. This block must not claim physical DSP validation and does not authorize a new release.

---

## 2026-10-20 — Operación Granada: Windows Developer Edition inspection + isolated diagnostics (candidate)

**Large-block objective:** make the existing real Windows LouderMe UI inspectable by the owner without corrupting the public edition or falsely claiming that audio has been verified. Main reference: issue #77, Michel's Lab Developer Twin Standard and \`docs/DEVELOPER_EDITION_WINDOWS.md\`.

**Implemented in branch \`feat/granada-20261020-desktop-developer-audit\`, NOT YET RELEASED:**
- Launch \`--developer\` and dedicated Inno Setup Start Menu shortcut, with separate mutex, developer-only settings directory, annotations and diagnostics; preserves the public UI/engine.
- Live pointer inspection of Home/About WinForms controls. Consumes inspection mouse clicks, captures stable target locator, screen, label, severity, exact owner comment, timestamps and status; provides edit/delete and persistent atomic JSON save.
- Read-only audit of real UI tree, About visibility and Equalizer APO configuration readiness; unmatched anchored annotations remain unresolved. Does not claim audible processing.
- Local export of structured audit JSON and AI-repair prompt with both automated findings and verbatim owner annotations. Manual editable review/redaction step; no network upload and no automatic stack, raw input or secret capture.
- Developer mode cannot toggle public Windows startup or install over the customer edition via updater.
- CI additions: contract test for storage isolation/export/persistence/corruption recovery, actual Windows Developer Edition launch and WinForms runtime audit smoke test.

**Validation state:** source staged; remote PR/Windows workflow status to be recorded separately. No local Windows SDK available in this session. No physical-device UI or sound confirmation. Android Developer Edition remains separately pending. Owner Windows v0.1.13 functionality is already recorded in \`docs/WINDOWS_OWNER_AUDIO_VALIDATION_2026-10-12.md\`; native audio path #46/#48 and exact artifact QA #33 are still open. Stable public release must not be published without explicit authorization.

**Granada campaign:** (A) Desktop Developer edition + live notes/export; (B) Android Developer edition (#77), separate platform; (C) native audio physical routing (#46/#48), blocked on signed virtual endpoint/test hardware; (D) final binary visual evidence (#33), independent QA; (E) physical APO and cross-device gain regression, pending owner machine. Skip conflicts/unverified blocks rather than calling them complete.

---

## 2026-10-20 — GRANADA ambiciosa: macro-bloques multi-entregable (regla adoptada)

**Orden del propietario:** evitar correcciones mínimas encadenadas; elegir campañas de desarrollo con aproximadamente **3–6 paquetes sustanciales relacionados y no interferentes**, cuando el backlog lo permita. Cada paquete debe entregar código/experiencia tangible, recuperar estados y bordes, añadir pruebas de regresión y documentar las verificaciones reales. El tamaño no justifica pisar ramas/claims, mezclar cuentas, omitir QA ni dar por concluida una aceptación física pendiente. Si algún paquete está bloqueado por login, hardware, CI o release, aparcarlo con issue/PR/SHA y continuar con otro trabajo seguro. El objetivo es hacer más, **no aparentarlo**. [Estándar maestro](https://github.com/michels-lab/Michel-Software-Standards/blob/main/standards/OPERACION_GRANADA_STANDARD.md).

**Objetivo de producto para próximas Granadas:** Real Android/Windows volume and EQ behavior, audio-route limitations, updater, Developer Twin, permission and installed regression checks. Do not label output as physical dB without calibrated device evidence.

**Estado de esta entrada:** adopción del protocolo únicamente; no afirma nueva funcionalidad, tests completados, integración de código ni publicación. Todo release/producción exige autorización expresa. Conservar los PRs y pendientes previamente registrados.

# LouderMe — Project Audit Log

Permanent chronological log for decisions, changes, tests, findings, regressions, pending work, and current status.

## 2026-10-05 — Project initialization

### Change / finding
The project was defined as an Android-first general audio booster, not merely a system volume control.

### Product decision
The primary goal is to increase the phone's general audio output so low-volume media can be heard more loudly. User-facing quick controls include 100%, 125%, 150%, 175%, and 200%. Mixer/EQ and predefined mixes are core secondary features. Smart Boost remains experimental and disabled by default.

### Action completed
- Product name selected: **LouderMe**.
- GitHub repository created as `realmichelduarte/LouderMe`.
- Android package fixed as `com.realmichelduarte.louderme`.
- README, proprietary LICENSE, permanent project audit log, architecture notes, project protocol, and About specification prepared.
- Initial Jetpack Compose shell created with boost-level controls, Mixer/EQ entry point, Smart Boost Beta entry point, and About screen.
- Initial version set to `0.1.0`.

### Validation
- Repository naming and package identity reviewed for consistency.
- About reads the version from `BuildConfig.VERSION_NAME` rather than a hard-coded production version.
- No claim of working global amplification has been made.

### Pending
- Prove reliable global audio processing on the target Android/Samsung device.
- Determine the supported system-wide audio-effect path on the target Android version.
- Implement boost engine with limiter/protection.
- Connect 100–200% quick controls to the real audio engine.
- Implement fine boost control, Mixer/EQ, preset persistence, and preset library.
- Implement Smart Boost Beta only after the manual/global path is stable.
- Add automated tests and device validation checklist.
- Produce an installable APK after the audio engine reaches a testable milestone.

### Current status
Foundation release `v0.1.0` prepared. UI shell and project protocol are established; the core global audio engine remains unimplemented and unvalidated.

## 2026-10-05 — Initial release CI setup

### Change / finding
The first two automated release attempts failed during Android SDK environment setup before the app build started.

### Evidence
- Run 1 failed because `android-actions/setup-android@v3` attempted to install the obsolete SDK package `tools`.
- Run 2 failed because the runner's preinstalled `sdkmanager` executable was not exposed on `PATH`.

### Action completed
- Removed the failing `setup-android` dependency.
- Updated the workflow to call the runner's Android SDK manager through its explicit SDK path.
- Updated checkout and Java setup actions to current major versions where applicable.
- Kept release publication gated behind a successful APK build.

### Current status
CI workflow corrected and queued for another validation run. No release will be published unless the APK build succeeds.


## 2026-10-05 — AndroidX build fix

### Change / finding
The release workflow reached the actual Android build successfully, but Gradle stopped at `:app:checkDebugAarMetadata` because the project uses AndroidX dependencies while AndroidX support was not enabled in Gradle properties.

### Evidence
GitHub Actions reported: `android.useAndroidX` was not enabled while the runtime classpath contained AndroidX Compose, Activity, Lifecycle, and related dependencies.

### Action completed
- Added root `gradle.properties`.
- Enabled `android.useAndroidX=true`.
- Triggered a fresh `v0.1.0` validation build.

### Current status
Awaiting the new CI build. Release publication remains gated behind a successful APK build.


## 2026-10-05 — Foundation release v0.1.0 published

### Change / finding
After enabling AndroidX, the next validation build reached Kotlin compilation and reported inconsistent JVM targets: Java was targeting 1.8 while Kotlin was targeting 17.

### Action completed
- Added Java 17 source/target compatibility to the Android module.
- Added Kotlin JVM toolchain 17.
- Re-ran the complete GitHub Actions build.
- Android debug APK compiled successfully.
- Release asset was prepared and uploaded.
- GitHub release `v0.1.0` was published successfully.

### Validation / evidence
- GitHub Actions run: `37291824472`.
- `:app:assembleDebug`: successful.
- Release tag: `v0.1.0`.
- Release title: **LouderMe v0.1.0 — Foundation**.
- APK asset: `LouderMe-v0.1.0-debug.apk`.
- APK size: 28,042,227 bytes.
- APK SHA-256: `42b5e15ca3e7c5e4b0b7b7e7ed7b6ac13eb166b6bc6b224564269dae1ae1600a`.
- Release page: https://github.com/realmichelduarte/LouderMe/releases/tag/v0.1.0

### Current status
The LouderMe foundation now builds successfully and has an installable debug APK release. The global/system-wide amplification engine remains intentionally unimplemented and unvalidated; implementing and testing that engine is the next product milestone.


## 2026-10-05 — v0.1.1 infrastructure and real audio-engine candidate

### Audit finding
A cross-app audit found that updater work had left LouderMe internally inconsistent: build metadata was already 0.1.1, MainActivity expected updater-aware UI, but the Compose UI was still the 0.1.0 signature. The existing workflow also reported green after v0.1.0 existed because it skipped the actual Android build.

### Decisions
- Michel's Lab is now the umbrella product identity.
- Android package migrated before Play publication to `com.michelslab.louderme`.
- Current-commit build validation and release publication are separate CI gates.
- Google Play is the production automatic-update channel.
- Global audio uses session 0 only as an experimental compatibility path because Android deprecates global insert effects on session 0.

### Implementation prepared
- Michel's Lab native About with canonical portrait and official links.
- Google Play update status/actions.
- User-started special-use foreground service.
- LoudnessEnhancer session-0 engine with DynamicsProcessing fallback.
- 100–200% quick levels and fine slider connected to engine requests.
- Engine/output-route diagnostics.
- Unit tests for gain mapping.
- Privacy and Play-readiness documentation.
- New CI that always builds/tests HEAD and publishes only after validation.

### Validation boundary
No claim of reliable system-wide boost is permitted until a physical target-device A/B test confirms external-app audio changes.

### Next
Run branch CI, fix compilation/test issues until green, merge to main, publish v0.1.1, then perform S26 Ultra audio validation.


## 2026-10-05 — LouderMe v0.1.1 validated and published

### Validation
- Release-candidate branch commit: `119b3a4982c411f3304c60e3163129d233cb0dd9`.
- Branch CI run `37299633996`: **success**.
- Branch validation actually executed unit tests, Android compilation, APK package-ID verification, checksum preparation, and artifact upload.
- Squash merge to main: `aec0f7c38923c237f12429c16e8b994622565dd1`.
- Main CI/release run `37299949758`: **success**.
- Main build again executed the full current-HEAD validation before publication.

### Release evidence
- Tag: `v0.1.1`.
- Release: **LouderMe v0.1.1**.
- APK: `LouderMe-v0.1.1-debug.apk`.
- APK size: 28,932,073 bytes.
- APK SHA-256: `e57e9589dc2cae76efaa1c4f697ec3da5a7a917563f2b3edbedebaaddad3102a`.
- Checksum file published alongside the APK.
- Package verified by CI: `com.michelslab.louderme`.

### Current status
The app now has a compiled experimental global-audio compatibility engine and production-oriented update infrastructure. **System-wide amplification is not yet considered validated** because Android deprecates global insert effects on session 0 and the target Samsung device still needs an external-app A/B test.

### Next device test
Install v0.1.1 on the target Samsung phone, play the same Spotify/YouTube passage at a fixed system volume, compare 100% vs 150% vs 200%, and report the in-app engine status/diagnostic implementation. That evidence determines whether session 0 is genuinely useful on the device or a different architecture is required.

## 2026-10-05 — Infrastructure / cloud audit

Added `docs/INFRASTRUCTURE_AUDIT.md`.

LouderMe's core audio path remains fully local. No Supabase/Google backend is required for v0.1.1. The important infrastructure gates are physical audio validation, Play production delivery/update validation and later persistence of mixer/EQ presets. Cloud sync becomes relevant only if account-based preset/profile features are deliberately added.


## 2026-10-05 — Target-device validation and 250% range

### Device validation
Michel reported that LouderMe v0.1.1 audibly increases general/external-app audio on the target Samsung device using the session-0 compatibility engine.

This validates the current path for the intended device, not universally across Android hardware.

### Product decision
Extend manual boost from 200% to **250%**.

New quick levels:
- 225% = +7.04 dB target signal gain.
- 250% = +7.96 dB target signal gain.

### Technical clarification
The displayed dB value is **digital target signal gain**, derived from the amplitude ratio:

`G_dB = 20 log10(A2/A1)`

It is not a measurement or prediction of loudspeaker sound-pressure level (dB SPL). Acoustic output depends on the output device, amplifier/headroom, vendor DSP, limiter/compression behavior, source material, frequency response, and listening geometry.

### Implementation
- All internal boost clamps raised to 250%.
- Slider extended to 250%.
- 225% and 250% quick controls added.
- Quick controls reflowed into two rows.
- UI wording changed to "target signal gain".
- Unit tests extended through 250%.
- Version bumped to v0.1.2 / versionCode 3.

### Status
Pending CI validation and release publication.

## 2026-10-05 — Michel's Lab parent/child governance contract

Added the repository-level Michel's Lab governance declaration:

- `.michelslab/project.yml` identifies `realmichelduarte/Michel-Software-Standards` as the shared standards authority.
- `MICHELS_LAB_PROJECT.md` documents the human-readable reporting contract.
- App-specific implementation evidence remains in this repository.
- Reusable/cross-app decisions are promoted to the master standards repository.
- The master repository polls child status centrally; this repository receives no credential that can write to the master.
- Secret values remain prohibited from both repositories.


## 2026-10-05 — LouderMe v0.1.2 published

### Validation
- Release-candidate branch CI: `37304511063` — **success**.
- Main CI/release run: `37304856564` — **success**.
- Current-HEAD unit tests: passed.
- Android build: passed.
- Packaged application ID: `com.michelslab.louderme`.
- Release publication: passed.

### Release evidence
- Tag: `v0.1.2`.
- Release: **LouderMe v0.1.2**.
- APK: `LouderMe-v0.1.2-debug.apk`.
- APK size: 28,948,461 bytes.
- APK SHA-256: `5516c990541bf865189d8ae5be8f718d2edb278fca1bbaed277f559ca6c3e39a`.
- Checksum file published alongside the APK.

### Product state
The manual boost range now spans 100–250%.

The displayed dB figure is explicitly **target digital signal gain**, not a measurement of speaker/headphone sound-pressure level (dB SPL).

Target Samsung device evidence from v0.1.1 remains positive: Michel reported audible general/external-app amplification using the current session-0 engine path.

### Next
- evaluate high-gain quality at 225% and 250%;
- implement Mixer / EQ;
- add limiter/compression strategy for high-gain presets;
- continue Play Store production readiness.


## 2026-10-05 — v0.1.3 IG Cleaner visual family + real equalizer

### Request
LouderMe should visually resemble the latest IG Cleaner Pro and should already include the equalizer, updater and surrounding production infrastructure rather than being only a visual reskin.

### Source UI audit
The current IG Cleaner Pro visual system uses:
- near-black workspace background `#060910`;
- canvas/surfaces `#090E17`, `#0D1521`, `#111C2B`, `#162335`;
- cyan `#71D7FF`, blue `#5D9CFF`, gold `#EFBD62`;
- thin translucent borders;
- ~20 px rounded surfaces;
- compact monospace operational labels;
- command-bar / workspace / dashboard-card hierarchy.

### Visual implementation
LouderMe now translates that language into native Jetpack Compose:
- Audio Workspace command bar;
- Boost Center workspace header;
- large live-engine hero;
- quick boost cards;
- fine-tune panel;
- metric dashboard;
- integrated equalizer panel;
- engine diagnostics;
- Audio Lab next-module deck;
- matching About surface.

No WebView or copied HTML is used.

### Equalizer implementation
Added a real Android session-0 Equalizer path.

LouderMe exposes seven stable target bands:
60 / 150 / 400 / 1000 / 2500 / 6000 / 12000 Hz.

Because device Equalizer implementations can expose a different band count:
- each target frequency is mapped using Android's native band lookup;
- targets resolving to the same native band are grouped;
- requested gain is averaged for that native band;
- requests are clamped to the device-reported supported level range;
- native center-frequency mapping is surfaced in Diagnostics.

User-facing target EQ range: -10 to +10 dB.

Presets:
Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones.

Manual adjustment creates Custom.

EQ settings persist locally and apply immediately while Global Boost is active or on the next service start.

### Updater
The existing Google Play updater remains intact and is now visible directly on Home:
- automatic availability checks;
- current status;
- Check action;
- Install action when available;
- About retains the same controls.

### Version
Bumped to v0.1.3 / versionCode 4.

### Validation boundary
The boost path is already audibly validated on the target Samsung device.

The equalizer is a real connected effect, but must still pass compile/tests and then audible target-device validation before its Samsung behavior is considered proven.

### Current status
Release candidate implementation prepared. Pending CI.


## 2026-10-05 — LouderMe v0.1.3 validated and published

### Validation
- Release-candidate commit: `1451fdbc1fa995b25f419e27cd0f85d5499784bd`.
- Branch CI run `37384205448`: **success**.
- Branch validation executed unit tests, Android compilation, package verification, APK/checksum generation and artifact upload.
- Squash merge to main: `987d4ab3193c67df3c216c13af244ce4f84c48e2`.
- Main CI/release run `37384489316`: **success**.
- Main validation again compiled/tested current HEAD before publication.

### Release evidence
- Tag: `v0.1.3`.
- Release: **LouderMe v0.1.3**.
- APK: `LouderMe-v0.1.3-debug.apk`.
- APK size: 29,112,349 bytes.
- APK SHA-256: `d34028c20423245b26626c6f2383cddbefc690063ba8c76a4b3deb36170fad90`.
- Checksum file published alongside the APK.

### Delivered
- IG Cleaner-inspired native Audio Workspace.
- Home-visible updater status/actions.
- Real session-0 Equalizer integrated with the foreground audio service.
- Seven LouderMe target EQ bands with device-native mapping.
- Flat, Bass, Deep Bass, Dialogue, Treble, Speaker and Headphones presets.
- Custom EQ mode.
- Local EQ persistence.
- EQ diagnostics.
- 100–250% boost preserved.

### Validation boundary
Boost remains audibly validated on the target Samsung device.

The new EQ compiled and is genuinely connected to Android's Equalizer API, but its audible system-wide behavior on the target Samsung device is still pending direct device validation.

### Next
Install v0.1.3 on the target Samsung, enable Global Boost, switch between Flat / Bass / Dialogue / Treble, confirm audible tonal changes, and inspect EQ Diagnostics for ATTACHED/DEGRADED/UNSUPPORTED plus the native band mapping.


## 2026-10-05 — v0.1.4 direct/sideload automatic updater

### Problem
The v0.1.3 updater only used Google Play In-App Updates. The APK currently installed directly from GitHub is not owned by Google Play, so it cannot use that production Play channel.

The private LouderMe source repository also cannot be queried anonymously from a shipped APK without embedding a private credential, which is prohibited.

### Architecture decision
Create two distribution flavors:
- `play` — Google Play updater; no APK-install permission.
- `sideload` — Michel's Lab direct updater.

The direct updater reads a public manifest from the existing public release infrastructure while the application source stays private.

### Direct updater implementation
The sideload channel now:
1. checks the public manifest automatically;
2. compares versionCode;
3. downloads a newer APK;
4. verifies SHA-256;
5. verifies package ID;
6. verifies candidate versionCode;
7. verifies that candidate and installed LouderMe share an accepted signing certificate;
8. requests Android's per-source install permission when necessary;
9. streams the verified APK through PackageInstaller;
10. lets Android present mandatory user confirmation.

### Play policy separation
`REQUEST_INSTALL_PACKAGES` exists only in `src/sideload/AndroidManifest.xml`.

The Play flavor does not request it.

### Signing finding
The v0.1.3 and earlier GitHub APKs were hosted-CI debug builds. A runner debug key is not an acceptable long-term update identity.

v0.1.4 therefore creates a one-time stable sideload signing key in a private CI bootstrap artifact. The key must be transferred into secure CI secrets and never committed.

Because the currently installed v0.1.3 has a different signing identity, migration to stable v0.1.4 requires one uninstall/reinstall. Future stable sideload builds can update in place.

### Public feed
Bootstrap feed created:
`realmichelduarte/michel-s-life-releases/louderme/latest.json`

No LouderMe source code or private token is exposed by this feed.

### Version
v0.1.4 / versionCode 5.

### Status
Implementation prepared on `release/v0.1.4`. Pending CI validation and stable signing bootstrap.


## 2026-10-05 — v0.1.4 release-Lint failure and Fragment fix

### Failure
GitHub Actions run `37388581098` failed during the real dual-flavor release build.

The failure was not hidden or bypassed. Android Lint reported `InvalidFragmentVersionForActivityResult` for the two `registerForActivityResult` calls in `MainActivity` while assembling the release target.

### Root cause
The release configuration did not have a sufficiently modern explicit AndroidX Fragment runtime available for ActivityResult's release-Lint contract.

### Fix
Added the current stable AndroidX Fragment KTX dependency:

`androidx.fragment:fragment-ktx:1.9.1`

No lint baseline, suppression, or fatal-check disabling was introduced.

### Status
A fresh v0.1.4 branch validation is required. The stable sideload signing bootstrap remains blocked until the full build succeeds.


## 2026-10-05 — v0.1.4 direct auto-update architecture

### Problem
The existing updater worked only for Google Play-owned installations. LouderMe is currently being tested through direct GitHub APK installation, so the user expected the APK build itself to discover and install newer versions.

The private LouderMe repository cannot be queried anonymously from a shipped APK without embedding a private GitHub credential, which is prohibited.

Older GitHub releases were also CI debug-signed. Hosted debug signing is not a permanent release identity, so Android cannot guarantee in-place updates across future runners.

### Architecture
LouderMe now has two explicit distribution flavors:

- `play` — Google Play In-App Updates; no `REQUEST_INSTALL_PACKAGES`.
- `sideload` — Michel's Lab Direct updater; includes `REQUEST_INSTALL_PACKAGES`.

The direct channel:
- reads a public update manifest from the Michel's Lab release hub;
- auto-downloads newer versions;
- verifies SHA-256;
- verifies package ID;
- verifies manifest/APK versionCode;
- verifies signing-certificate continuity;
- installs through Android PackageInstaller;
- handles the Android unknown-sources permission flow;
- records PackageInstaller results.

### Public/private boundary
Source remains private.

The public update feed and signed APKs live in `realmichelduarte/michel-s-life-releases` under the LouderMe channel.

No private repository token is shipped in LouderMe.

### Signing migration
v0.1.4 establishes a dedicated stable sideload signing key.

Because v0.1.3 and earlier were debug builds, the installed old build must be uninstalled once before installing the stable v0.1.4 baseline.

After that one-time migration, same-key direct releases can update in place.

### CI safety
- validation builds both Play and sideload flavors;
- CI explicitly asserts Play does not request `REQUEST_INSTALL_PACKAGES`;
- CI explicitly asserts sideload does;
- one-time signing-key bootstrap is gated behind an explicit `[bootstrap-signing]` commit;
- normal main releases require stable key secrets;
- public-feed publication requires a dedicated cross-repo token;
- the signing key is never committed to source.

### Current status
Code and CI architecture prepared. Pending final branch validation, one-time signing bootstrap, secret installation and public v0.1.4 baseline publication.


## 2026-10-06 — Stable sideload signer bootstrap validated

### Validation
- Bootstrap workflow run: `37392415939`.
- Current-HEAD Android validation: **success**.
- Play + sideload flavor builds/tests: **success**.
- Play permission audit: **success** — no `REQUEST_INSTALL_PACKAGES`.
- Sideload permission audit: **success** — `REQUEST_INSTALL_PACKAGES` present.
- Stable sideload signing bootstrap: **success**.
- Signed bootstrap artifact: `LouderMe-v0.1.4-stable-sideload`.
- Private signing handoff artifact: `LouderMe-sideload-signing-material-PRIVATE`.
- Private handoff expires 2026-10-13 and must be transferred to GitHub Actions secrets plus a secure offline backup.

### Stable signing identity
- Certificate DN: `CN=Michel's Lab, OU=Software, O=Michel's Lab, L=Saltillo, ST=Coahuila, C=MX`.
- Certificate SHA-256: `4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3`.
- RSA key size: 4096 bits.
- APK signature scheme verified: v3.
- Signed v0.1.4 sideload APK size: 22,100,564 bytes.
- Signed APK SHA-256: `7eee11c065330d0064378172840cdb5aa067c17463332db8a6c6626e674f9d83`.

### Security boundary
The private key/password material is not committed to Git and must never be copied into app source, documentation, logs, chat, or the public update feed.

### Remaining infrastructure gate
The GitHub connector cannot write Actions Secrets. Before merging v0.1.4 to `main`, the private handoff values must be added to the LouderMe repository as Actions secrets, and a fine-grained cross-repository token must be added for publishing the public Michel's Lab release feed.

### Migration
v0.1.3 and older direct APKs were CI-debug signed. Android cannot update those in-place to the new stable signer.

One-time migration:
1. uninstall the old direct/debug LouderMe;
2. install the stable signed v0.1.4 sideload baseline;
3. from v0.1.5 onward, same-signer direct updates can install in place after Android confirmation.


## 2026-10-06 — v0.1.4 final direct-update channel validated and released

### Final validation

The permanent Michel's Lab Direct update path is now operational.

- Final release-candidate build/sign run: `37424013605` — **success**.
- Squash merge to `main`: `ad1f3284ea8e39552ae0711a48f995ea7976f2c3`.
- Final `main` build/sign/private-release run: `37424364775` — **success**.
- Public binary publisher run: `37423737829` — **success**.
- Play and sideload unit/build validation: passed.
- Play permission audit: passed — no `REQUEST_INSTALL_PACKAGES`.
- Sideload permission audit: passed — install permission present.
- Stable sideload signing: passed.
- Private signing vault refresh: passed.
- Private v0.1.4 release publication: passed.
- Public v0.1.4 binary release publication: passed.
- Public `latest.json` update: passed.

### Authoritative stable signer

The final canonical Michel's Lab Direct signing identity is:

- DN: `CN=Michel's Lab, OU=Software, O=Michel's Lab, L=Saltillo, ST=Coahuila, C=MX`.
- certificate SHA-256: `e0d497f4872c116632f040e51e08b7beb410f0a3df4d2b959d22fbd7bde48479`.
- RSA: 4096-bit.
- APK signature scheme: v3.

**Supersession note:** earlier v0.1.4 development/bootstrap entries that recorded certificate SHA `4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3` and bootstrap APK SHA `7eee11c065330d0064378172840cdb5aa067c17463332db8a6c6626e674f9d83` are provisional historical evidence and are **not** the final release identity. The `e0d497...` certificate above is authoritative.

### Release evidence

Private source-repo release:
- tag: `v0.1.4`;
- APK: `LouderMe-v0.1.4-sideload.apk`;
- size: 22,100,564 bytes;
- APK SHA-256: `b2687abd2242265abe7322426a22ba7069e58aecfe228b2206d79151f89331df`.

Public Michel's Lab Direct release:
- tag: `louderme-v0.1.4`;
- APK: `LouderMe-v0.1.4-sideload.apk`;
- size: 22,100,564 bytes;
- APK SHA-256: `7c4231e718fba60a74df0717a68f0f5ff0b9273b79194de501ffc41b4386958a`.

The two whole-file hashes differ because they were independent builds. Both APKs were verified against the same final canonical signing certificate, which is the property Android requires for update continuity.

### Public feed

`realmichelduarte/michel-s-life-releases/louderme/latest.json` now advertises:

- package: `com.michelslab.louderme`;
- versionName: `0.1.4`;
- versionCode: `5`;
- public APK URL;
- SHA-256 `7c4231e718fba60a74df0717a68f0f5ff0b9273b79194de501ffc41b4386958a`.

The LouderMe APK does not contain a private GitHub token.

### Signing continuity

Normal CI now preserves one stable signing identity.

The workflow prefers valid GitHub Actions signing Secrets but can recover from missing/stale Secrets through a private Actions signing-vault artifact. The vault is refreshed after successful signing and by a scheduled monthly run.

The stable signer is not regenerated during normal releases.

### Migration requirement

v0.1.3 and older direct builds are debug-signed and cannot update in place to the permanent signer.

Exactly one migration reinstall is required:

1. uninstall the old debug-signed LouderMe;
2. install the stable v0.1.4 Michel's Lab Direct APK;
3. from v0.1.5 onward, LouderMe can automatically discover/download/verify newer direct releases and Android can accept them as same-signer updates after the required system confirmation.

### Status

**v0.1.4 is released and the Michel's Lab Direct update channel is operational.**

No additional manual GitHub browsing should be necessary for normal same-signer sideload updates after the one-time v0.1.4 migration.


## 2026-10-06 — v0.1.5 high-gain peak protection candidate

### Goal
Improve quality and overload handling at 225–250% without replacing the Samsung-validated LoudnessEnhancer path.

### Platform finding
Android's LoudnessEnhancer already compresses samples that would otherwise exceed the platform sample range. DynamicsProcessing exposes a dedicated Limiter stage intended to protect the signal from overloading and distortion.

### Implementation
- Kept LoudnessEnhancer as the primary session-0 engine.
- Added `PeakProtectionPolicy` for the DynamicsProcessing fallback.
- Explicit limiter begins at 175% and becomes progressively stronger at 200%, 225% and 250%.
- Limiter configuration failure is non-fatal; the fallback continues and reports the condition in diagnostics.
- Added unit tests for policy thresholds, progression and supported-range clamping.
- Bumped candidate version to v0.1.5 / versionCode 6.

### Validation boundary
Code is prepared on `release/v0.1.5`. CI must pass before merge or release. Real-device listening remains required to judge tonal quality and audible limiting behavior.

### Current status
Release candidate prepared; pending CI.


## 2026-10-06 — Android status-bar collision fix

### Finding
A physical target-device screenshot showed the custom LouderMe Home command bar occupying the Android status-bar region. The LM mark, product title and About action visually collided with the clock, media/notification indicators and system icons. The About screen used the same unsafe custom-top-bar pattern.

### Root cause
The app intentionally renders edge-to-edge on modern Android, but its custom Compose top bars did not apply system safe-drawing insets. Material 3 `Scaffold` does not automatically make arbitrary custom top-bar content respect the status-bar/cutout inset.

### Action completed
- Applied `WindowInsets.safeDrawing` to the foreground content of the Home command bar.
- Limited the inset application to Top + Horizontal sides so the decorative bar background can remain edge-to-edge without adding irrelevant bottom navigation padding.
- Applied the same correction to the About top bar.
- Preserved the existing layout proportions and Michel's Lab visual design.
- Did not use a device-specific fixed dp/pixel workaround.

### Validation
- Source-level review confirms both custom top bars now consume safe-drawing top/horizontal insets before their ordinary visual padding.
- Android compilation/CI is the next automated gate.
- Final visual confirmation remains a physical-device check because the original defect was device-rendered.

### Current status
Implementation is on `main` in commit `ed524ce1fb5ba97499fe53229781a1f5cb2a0fb1`. Pending CI result and on-device screenshot confirmation.

## 2026-10-06 — GitHub Copilot agent delegation

Added repository-level Copilot instructions and custom App Maintainer, QA Regression and Release Manager agents. The contracts preserve Play/sideload separation, stable direct-update signing, SHA/package/version/signing checks, correct gain-vs-dB-SPL semantics, EQ mapping transparency, Android safe-area rules and explicit target-device validation gates.

Purpose: delegate bounded implementation, regression checks and release preparation to repository agents so cross-project ChatGPT work can focus on product decisions and coordination.

No audio behavior, version, signing material or release artifact changed in this infrastructure-only update.

## 2026-10-06 — ChatGPT-ready task intake

Added `.github/ISSUE_TEMPLATE/chatgpt-task.yml` so new implementation/audit tasks can capture the desired outcome, evidence, scope, acceptance criteria, required validation and release permission up front.

Purpose: reduce repeated context reconstruction in future ChatGPT sessions and make repository work resumable from a bounded GitHub Issue without changing product behavior.
\n

## 2026-10-06 — Official LouderMe product identity integration

### Source of truth
Consumed the official LouderMe branding from `realmichelduarte/Michel-Software-Standards`:
- approved concept: **Option 4 — flowing waveform**;
- `shared-assets/product-logos/louderme/official-app-icon.svg`;
- `shared-assets/product-logos/louderme/official-mark.svg`;
- `shared-assets/product-logos/louderme/official-lockup.svg`.

The canonical SVGs are copied into `app/src/main/assets/branding/louderme/` for provenance. Android runtime assets are local platform derivatives; the app does not hotlink the private standards repository.

### Implementation
- Added official launcher and round-launcher identity.
- Added adaptive Android launcher foreground/background assets.
- Added official branded launch/splash treatment.
- Replaced the temporary `LM` command-bar placeholder with the official waveform mark.
- Added the official waveform/wordmark/tagline treatment to the About hero.
- Preserved the existing IG Cleaner-family LouderMe layout and colors outside the requested branding surfaces.
- No audio, updater, permission, signing, distribution or cloud behavior was changed.
- No version bump or release publication was performed as part of this branding-only implementation.

### Validation
Pending pull-request CI for Android resource compilation, both distribution flavors, package IDs and permissions.

### Current status
Official LouderMe identity is implemented on the branding branch and awaiting CI validation before merge.


### Branding validation evidence — 2026-10-06
- PR #9 merged to `main` as `8b1aa2ff4efabbcbdc0b5db063cec0df4ae91898`.
- First PR CI run `37437629602` failed during Android resource linking because a literal color was incorrectly used as a layer-list `drawable` reference in `louderme_launch_background.xml`.
- Corrected the splash background to use an embedded shape/solid drawable.
- Second PR CI run `37437798285`: **success**.
- Unit tests: passed.
- Play debug build: passed.
- Sideload debug/release builds: passed.
- Package-ID and Play/sideload permission separation checks: passed.
- Validation artifacts uploaded successfully.
- No product version bump was made for this branding integration.


## 2026-10-06 — v0.1.6 stable branding release candidate

### Release authorization
Michel explicitly authorized a **stable release**, not a prerelease.

### Scope
v0.1.6 packages the already-merged official LouderMe branding:
- Option 4 — flowing waveform;
- official launcher/round launcher;
- Android adaptive icon;
- branded launch/splash;
- official command-bar mark;
- official About waveform/wordmark/tagline treatment.

### Version
- versionName: `0.1.6`
- versionCode: `7`

### Preserved behavior
No change to:
- 100–250% boost behavior;
- LoudnessEnhancer/DynamicsProcessing engine selection;
- EQ mapping or presets;
- updater verification;
- stable sideload signer;
- Play/sideload permission separation;
- local privacy behavior.

### Validation / release gate
The release branch must pass full Android CI. After merge to `main`, the stable signing job and private release publication must succeed. The signed artifact must then be propagated to the public Michel's Lab Direct release repository and `louderme/latest.json` must advertise versionCode 7 / versionName 0.1.6.

### Current status
Release candidate prepared; pending CI.


## 2026-10-06 — LouderMe v0.1.6 stable release published

### Authorization
Michel explicitly requested a **release**, not a prerelease.

### Validation
- Release PR: #10.
- Release-candidate CI run: `37497219727` — **success**.
- Merge to `main`: `1e8c29a99ce402fe24e2bdeba7dc4180771d0ff8`.
- Main build/sign/release run: `37497675432` — **success**.
- Unit tests: passed.
- Play build: passed.
- Sideload build: passed.
- Package ID verification: passed.
- Play/sideload permission separation: passed.
- Stable Michel's Lab Direct signing: passed.
- Private release publication: passed.
- Public publisher run: `37498269151` — **success**.
- Transfer-staging cleanup run: `37498345236` — correctly **skipped** by the cleanup guard.

### Release evidence
Private source release:
- tag: `v0.1.6`;
- release type: stable, not prerelease;
- APK: `LouderMe-v0.1.6-sideload.apk`;
- APK size: 22,130,370 bytes;
- APK SHA-256: `b0b0165b2b7b967e090083ae0b681d3bf2591eafbc8b9277129fc27cafc3a6e5`.

Public Michel's Lab Direct release:
- tag: `louderme-v0.1.6`;
- release type: stable, not prerelease;
- APK: `LouderMe-v0.1.6-sideload.apk`;
- APK size: 22,130,370 bytes;
- APK SHA-256: `b0b0165b2b7b967e090083ae0b681d3bf2591eafbc8b9277129fc27cafc3a6e5`.

### Public updater feed
`louderme/latest.json` now advertises:
- appId: `com.michelslab.louderme`;
- channel: `sideload`;
- versionName: `0.1.6`;
- versionCode: `7`;
- APK URL: public `louderme-v0.1.6` asset;
- SHA-256: `b0b0165b2b7b967e090083ae0b681d3bf2591eafbc8b9277129fc27cafc3a6e5`.

### Delivered branding
v0.1.6 is the first stable LouderMe release carrying the official Michel's Lab **Option 4 — flowing waveform** identity across launcher, adaptive icon, splash, command bar and About.

### Current status
**LouderMe v0.1.6 is fully released on the stable direct channel and discoverable by the in-app sideload updater.**


## 2026-10-06 — Cross-device startup + native Windows desktop foundation

### User request
- LouderMe Android and LouderMe Desktop must both offer opt-in startup with the device.
- LouderMe Desktop must be a real installable Windows application, not an HTML file.

### Android implementation
- Added an opt-in **Start with phone** preference.
- Added `RECEIVE_BOOT_COMPLETED`.
- Added `BootCompletedReceiver`.
- On Android boot, LouderMe resumes the previously active boost only when:
  - the user enabled start-on-boot;
  - boost was active before shutdown/reboot;
  - notification permission is available where Android requires it.
- The receiver starts the existing foreground audio service instead of attempting to force-open an Activity.
- Startup remains disabled by default.
- Manufacturer/OS background-start restrictions remain an Android platform boundary.

### Windows Desktop foundation
Created `desktop/` as a native Windows product surface:
- .NET 10 WinForms native application;
- NAudio 3.1.0 stable package for Windows Core Audio endpoint access;
- no browser/WebView desktop UI;
- default playback-device detection;
- real Windows master-volume control;
- mute control;
- native **Start LouderMe with Windows** setting via `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`;
- Inno Setup installer with optional startup and desktop-shortcut tasks;
- self-contained single-file portable EXE build;
- official LouderMe icon generated from the canonical Option 4 SVG;
- SHA-256 checksums for portable EXE and installer.

### Accuracy boundary
The current Desktop milestone does **not** claim system-wide gain above the normal Windows endpoint maximum. Windows master volume is 0–100. A true >100% system-wide boost needs a separately validated audio-processing path; the UI explicitly says so rather than relabeling endpoint volume as 250%.

### Versioning
- Android remains v0.1.6 / versionCode 7 on this feature branch.
- Desktop starts as v0.1.0.
- No release publication is authorized by this feature implementation.

### Validation
- Desktop Windows CI is required to prove .NET restore/build, self-contained EXE packaging, icon generation, Inno Setup installer creation and checksums.
- Android PR CI is required to prove the boot receiver/UI integration compiles in both Play and sideload flavors.
- Actual reboot/sign-in behavior remains a physical-device / Windows-session validation item.


### Final validation / merge evidence — 2026-10-06
- PR #12 merged to `main` as `8ef1e49ee40fd4ea565de31c4df9fc5bf5c16b27`.
- Android PR CI run `37512825591`: **success**.
  - Play + sideload tests/builds passed.
  - package IDs and permission split passed.
  - Android validation artifacts uploaded.
- Desktop PR CI run `37512825617`: **success**.
  - .NET 10 restore/build passed.
  - official Windows icon generation passed.
  - self-contained portable EXE publication passed.
  - native startup integration checks passed.
  - Inno Setup installer build passed.
  - portable + installer SHA-256 generation passed.
  - Windows artifact upload passed.
- Earlier desktop branch run `37512672065`: **success**, artifact `LouderMe-Desktop-v0.1.0`.
- Merge commit intentionally includes `[skip ci]` so this feature merge does not mutate or republish the already released Android v0.1.6 artifacts.
- No Desktop release has been published yet.


## 2026-10-06 — Windows Desktop real 100–250% system-wide boost

### Goal
Replace the Desktop placeholder boundary with a real Windows processing path for gain above the normal 0–100 endpoint volume range.

### Architecture decision
Windows endpoint master volume remains a separate 0–100 control through Core Audio.

LouderMe **Global Boost** now targets the Windows system-effects/APO path through Equalizer APO 1.4.2:
- 100–250% is converted with `20 * log10(percent / 100)`;
- 100% = 0.00 dB;
- 150% = +3.52 dB;
- 200% = +6.02 dB;
- 250% = +7.96 dB.

This is real post-mix digital gain, not a relabeled Windows volume slider and not an acoustic dB SPL claim.

### Equalizer
Desktop now exposes the same seven stable product-facing bands as Android:
- 60 Hz;
- 150 Hz;
- 400 Hz;
- 1 kHz;
- 2.5 kHz;
- 6 kHz;
- 12 kHz.

Presets: Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones and Custom.

### Configuration safety
- LouderMe writes only `LouderMe.txt`.
- It adds one `Include: LouderMe.txt` entry to Equalizer APO's main config.
- It creates `config.txt.louderme.bak` before the first main-config modification.
- The managed file explicitly resets `Device: all`, `Stage: post-mix` and `Channel: all` so inherited configuration state does not accidentally scope LouderMe to an unrelated device/channel.
- Existing user Equalizer APO filters are preserved.

### Third-party engine
- Supported engine: Equalizer APO 1.4.2 x64.
- LouderMe does not redistribute the installer.
- The UI opens the official SourceForge download page.
- Official x64 SHA-256 recorded in UI/docs: `7403be7427bbe1936a40dded082829b6e217fc4f5990fee5cba501f0ae055afa`.
- The user selects/attaches the intended playback endpoint in Equalizer APO Configurator.

### Compatibility / audio-quality boundary
- ASIO and WASAPI exclusive-mode streams can bypass Windows APO effects.
- Positive preamp consumes digital headroom; high gain can cause downstream Windows limiting/compression on loud material.
- Repository CI cannot prove endpoint attachment or audible quality.
- Physical Windows validation is still required at 100/150/200/250% and with each important output route.

### Privacy
Processing remains local. No account, telemetry, cloud service, audio upload or new secret was added.

### Version / release
Desktop remains v0.1.0 during this implementation. No Desktop release is published by this change unless separately authorized.


### Final Windows global-boost validation — 2026-10-06
- PR #13: **merged**.
- Merge commit: `443c8cd09af593bd15d9dc7d46d2cd43b8044a18`.
- Desktop PR CI run `37515489882`: **success**.
  - .NET 10 native build passed.
  - real-global-boost source contract passed.
  - 100–250% gain formula contract passed.
  - APO managed configuration contract passed.
  - portable self-contained EXE passed.
  - Inno Setup installer passed.
  - EXE/installer checksums passed.
  - Windows artifact upload passed.
- Android regression CI run `37515490245`: **success**.
  - unit/build validation passed;
  - Play and sideload builds passed;
  - package ID and permission separation passed.
- Additional branch Windows run `37515460340`: **success**.
- Merge used `[skip ci]` intentionally to avoid re-running the already released Android v0.1.6 publication path for a Desktop-only feature.
- Desktop version remains v0.1.0 and is **not yet released**.
- Physical Windows endpoint/APO attachment and audible 100/150/200/250% validation remain required before claiming hardware-level validation.


## 2026-10-06 — LouderMe Desktop v0.1.0 stable release candidate

### Authorization
Michel explicitly authorized publication of the first LouderMe Desktop release.

### Release
- product: LouderMe Desktop;
- version: `0.1.0`;
- channel: stable;
- release type: normal release, not prerelease;
- source tag: `desktop-v0.1.0`;
- public distribution tag: `louderme-desktop-v0.1.0`.

### Planned assets
- `LouderMe-Setup-v0.1.0.exe`;
- `LouderMe-Setup-v0.1.0.exe.sha256`;
- `LouderMe-v0.1.0.exe`;
- `LouderMe-v0.1.0.exe.sha256`.

### Release gate
Publication requires a build from the exact merged `main` commit:
- .NET 10 restore/build;
- real 100–250% global-boost contract;
- Equalizer APO managed-config contract;
- self-contained portable EXE;
- Inno Setup installer;
- SHA-256 files.

### Distribution
Public Windows binaries will be published in `realmichelduarte/michel-s-life-releases` under a Desktop-specific tag/feed, separate from LouderMe Android.

### Known release boundary
- Equalizer APO 1.4.2 x64 remains a local prerequisite for system-wide boost above 100%.
- Physical Windows/APO audible validation is still pending.
- Windows Authenticode publisher signing is not configured for v0.1.0; published SHA-256 files verify file integrity, but SmartScreen may warn.

### Current status
Stable release candidate prepared; pending PR CI, merge-to-main release build, public transfer verification and final publication.


## 2026-10-06 — LouderMe Desktop v0.1.0 stable release published

### Authorization
Michel explicitly authorized publication of the first LouderMe Desktop stable release.

### Source release
- tag: `desktop-v0.1.0`;
- release type: stable, not prerelease;
- target commit: `dbd775e5fc492a12dbfbb2bbcd931a9bd397a958`;
- release workflow run: `37519498262` — **success**;
- Desktop build workflow run: `37519498307` — **success**;
- Android regression workflow run: `37519498193` — **success**.

Assets:
- `LouderMe-Setup-v0.1.0.exe` — 36,163,803 bytes — SHA-256 `958a4e9545e20b7654b10331683958d9c8cd71ae64a1ef7b1492f2c1225a4a0f`;
- `LouderMe-v0.1.0.exe` — 118,431,673 bytes — SHA-256 `0e1832b282c5daa3c45e828ba5dcdab53dea3326b03a62c80a4422efccf499a8`;
- matching checksum files published.

### Public Michel's Lab Windows channel
Public release:
- repository: `realmichelduarte/michel-s-life-releases`;
- tag: `louderme-desktop-v0.1.0`;
- release type: stable, not prerelease;
- publisher workflow run: `37519908247` — **success**.

Public feed:
- `louderme-desktop/latest.json`;
- channel: `stable`;
- platform: `windows-x64`;
- versionName: `0.1.0`;
- installer SHA-256: `958a4e9545e20b7654b10331683958d9c8cd71ae64a1ef7b1492f2c1225a4a0f`;
- portable SHA-256: `0e1832b282c5daa3c45e828ba5dcdab53dea3326b03a62c80a4422efccf499a8`.

Transfer staging was removed after publication.

### Release contents
- native .NET 10 WinForms app;
- official LouderMe identity;
- Windows endpoint device/volume/mute control;
- opt-in Start LouderMe with Windows;
- real APO-backed Global Boost 100–250%;
- 7-band EQ and presets;
- Inno Setup installer;
- self-contained portable EXE;
- SHA-256 integrity files.

### Known release boundary
- Equalizer APO 1.4.2 x64 is required for system-wide gain above 100%;
- physical Windows endpoint/APO audible validation remains pending;
- Windows Authenticode publisher signing is not configured for v0.1.0, so SmartScreen can warn even when hashes match.

### Current status
**LouderMe Desktop v0.1.0 is fully published on the stable public Windows channel.**

## 2026-10-06 — Intelligent Michel's Lab brand-adoption guidance

Repository instructions now explicitly route logo, launcher, splash/startup and About work through the Michel-Software-Standards Product Identity Standard and Brand Adoption Playbook.

The required interpretation is structural integration rather than sticker placement: replace active legacy identity, adapt canonical geometry to the existing product design language, preserve unrelated behavior, validate the build, and keep release publication separate unless explicitly authorized.



## 2026-10-06 — Complete cross-platform LouderMe brand integration

### Audit trigger
Michel requested a direct audit of what still remained to implement in the real LouderMe product, especially branding.

The audit found that initial official-logo adoption was real but the newer Michel's Lab integrated-branding contract was not yet fully satisfied.

### Key findings
Android already had:
- official launcher/adaptive icon;
- official branded splash;
- official LouderMe command-bar mark;
- official LouderMe About hero.

But Android still had:
- a legacy Michel Duarte portrait instead of the current canonical portrait;
- text-only social rows plus arrow;
- no Facebook row from the canonical developer profile;
- Michel's Lab referenced only as text rather than its official parent-brand mark/lockup;
- mixed tagline language;
- mostly generic Boost/EQ visual feedback rather than waveform-derived product language.

Windows Desktop still had:
- official executable icon but a text-only in-app header;
- no branded startup/splash surface;
- a plain MessageBox About;
- no canonical portrait / Michel's Lab parent-brand asset in About;
- no canonical social hierarchy;
- no in-app stable updater despite the public Desktop feed already existing;
- little waveform-derived visual language inside Boost/EQ.

### Canonical asset reconciliation
Vendored the current master assets from `realmichelduarte/Michel-Software-Standards`:
- canonical Michel Duarte portrait blob: `be4d18572bec28d53783cd4db05cb6cd289a7916`;
- Michel's Lab mark blob: `f205c15c3b6bd7fe676ced83fd1ea2ae24c25586`;
- Michel's Lab lockup blob: `7819ef5c7d1a5c338c67c9bbd517e5448724a5cf`.

The Android portrait previously in use was legacy blob `1594e609850dd1632ede85306bc9d7fc719a3bff` and was replaced.

### Android implementation
- Added local canonical developer-profile constants sourced from the master profile.
- About now uses the canonical Michel Duarte portrait.
- Added official Michel's Lab lockup to the author/studio block.
- Social order is now Instagram → Facebook → LinkedIn → GitHub → Email.
- Social entries use native recognizable glyphs plus visible network names.
- Reconciled About tagline to the official `SOUND THAT LIFTS YOU` identity.
- Product version is visible in the product hero.
- Added canonical-waveform visual feedback to Global Boost.
- Added waveform-derived EQ curve visualization.
- Preserved existing audio/update/startup behavior.

### Windows Desktop implementation
- Added exact canonical-waveform geometry as a native GDI+ control.
- Replaced the text-only top identity with LouderMe waveform + product name + official tagline.
- Added waveform-derived live boost visualization.
- Added EQ curve visualization derived from the same audio/waveform language.
- Added a native branded startup splash.
- Added a dedicated native About experience replacing the MessageBox.
- Desktop About now includes:
  - product-first LouderMe identity;
  - build-derived Desktop version;
  - canonical Michel Duarte portrait;
  - official Michel's Lab lockup;
  - About the Author section;
  - Instagram / Facebook / LinkedIn / GitHub / Email rows with native recognizable glyphs + visible names;
  - privacy/legal copy;
  - Equalizer APO third-party notice;
  - explicit Authenticode/SmartScreen state.
- Embedded the canonical portrait and Michel's Lab brand assets inside the Windows executable.
- Aligned Inno Setup publisher/support/update metadata with Michel's Lab.

### Desktop stable updater
Connected the already-established public Windows stable feed:
`louderme-desktop/latest.json`.

Updater behavior:
1. compare running product version with the public stable manifest;
2. expose installation only for a newer version;
3. download the public installer;
4. verify the manifest SHA-256;
5. only then ask Windows to launch the verified installer.

No silent update execution was added.

### Authenticode boundary
This branding/product pass does **not** claim Authenticode signing.
No Windows publisher certificate currently exists in the repository release path.
The UI/About explicitly preserves the accurate state: SHA-256 integrity is supported; SmartScreen can still warn.

### CI hardening
Android CI now checks:
- canonical portrait Git blob;
- canonical Michel's Lab mark/lockup Git blobs;
- Facebook presence;
- parent-brand lockup presence;
- official tagline;
- waveform Boost/EQ components.

Desktop CI now checks:
- canonical author/studio asset blobs;
- dedicated About surface;
- full social hierarchy;
- branded splash;
- waveform UI control;
- stable updater feed and SHA-256 verification contract.

The Desktop release workflow is also protected by the same branding/updater contract.

### Intermediate Desktop CI failures
Several branch pushes failed while the new native Windows surfaces were being assembled. These were not treated as release evidence.
Observed/fixed issues included:
- About color field colliding with `Form.Text`;
- invalid null-coalescing `Process.Start` statement;
- WinForms designer-serialization diagnostics on runtime-only custom-control properties;
- embedded-image lifetime handling.

A final green current-HEAD run is required before merge.

### Release discipline
- Android remains v0.1.6 / versionCode 7.
- Desktop remains v0.1.0 in source.
- No new release/version bump is authorized by this branding implementation.
- A future release must bump to a new product version and cannot overwrite the already-published Desktop v0.1.0 release.


### Final integrated-branding validation / merge — 2026-10-06
- PR #17 **merged**.
- Merge commit: `ab0b127c3b27ba640724e0abb5125db7195a1d49`.
- Final Desktop PR CI run `37540074990`: **success**.
  - native .NET build passed;
  - canonical asset/About/splash/updater contract passed;
  - portable EXE passed;
  - installer passed;
  - SHA-256 generation passed;
  - artifact upload passed.
- Final Android PR CI run `37540075001`: **success**.
  - canonical branding contract passed;
  - Play + Direct unit/build validation passed;
  - package IDs and permission split passed;
  - validation artifacts uploaded.
- Android intermediate AAPT2 failure was traced to compiling the canonical Michel's Lab PNG from `res/drawable`.
  - Resolution: preserve the exact canonical PNG bytes under `app/src/main/assets/branding/michels-lab/` and decode them natively from Compose.
  - This keeps the master asset unchanged while avoiding AAPT2 raster compilation.
- Desktop release workflow trigger was hardened so editing the release workflow cannot republish an already-released version; stable publication requires an explicit `desktop/releases/vX.Y.Z.md` release note or manual dispatch.
- Merge used `[skip ci]` intentionally because this product/branding change is **not** a release and must not republish Android v0.1.6 or Desktop v0.1.0.

### Current product state after merge
- Android source: integrated LouderMe + Michel's Lab branding complete under the current contract.
- Desktop source: integrated branding, native About/splash, waveform-derived UI and verified stable-feed updater implemented.
- Existing public binaries remain unchanged:
  - Android stable: v0.1.6;
  - Desktop stable: v0.1.0.
- A new Desktop/Android release requires separate explicit authorization and a new version; the already-published Desktop v0.1.0 must not be overwritten.
- Windows Authenticode remains unimplemented because no publisher certificate has been configured.


## 2026-10-06 — Agent contracts reconciled with dual-platform product state

After LouderMe branding/Desktop work landed, the repository-local agent prompts still described LouderMe primarily as Android-only.

Updated the local contract surfaces to current product truth:
- `AGENTS.md` now treats LouderMe as Android + native Windows Desktop;
- App Maintainer covers Compose/Gradle and .NET/APO/Inno Setup/updater work;
- QA Regression covers Windows endpoint/APO/config/installer/feed/SmartScreen boundaries in addition to Android;
- Release Manager covers Android Play/Direct plus Desktop source/public tags, checksums, public feed and Authenticode truthfulness;
- Copilot instructions now preserve both Android and Windows engine/update boundaries;
- release wording is stable-by-default when Michel explicitly says "release" and does not request prerelease/beta/RC.

The canonical Michel's Lab managed contract block was preserved unchanged.

No product code, version metadata, release artifact or public feed was changed by this agent-contract synchronization.


## 2026-10-06 — Android v0.1.7 + Desktop v0.1.1 stable release candidate

### Authorization
Michel explicitly authorized publication of the next LouderMe release after the integrated branding implementation.

### Versions
- Android: `0.1.7` / versionCode `8`.
- Windows Desktop: `0.1.1`.
- Release type: **stable**, not prerelease.

### Included Android changes
- integrated About/product branding;
- current canonical Michel Duarte portrait;
- official Michel's Lab parent-brand lockup;
- canonical social rows including Facebook;
- waveform-derived Boost/EQ visual language;
- opt-in Start with phone;
- existing audio engine, EQ, Play/Direct updater and stable signing preserved.

### Included Desktop changes
- native LouderMe waveform header and branded splash;
- dedicated About surface with canonical author/studio/social hierarchy;
- waveform-derived Boost/EQ visuals;
- stable-feed in-app updater with installer SHA-256 verification;
- existing APO-backed Global Boost, EQ, startup and installer behavior preserved.

### Release gates
Candidate must pass PR validation before merge:
- Android current-commit unit/build validation for Play + Direct;
- Android branding identity contract;
- package ID and permission split;
- Desktop native .NET build;
- Desktop branding/About/splash/updater contract;
- APO boost contract;
- portable EXE + Inno Setup installer + checksums.

### Publication plan
After green PR merge to `main`:
- Android workflow publishes private source tag `v0.1.7` and stable-signed APK.
- Desktop workflow publishes private source tag `desktop-v0.1.1`.
- validated artifacts are transferred to the public Michel's Lab release repo;
- public tags become `louderme-v0.1.7` and `louderme-desktop-v0.1.1`;
- public feeds `louderme/latest.json` and `louderme-desktop/latest.json` are advanced only after checksum verification.

### Known boundaries
- Android audible high-gain/EQ and Play production delivery remain device/store validation items.
- Windows APO endpoint/audible validation remains a physical-machine item.
- Windows Authenticode publisher signing remains unconfigured.


## 2026-10-06 — Android v0.1.7 + Desktop v0.1.1 stable releases published

### Authorization
Michel explicitly authorized publication. Per repository release policy, both releases are **stable normal releases**, not prereleases.

### Release source
- PR #18 merged to `main`.
- Release target commit: `da196b542a3f2e0cd196120006c2d40c40ff6ae5`.
- Android version: `0.1.7` / versionCode `8`.
- Desktop version: `0.1.1`.

### Candidate validation
- Android PR CI run `37544585036`: **success**.
  - branding identity contract passed;
  - Play + Direct unit/build validation passed;
  - package ID and permission separation passed;
  - validation artifacts uploaded.
- Desktop PR CI run `37544585024`: **success**.
  - native .NET build passed;
  - real-global-boost contract passed;
  - portable EXE passed;
  - Inno Setup installer passed;
  - checksums passed.

### Android source release
- Main Android release run `37544958547`: **success**.
- Build and validate: success.
- Stable Michel's Lab Direct signing: success.
- Private source release publication: success.
- Tag: `v0.1.7`.
- Release type: stable / `prerelease=false`.
- APK: `LouderMe-v0.1.7-sideload.apk`.
- APK size: 22,118,266 bytes.
- APK SHA-256: `ec61f53a8e80fa5f9b5cbda944ef4fa27b99a299508425981e24f6ef659d9df9`.

### Android public Direct channel
- Public publisher run `37545535455`: **success**.
- Public tag: `louderme-v0.1.7`.
- Release type: stable / not prerelease.
- Public APK SHA-256: `ec61f53a8e80fa5f9b5cbda944ef4fa27b99a299508425981e24f6ef659d9df9`.
- `louderme/latest.json` now advertises:
  - versionName `0.1.7`;
  - versionCode `8`;
  - the v0.1.7 public APK URL;
  - the matching SHA-256.
- Transfer staging cleanup run `37545641762`: **skipped as intended** by the cleanup guard.

### Desktop source release
- Desktop release workflow run `37544958566`: **success**.
- Independent Windows build run `37544958503`: **success**.
- Tag: `desktop-v0.1.1`.
- Release type: stable / `prerelease=false`.
- Installer: `LouderMe-Setup-v0.1.1.exe`.
  - size: 36,206,793 bytes;
  - SHA-256: `6175aef2f06c8563ccd0273acc9204f376d46112c2ec5008b6012e56d18dcb38`.
- Portable: `LouderMe-v0.1.1.exe`.
  - size: 118,505,401 bytes;
  - SHA-256: `733c1a2b270cc0e3737763eb0a71715edbdc90baca3736f6e27a1fe02196ed53`.

### Desktop public channel
- Public publisher run `37545285435`: **success**.
- Public tag: `louderme-desktop-v0.1.1`.
- Release type: stable / not prerelease.
- Public hashes match the private source release:
  - installer: `6175aef2f06c8563ccd0273acc9204f376d46112c2ec5008b6012e56d18dcb38`;
  - portable: `733c1a2b270cc0e3737763eb0a71715edbdc90baca3736f6e27a1fe02196ed53`.
- `louderme-desktop/latest.json` now advertises stable `0.1.1` with installer + portable URLs and matching SHA-256 values.
- Transfer staging cleanup run `37545431086`: **skipped as intended** by the cleanup guard.

### Delivered product changes
Android v0.1.7:
- complete integrated LouderMe / Michel's Lab branding;
- current canonical Michel Duarte portrait;
- official Michel's Lab lockup;
- complete canonical social hierarchy including Facebook;
- waveform-derived Boost/EQ visuals;
- opt-in Start with phone;
- existing 100–250% boost, EQ, Play/Direct split and updater preserved.

Desktop v0.1.1:
- native LouderMe waveform header and branded splash;
- dedicated About hierarchy;
- canonical author/studio/social identity;
- waveform-derived Boost/EQ visuals;
- stable-channel in-app updater with SHA-256 installer verification;
- existing APO boost, EQ, Start with Windows and installer behavior preserved.

### Known release boundaries
- Android audible high-gain/EQ quality and Google Play production delivery remain device/store validation items.
- Windows APO endpoint attachment/audible behavior remains a physical-machine validation item.
- Windows Authenticode publisher signing is still not configured, so SmartScreen can warn.

### Current status
**Android v0.1.7 and LouderMe Desktop v0.1.1 are fully published on their stable public Michel's Lab channels.**


## 2026-10-06 — LIMÓN handoff after v0.1.7 / Desktop v0.1.1 field feedback

### Production regression — Android About
User reported that the public Android **v0.1.7 / versionCode 8** app closes/crashes when opening About.

Evidence/state:
- issue created: **#19 — Android v0.1.7 crashes when opening About**;
- repository CI for v0.1.7 had passed, so the failure is a runtime/device regression not caught by the current build/static checks;
- root cause is **not yet confirmed**;
- likely investigation surface is the new About/branding runtime path: `LouderMeApp.kt`, `BrandComponents.kt`, canonical Michel's Lab raster asset loading and target-device rendering;
- next session must reproduce on the target Samsung and capture the actual stack trace/logcat before declaring a fix;
- do not overwrite v0.1.7; any production correction requires a new stable Android version.

### Desktop installer experience feedback
User expected LouderMe Desktop to install like FoamLens / Michel's Life and reported that the current public v0.1.1 flow does not communicate that clearly enough.

Audit finding:
- FoamLens and Michel's Life also use GitHub-built Windows releases with Inno Setup installers;
- Michel's Life additionally demonstrates optional Authenticode signing through `WINDOWS_CERT_BASE64` / `WINDOWS_CERT_PASSWORD` and `signtool`;
- LouderMe v0.1.1 does have an Inno Setup installer, but the public release also exposes a generically named portable EXE, making the normal install path ambiguous;
- LouderMe is currently unsigned with Authenticode, so Windows can show **Unknown publisher** before the installer wizard. This cannot be honestly removed without a real publisher certificate.

### Desktop hotfix branch
Branch: `fix/desktop-installer-experience`
Current branch head at handoff: `84b8d5ec740938112796fec25ddca3a639f39493`

Changes already staged on that branch:
- Desktop source version bumped from 0.1.1 to **0.1.2**;
- `desktop/installer/LouderMe.iss` reworked toward the Michel's Lab / FoamLens / Michel's Life install pattern;
- bilingual English/Spanish installer messages added;
- installer-side Start with Windows option retained;
- branded installer wizard-image references added;
- `desktop/installer/prepare-branding.ps1` added to generate ICO + installer wizard BMP assets from canonical LouderMe branding;
- normal Desktop CI updated to generate installer branding, rename the portable artifact toward **LouderMe-Portable-vX.Y.Z.exe**, validate installer contract and smoke-test an actual install/uninstall;
- release workflow partially updated to use the branded installer assets and explicit portable naming.

### Desktop hotfix validation status
**Not validated yet.**
No PR has been opened and no CI result exists for the v0.1.2 installer branch at this handoff.

Before merge/release:
1. finish/review both Desktop workflows for consistent `LouderMe-Portable-vX.Y.Z.exe` naming;
2. ensure public publisher/feed logic also understands the new portable filename;
3. add/update v0.1.2 release notes/changelog;
4. run Windows PR CI;
5. confirm branded installer builds;
6. confirm CI installs `LouderMe.exe` into an isolated directory and uninstalls cleanly;
7. publish only after explicit stable release authorization.

### Shared standards finding
The Michel's Lab master repository already documents:
- Windows installer + portable + SHA-256;
- current-commit validation;
- installer smoke installation/uninstallation;
- public binary release repositories;
- automatic updater expectations.

However, it does **not yet state strongly enough** that the normal Windows production path should be built/published from GitHub Actions, that the installer is the recommended user-facing artifact, and that a portable build should be clearly secondary/named as portable.

A master-standard update was investigated but **not yet committed** at this LIMÓN stop.

### Stop state
Public versions remain:
- Android **v0.1.7** — contains the About crash regression;
- Desktop **v0.1.1** — stable/public, but installer UX/naming needs the v0.1.2 hotfix.

No hotfix release was published in this session after the field reports.


## 2026-10-06 — Desktop v0.1.2 installer hotfix validated in PR #20

### Scope
Continuation of the LIMÓN handoff for the Windows installer experience. No stable release was published by this work.

### Implemented
- Desktop candidate remains **v0.1.2** on `fix/desktop-installer-experience`.
- The normal Windows artifact is the branded Inno Setup installer: `LouderMe-Setup-vX.Y.Z.exe`.
- The standalone secondary artifact is explicitly named `LouderMe-Portable-vX.Y.Z.exe`.
- Installer wizard branding is generated from canonical LouderMe assets.
- English/Spanish installer messages and the optional **Start LouderMe with Windows** task are preserved.
- Normal Windows CI now performs a real silent install, verifies `LouderMe.exe` and `unins000.exe`, and uninstalls the test installation.
- The public Michel's Lab Desktop publisher was aligned to the explicit Portable filename while retaining a legacy-transfer fallback for older artifacts.
- The current public stable feed remains Desktop v0.1.1 until an explicitly authorized v0.1.2 publication.

### Verified evidence
PR: **#20 — Desktop v0.1.2 — installer-first Windows experience**

Head validated: `c687e45d8309e9b631387acdd6da91c222c5ceab`

- Windows PR CI run **37550569236**: **success**.
- Windows same-head push run **37550491583**: **success**; step **Smoke-test real Windows installation** passed.
- Android PR CI run **37550569270**: **success**, confirming the Desktop hotfix did not regress the Android Play/Direct build gates.

### Remaining release boundary
- PR #20 is intentionally left open/unmerged because merging `desktop/releases/v0.1.2.md` to `main` triggers the stable Desktop release workflow.
- Windows Authenticode remains unconfigured; SmartScreen/Unknown publisher can still appear.
- Physical Windows APO endpoint/audible behavior remains a real-machine validation item.

### Separate Android regression
Issue **#19** remains open for the Android v0.1.7 About crash. Source inspection confirms the canonical Michel's Lab lockup and Michel Duarte portrait resources are present; no runtime root cause has been proven without target-device stack-trace/logcat evidence. Do not claim this Android regression fixed yet.


## 2026-10-06 — Stable releases published: Desktop v0.1.2 + Android v0.1.8

### Release decision
Michel explicitly authorized publication after the canonical About portrait correction. The validated PR #20 was merged to `main` at commit `654635407ef3f63a5a00ed04db7e0fa1e92f3a5e`.

### Desktop v0.1.2
- Source release tag: `desktop-v0.1.2` — stable, not prerelease.
- Public release tag: `louderme-desktop-v0.1.2` — stable, not prerelease.
- Recommended installer: `LouderMe-Setup-v0.1.2.exe`.
- Secondary portable build: `LouderMe-Portable-v0.1.2.exe`.
- Both binaries publish matching SHA-256 files.
- Public `louderme-desktop/latest.json` now points to v0.1.2.
- Installer SHA-256: `a042c5471a4b733f8503e1a809b57025cca33f4ebc1e9f16fccac29e1db12d36`.
- Portable SHA-256: `ff4c465f2f48dfd2688ab67fbcd468ee04254ae457f3590f1e9d14b19ec07482`.
- Source release workflow `37555024781`: success, including real install/uninstall smoke test.
- Public publisher workflow `37555336629`: success.

### Android v0.1.8
- versionName: `0.1.8`; versionCode: `9`.
- v0.1.7 was not overwritten.
- Source release tag: `v0.1.8` — stable, not prerelease.
- Public release tag: `louderme-v0.1.8` — stable, not prerelease.
- Public APK: `LouderMe-v0.1.8-sideload.apk` with SHA-256 companion.
- Public `louderme/latest.json` now points to v0.1.8 / code 9.
- APK SHA-256: `6edd30284a6104c03cd3bd2c017f83ab318f01b8c8456bb7c804512d7936cb5f`.
- Source Android workflow `37555024818`: success.
- Public publisher workflow `37555426309`: success.

### Canonical About portrait correction
- Replaced rejected interim portrait blob `be4d18572bec28d53783cd4db05cb6cd289a7916` with canonical blob `18fe1a68722850c3d8f918dc0799f46ffeb6dbaf` in Android and Desktop.
- Canonical JPEG remains byte-for-byte unchanged; only render size was reduced (Android 78×104 dp; Desktop 96×128 px).
- CI now rejects portrait drift on both platforms.

### Android issue #19
The corrupt/rejected portrait was a confirmed About defect and a plausible contributor to the Android crash. Issue #19 remains open until v0.1.8 is opened on the target Samsung and About is confirmed not to crash. Do not claim the runtime crash conclusively fixed until that device validation occurs.

### Distribution state
Both public stable feeds and release assets were verified after publication. Temporary transfer-staging files were removed from the public binary repository after successful publishing.


## 2026-10-06 — Android v0.1.9 system Back hotfix + Desktop v0.1.3 FoamLens installer parity

### Android v0.1.9 / versionCode 10
- User validated on the target Samsung that v0.1.8 About opens normally and the in-app Back control returns to the main screen.
- A separate navigation defect remained: Android system/gesture Back from About exited the app.
- Added Compose `BackHandler(enabled = showAbout)` so system Back from About now performs `showAbout = false`.
- System Back from the LouderMe main screen remains unchanged and exits normally.
- Android CI now explicitly guards the About BackHandler contract.
- PR #21 merged at `e475940758a4b39a1957f2f4ba7f629ce349deed`.
- Source release tag `v0.1.9`: stable, not prerelease.
- Public release tag `louderme-v0.1.9`: stable, not prerelease.
- Public feed `louderme/latest.json` points to v0.1.9 / versionCode 10.
- Public APK SHA-256: `1c368d5c4c76d3622f1da132e335243cfa70e6ab8d4583c3b1f457ae8b328e64`.
- Source workflow `37556701800`: success.
- Public publisher workflow `37557109373`: success.
- Original issue #19 (v0.1.7 About closes/crashes on open) was closed after target-device confirmation that About now opens normally. The asset defect was confirmed, but the exact sole runtime cause was not asserted.

### Desktop v0.1.3
- User screenshot showed v0.1.2 Setup did not visually match the FoamLens installer family because LouderMe added custom `WizardImageFile` and `WizardSmallImageFile` branding.
- Compared directly with FoamLens `desktop/installer/FoamLens.iss`, which uses the clean modern Inno Setup wizard without custom wizard-banner images.
- Removed LouderMe's custom side/header wizard imagery while preserving:
  - `WizardStyle=modern`;
  - LouderMe icon, product name/version and Michel's Lab publisher identity;
  - English/Spanish installer text;
  - optional desktop shortcut;
  - optional Start LouderMe with Windows.
- Simplified installer branding generation to the official Windows ICO only.
- Windows CI now fails if `WizardImageFile` or `WizardSmallImageFile` is reintroduced.
- PR #22 was rebased cleanly after the Android v0.1.9 merge, then merged at `67a6477798777a1d56dd039ee469db0b7b1cc32c`.
- Source release tag `desktop-v0.1.3`: stable, not prerelease.
- Public release tag `louderme-desktop-v0.1.3`: stable, not prerelease.
- Recommended installer: `LouderMe-Setup-v0.1.3.exe`.
- Secondary portable: `LouderMe-Portable-v0.1.3.exe`.
- Public `louderme-desktop/latest.json` points to v0.1.3.
- Installer SHA-256: `00f1a2a0ed289d47a4c36cfc09412ef6e721fd7c197c9201e60a635616acac39`.
- Portable SHA-256: `b5cec6f3ba79687242becff4d7b81683f79e6e413c931dbc4612ccb25765167c`.
- Source release workflow `37557224444`: success, including real install/uninstall smoke test.
- Public publisher workflow `37557459967`: success.

### Distribution state
- Android v0.1.9 and Desktop v0.1.3 are both available in the public binary repository.
- Both public update feeds point to the new stable versions.
- Temporary transfer-staging files were removed after publication.


## 2026-10-06 — Desktop v0.1.3 installed-startup regression reproduced and fixed in v0.1.4 candidate

### Field report
- User installed public Desktop v0.1.3 successfully, but the application did not open afterward.

### CI gap discovered
- The prior Windows installer smoke test verified installation and uninstallation only.
- It did **not** launch the installed `LouderMe.exe`.
- This differed from FoamLens, whose Windows CI validates the packaged/installed executable at runtime.

### Reproduction
Branch: `fix/desktop-installed-startup`

Initial diagnostic CI run: **37558473562**
- installer build: success;
- install: success;
- installed EXE launch: **failure**;
- process exit code: `-532462766` / `0xE0434352` (.NET unhandled exception).

A temporary startup diagnostic layer was then added so the installed smoke test could print the real exception.

Diagnostic CI run: **37558716952**

Confirmed stack trace:
```
System.ArgumentException: Control does not support transparent background colors.
   at System.Windows.Forms.Control.set_BackColor(Color value)
   at LouderMeDesktop.WaveformMarkControl..ctor()
   at LouderMeDesktop.SplashForm..ctor()
   at LouderMeDesktop.Program.Main(String[] args)
```

### Root cause
Custom WinForms controls were assigning `BackColor = Color.Transparent` without first enabling `ControlStyles.SupportsTransparentBackColor`.

The unsafe pattern existed in four controls:
- `WaveformMarkControl`;
- `AudioPulseControl`;
- `EqCurveControl`;
- `SocialGlyphControl`.

### Fix
- All four controls now enable `SupportsTransparentBackColor` before assigning transparent backgrounds.
- Startup exceptions are logged to:
  `%LOCALAPPDATA%\Michel's Lab\LouderMe\startup-crash.log`
- Production startup failures show the diagnostic path instead of silently terminating.
- Windows build CI now performs: install → launch installed EXE → require it to stay alive → terminate → uninstall.
- Stable release CI now carries the same installed-runtime launch gate.
- CI statically guards that all four transparent custom controls keep the required WinForms style.

### v0.1.4 candidate validation
PR: **#24 — Desktop v0.1.4 — fix installed startup crash**

Validated head: `14d2482e898a1cd66014eb1659c3ff9334b4fa54`

- Windows PR CI run **37559243120**: **success**, including installed executable startup.
- Android PR CI run **37559243046**: **success**.
- Same-head Android push CI run **37559235544**: **success**.
- PR #24 state: mergeable / clean.

### Release boundary
Desktop v0.1.4 is prepared and validated but **not yet published** in this entry. Merge/publication still requires explicit stable release authorization.


## 2026-10-06 — Desktop v0.1.4 published + unified Android/Windows public release

### Desktop v0.1.4 publication
- PR #24 merged at `1ab6e9f786c28be2657dc89af27aef9396123eea`.
- Source release workflow `37560800602`: **success**.
- Installed-runtime gate passed again in release CI: install → launch installed LouderMe → remain alive → terminate → uninstall.
- Source release tag `desktop-v0.1.4`: stable, not prerelease.
- Source assets:
  - `LouderMe-Setup-v0.1.4.exe`
  - `LouderMe-Portable-v0.1.4.exe`
  - matching SHA-256 files.

### Unified public release migration
The user requested that Android and Windows downloads appear together on one public release page, matching the distribution style used by other Michel's Lab apps.

Public release workflows were changed so either platform publisher:
- reads the current stable metadata for the other platform;
- downloads and checksum-verifies the unchanged platform's current binaries;
- publishes both platforms into one unified GitHub release;
- points both platform-specific update feeds to that same unified release;
- serializes Android/Windows publication through one shared concurrency group.

Current unified public release:
- tag: `louderme-android-v0.1.9-desktop-v0.1.4`
- title: `LouderMe — Android v0.1.9 + Windows v0.1.4`
- stable / not prerelease / GitHub Latest release.

Assets in the same release:
- `LouderMe-v0.1.9-sideload.apk`
- `LouderMe-v0.1.9-sideload.apk.sha256`
- `LouderMe-Setup-v0.1.4.exe`
- `LouderMe-Setup-v0.1.4.exe.sha256`
- `LouderMe-Portable-v0.1.4.exe`
- `LouderMe-Portable-v0.1.4.exe.sha256`

Verified public feeds:
- Android `louderme/latest.json`: v0.1.9 / versionCode 10, APK URL points to the unified release.
- Desktop `louderme-desktop/latest.json`: v0.1.4, installer/portable URLs point to the same unified release.
- Android APK SHA-256: `1c368d5c4c76d3622f1da132e335243cfa70e6ab8d4583c3b1f457ae8b328e64`.
- Windows Setup SHA-256: `29d4813b9ca7cc8dd83ae13e1d86140ce1a5cbd957ec7e1ae49aceb633c52e01`.
- Windows Portable SHA-256: `1a39efca925444960dc08d847e8209a1fb4bd861d6573f747e7761841585d646`.

Publisher validation:
- Windows-driven unified publisher `37561008337`: **success**.
- Android-driven unified publisher `37561124781`: **success**.
- Both directions were tested to prove that future updates from either platform keep one complete release bundle.
- Temporary transfer staging files were removed after validation.

### Standards
The reusable rule was added to Michel-Software-Standards `standards/REPOSITORY_DISTRIBUTION_STANDARD.md`: multi-platform products should expose one unified customer-facing public release containing all current stable platform binaries while preserving real platform versions and separate updater feeds when needed.

Historical platform-specific releases were intentionally retained for compatibility.


## 2026-10-07 — Repository transferred to Michel's Lab organization

**Change:** repository ownership moved from `realmichelduarte/LouderMe` to `michels-lab/LouderMe`.

**Active references updated:** `.michelslab` governance and agent/Copilot authority now use `michels-lab`; Android Direct and Windows Desktop update manifests now resolve from `michels-lab/michel-s-life-releases` instead of the old-owner redirect.

**Preserved intentionally:** Michel Duarte personal developer/social identity remains under `realmichelduarte`. No release is authorized by this migration change.

**Validation:** Android and Windows branch CI are required before merge.

## 2026-10-08 — Device-volume/identity continuation (partial)

- Closed superseded PR #11 and resolved Desktop startup issue #23 after published v0.1.4 evidence.
- Corrected README direct-update manifest ownership to michels-lab.
- Added Michel's Lab 'Tools with identity' subtitle in Windows About; LouderMe tagline remains 'SOUND THAT LIFTS YOU'.
- Added Windows polling of active endpoint master volume and mute; this is source-level work, not yet runtime-validated. Device volume remains 0–100%, independent from Global Boost 100–250%.
- Android About + media-volume implementation remain pending; attempted Android source updates were blocked, so no Android result is claimed.
- Windows build, installed startup test, real device test and current-SHA CI are pending. No release has been authorized or published.

## 2026-10-08 — Android device volume and About implementation

- After repository visibility changed to public, successfully added 'Tools with identity' under the Michel's Lab lockup in Android About.
- Android MainActivity now reads and sets AudioManager.STREAM_MUSIC (true media stream), normalizes display to 0–100%, and resyncs once per second while the Activity is started. Foreground callbacks are removed on stop.
- Android Compose now has a separate Device Volume slider panel above Global Boost; its changes request Android media stream volume independently from gain.
- Pull request #31 opened with both platform implementations and documentation.
- Android and Windows current-head CI/build status and real-device tests were not confirmed at time of this entry. Do not claim the volume feature functionally validated or close #26 until validation and merge.

## 2026-10-08 — Remove unconditional human-review publication blocker

- User explicitly requested that visual inspection take place after publication rather than requiring manual human approval to start the release.
- Removed the unconditional `throw` step from the Windows Desktop release workflow.
- Existing build, checksum, real installer installation/launch and uninstall smoke checks are retained unchanged.
- This change does **not** establish rendered UI PASS or human visual acceptance. Post-release visual review remains outstanding and any defect found requires a follow-up fix.
- No release was published in this change.

## 2026-10-08 — Stable release preparation: Android v0.1.10 / Windows v0.1.5

- User explicitly authorized release. Version changes: Android versionName 0.1.10 / versionCode 11; Desktop 0.1.5.
- Includes real-device volume controls (Android media stream and Windows playback endpoint) distinct from 100–250% Global Boost, plus Michel's Lab studio About slogan.
- Source repository visibility is public. Removed Android workflow fallback that retrieved production signing keys/passwords from GitHub Actions artifacts, and removed private signing-vault artifact publication. Stable sideload signing now requires valid GitHub Actions Secrets; fail closed if absent. Existing public artifacts and old credentials should be reviewed for exposure; do not assume a key is safe merely because the new workflow is hardened.
- Release notes prepared for both platforms. Same-SHA CI and published artifacts must be rechecked before claiming the release completed. Physical device behavior remains pending.

## 2026-10-08 — Windows compact-window regression and persistent About fix

- User screenshot from Windows v0.1.5 at a small working-area height showed the main window clipped below the Boost card, no working mouse-wheel scroll and About unreachable because it was in the footer.
- Reworked MainForm with monitor-clamped initial bounds, a fixed shell header with always-visible About at top-right and a separate vertically scrollable content Panel; mouse wheel is forwarded to that viewport even if a nested control has focus. Removed footer-only About entry.
- Made AboutForm initial size fit the monitor and responsive card, artwork and social-entry widths.
- Replaced the oversized APO missing-dependency text with a shorter actionable message and explicit distinction between independent Windows device volume and third-party Equalizer APO-dependent Boost/EQ. Equalizer APO remains a user-installed prerequisite; do not claim the boost works without it.
- Added static CI safeguards for top About and scrolling in both Desktop build and release workflows, and added local agent requirements.
- Validation: pending same-SHA CI Windows build, installation/launch smoke, and user's real-window follow-up. No new Windows release published by these source changes.

## 2026-10-08 — Stop existing Android releases being silently replaced

- Desktop-only commits to main still trigger the Android CI & Direct Release workflow. Existing source workflow previously re-signed and uploaded the published APK with `--clobber` even when `versionName` was unchanged, risking mismatch with stable updater SHA-256.
- Added `release_guard` to check whether the stable Android tag already exists. If so, signing and publication jobs skip while build validation continues. The publisher also refuses to overwrite an existing tag as a second safeguard.
- Future Android releases require a unique version and validated continuity of the original signing identity. No new APK version or release authorized by this guard.

## 2026-10-08 — Restore canonical Android APK after stale workflow clobber

- A pre-guard Android main run replaced source-release v0.1.10 APK with a new same-version build (source release SHA-256 diverged from the already-published and validated unified release).
- The unified public asset retained canonical SHA-256 `de092ebbc9516395a588f22092075ac7293d291a0f49d3655773b5c6bba1be38`.
- Added a one-time CI workflow to fetch the canonical public APK/checksum, verify pinned SHA-256, restore identical files to the LouderMe source GitHub Release, then recheck source asset digest. It does not build or sign a new APK and does not modify the unified release.
- Previous Android release guard PR #43 prevents future same-version overwrites; public feeds remain unchanged.

## 2026-10-08 — Michel's Lab own native Audio Engine milestone 1

- Product direction confirmed by owner: native gain/EQ DSP must be a LouderME competitive capability, not depend on user installing Equalizer APO.
- Started portable C++17 native audio DSP in `native/audio-engine`: real normalized float PCM processing, 100–250% smooth gain, seven peaking filters and bounded soft limiter. Added synthetic PCM tests and Windows/Linux CMake CI.
- Architecture decision recorded at `docs/NATIVE_AUDIO_ENGINE_ARCHITECTURE.md`: Windows custom APO vs signed virtual endpoint (with deployment limits); Android session/capture restrictions; no implication that a regular app can modify every other application's audio.
- **Current shipped Desktop v0.1.5 / source current Desktop v0.1.5 still uses user-installed Equalizer APO for actual system-wide output processing.** This standalone DSP does not alter system audio and is not a release-ready replacement.
- Next: verify CI, measure DSP performance and signal fidelity, implement and test Windows OS audio routing/integration, hook control plane, certify/sign necessary components, then switch user-facing engine status only after actual hardware verification.
- No release or installer-driver change performed in this milestone.

## 2026-10-08 — Windows audio bridge prototype / own DSP C ABI (M2 partial)

- Developed a versioned native C ABI (`native/audio-engine/include/louderme/AudioEngineC.h` and `AudioEngineC.cpp`) over Michel's Lab-owned C++ PCM DSP; functions for create/destroy, boost, EQ and in-place processing. Includes ABI test on Windows/Linux.
- Added an isolated managed .NET 10 Windows WASAPI host prototype (`native/windows-bridge`) using NAudio: captures shared-mode loopback **only from an explicitly selected LouderME Virtual render endpoint**, processes PCM in the native DSP library, then sends samples to an explicitly selected independent physical output. It never changes Windows default audio device, creates a driver or performs unannounced capture.
- Guarded against source=destination and capturing an ordinary physical source; only uses float32 PCM when virtual and real endpoints' mix sample rate/channel format match; errors instead of silently resampling. Drops overflow packets and tracks processed/dropped counts. No recording or network.
- CI extended to build C native ABI shared library/test on Windows and Linux plus .NET bridge build and native DLL interop check on Windows; real virtual audio driver and physical-device routing tests remain **NOT IMPLEMENTED/NOT VERIFIED**.
- Production Windows still uses Equalizer APO until the own signed virtual endpoint, OS routing, robust audio bridge, driver packaging and measurements exist. This prototype does not replace the system-wide engine, and no release was made.

## 2026-10-08 — M2 real Windows virtual endpoint source and WDK lab prototype

- User authorized continuation of native system audio integration. Investigated official Microsoft SysVAD sample (MS-PL, upstream revision 2dc3fd3a0cc84a2933f2194e7ec0871584979071).
- Added `native/virtual-driver/prepare_sysvad.py`: reproducible, version-pinned source derivation creating **one render-only speaker endpoint, zero microphone endpoints**, isolated `Root\LouderMe_VirtualRender_Lab` hardware ID, Michel's Lab speaker/device/service labeling, loopback-support configuration and preserved MS-PL license. No driver installation or shipping.
- Added source guard tests, GitHub Actions source integration and experimental WDK x64 build workflow, plus explicit lab bring-up/readiness checklist.
- **No signed virtual driver, INF-validated installed endpoint, non-silent WASAPI loopback, end-to-end audible DSP or physical-device evidence claimed.** Current released app remains on legacy Equalizer APO.
- Next: resolve any pinned source derivation/WDK CI build failures; test signed lab deployment on isolated Windows hardware and prove real PCM capture/output once. Integrate actual endpoint only after evidence, then add robust routing/install/uninstall and user opt-in.

## 2026-10-08 — Driver CI compile/test boundary

- Real pinned SysVAD render-only source derivation passed Ubuntu CI; disabled vendor sample audio file recording and internal synthetic tones; C++/Windows bridge CI passed.
- Windows WDK x64 miniport project compiled and linked `TabletAudioSample.sys` in run 37831513502, but post-link `ApiValidator.exe` failed with MSB3721. Hosted runner also lacks `StampInf.exe` for INF stamping. Neither validation is waived for distributing a kernel driver.
- Separated **compile-only lab** acceptance of a linked SYS from a SPECIFIC recorded post-link `ApiValidator` MSB3721 error. This **does not bypass** the required Universal API/INF validation for packaging; it is NOT a production package and any automatic WDK test signature cannot be shipped. Next evidence: green compile-only test, then full API validation/INF verification and approved signing in a dedicated Windows driver lab, installed virtual render PCM loopback test, physical output DSP route.
- No driver installed, no default output changed and no public release authorized.

## 2026-10-09 — WDK CI lab compilation gate repaired, independent API analysis and lab package checks

- Confirmed PR #50 kernel lab CI run 38002614365 **success**: pinned SysVAD source derivation + C++ WaveRT/driver compilation; the `TabletAudioSample.sys` exists and a *specific* post-link Universal API validation failure is recorded independently. Fixed GitHub PowerShell's implicit native-process exit-code propagation; do not confuse successful kernel linking with Windows driver certification.
- Added explicit standalone `ApiValidator` diagnostic against x64 WDK XML and required executable dependencies to identify actual API or toolchain faults without silently certifying the driver. Follow-up CI will determine whether this diagnostic passes; failure is still a driver-release blocker.
- Added `native/virtual-driver/validate_lab_package.ps1`, a **fail-closed** full-WDK lab validation helper using Microsoft StampInf, InfVerif /w and Inf2Cat for an unsigned test package. It requires a properly provisioned, matching SDK/WDK lab and has **not** yet been executed; signed distribution and device installation remain prohibited.
- Stability boundaries: no modifications to the existing released Windows or Android binaries, no driver installed or default playback device changed, no audible virtual-to-physical proof. Issue #48 remains open.

## 2026-10-09 — Correct Windows Desktop driver target (SysVAD M2)

- Identified root product mismatch: Microsoft's reference SysVAD `TabletAudioSample.vcxproj` targeted the **Universal** driver model, whereas LouderME is a Windows desktop application. Official Microsoft `Windows Driver Types` guidance supports an explicit **Desktop** target with different qualification conditions.
- Added a strict source derivation transformation for the four project configurations to `<DriverTargetPlatform>Desktop</DriverTargetPlatform>`, captured in the machine-readable manifest and regression-tested in CI.
- Changed the full-WDK lab INF validation helper to run `InfVerif /h` (Desktop/WHQL eligibility rules) instead of treating Universal-driver API checks as automatically required for the desktop-only product. This does **not** waive platform-specific driver signing, package compliance, runtime tests or privileges.
- **Awaiting CI** to confirm whether the desktop-target kernel build runs without the former post-link Universal `ApiValidator` error; WDK full INF/CAT toolchain and actual installed loopback/acoustic tests remain unverified, and no driver/release has shipped.

## 2026-10-11 — Cross-app rendered UI gate candidate

The master audit identified missing automated visual evidence checks in LouderMe. This branch adds the canonical validator, real Android installed-emulator Home/About screenshots, real running WinForms Home/About screenshots, artifact/source hashing, and fail-closed Android/Windows release workflow checks. Android captured APK is a debug build, not production-signed or a physical-device pass. Windows captures come from an installed executable rendered by WinForms, not physical-user acceptance. CI and merge status remain to be verified; no version bump or release. The separate pending v0.2.0 device-volume PR remains untouched.

## 2026-10-11 — M2 virtual render lab KMDF INF qualification follow-up

- Run [38126085761](https://github.com/michels-lab/LouderMe/actions/runs/38126085761) against PR #51 commit `e258f1db7b828205263df30ca9aec6e4e25e9c1b`: source/guard derivation, linked x64 kernel SYS and full Windows WDK provisioning passed. Required `InfVerif /h` failed with **ERROR(1081): KmdfLibraryVersion invalid value "$KMDFVERSION$"** (line 123); `Inf2Cat` did not execute. The optional standalone Universal API diagnostic also failed on missing dependencies and does not qualify or invalidate the separate Desktop target.
- Root cause: the lab packaging helper called `StampInf` without its `-k` KMDF version argument. Microsoft documents `-k` as the expansion mechanism for this exact variable; the pinned INF targets Windows 11 build 22621 or later, compatible with KMDF 1.33.
- On PR #51 branch added `StampInf -k 1.33`, explicit checks for the unresolved marker and the expected stamped value, preserving strict `InfVerif` and `Inf2Cat` failure gates. Follow-up commits `1b37b9e8ee01ac2fe9bddadc67faf08706318ba1` and `d6fedab772b4c9c322a73add63b1e5ae9fee54c0`. This source change alone is **NOT** a validation pass.
- Still pending: new exact-HEAD Windows package CI result, actual INF/CAT signability, Microsoft-trusted consumer driver signing, isolated Windows VM installation/uninstallation and non-silent virtual endpoint PCM, native DSP audible routing, crash/rollback, format/latency/peripheral tests. Production Windows remains Equalizer APO-backed. No release, signed lab driver publication, physical driver installation, default audio device modification or integration into `main` authorized by this step.

## 2026-10-11 — WDK INF/CAT automated qualification: PASS

- Run [38126776635](https://github.com/michels-lab/LouderMe/actions/runs/38126776635) passed source derivation, kernel compile, actual StampInf, InfVerif /h and Inf2Cat package validation at commit `d6fedab772b4c9c322a73add63b1e5ae9fee54c0`; Android CI run [38126776661](https://github.com/michels-lab/LouderMe/actions/runs/38126776661) passed on the same commit.
- This is an **unsigned lab-package** qualification, not a Microsoft-signed production driver. A Windows lab VM with driver installation/uninstallation, actual non-silent virtual PCM loopback, audible physical endpoint DSP, reboot/hotplug and rollback testing remains mandatory before enabling or distributing native global audio. Existing shipped Windows app still depends on Equalizer APO.
- PR #51 branch merged the current `main` history in its resolution of the PROJECT_LOG overlap, keeping newer Android onboarding and cross-app rendered UI gates. Revalidate the merge commit. No stable release authorized by this change.

## 2026-10-11 — Windows M2 experimental PCM format and sample-rate bridge

- **Scope:** prior bridged virtual→physical WASAPI audio only when both endpoint mix formats were identical float32, with matching rate and channel count. This blocked ordinary PCM16 paths and physical devices with a different mix rate.
- **Code:** `native/windows-bridge/PcmFormatAdapter.cs` now decodes PCM16/IEEE-float32 input, feeds a private float32 copy to the Michel's Lab native DSP, performs stateful linear interpolation with carry across WASAPI callback boundaries, and encodes PCM16/float32 output at the physical mix rate. Channel count must remain equal and 1–8; rates 8–192 kHz, ratios 1:4–4:1. Callback failures are latched and handled by the console control loop, not thrown through NAudio's callback; overflow remains counted and dropped.
- **CI coverage added:** `--check-formats` runs device-free numeric PCM16 conversion, saturation, 44.1/48/96 kHz streaming/rate transitions and mismatched-channel fail-closed checks. `--check-pipeline` additionally exercises a synthetic 44.1 kHz stereo PCM16 signal → actual native C++ 150% boost → streaming 48 kHz float32 output, checking RMS and frame count. `native-audio-engine-ci.yml` runs these after C++ engine ABI verification on Windows. Exact-HEAD CI result is **PENDING** at edit time; do not treat source implementation as proven until green.
- **Limitations:** linear interpolation is not band-limited or production anti-aliasing; dynamic clock drift, underruns/clicks, Bluetooth/HDMI/hotplug, channel-layout mapping, soft UI live controls, device switch/restore and actual human-audible routed PCM remain unverified. The WDK lab INF/CAT pass is not driver installation/signing proof. Do not switch default Windows playback automatically or remove existing Equalizer APO-based stable fallback. **No release or merge from this log entry.**

## 2026-10-11 — Android rendered UI CI failure during Windows M2 PR validation

- **Actual failure:** exact-head Android CI [run 38129851874](https://github.com/michels-lab/LouderMe/actions/runs/38129851874) built all APK flavors successfully, but the installed emulator screenshot gate failed with `RuntimeError: Installed LouderMe Home has no labeled About action`. No Android runtime feature code changed by the Windows PCM adapter.
- **Root-cause investigation:** the original capture helper did only a fixed 3-second pause after cold Android activity start and demanded a uiautomator node's `text == "About"`, ignoring real `content-desc` accessibility labels. This can mistake startup delays or Compose semantics for absent UI. The source UI `CommandBar` still renders a visible `OutlinedButton` with `Text("About")`; CI must verify **runtime** evidence rather than assume.
- **Follow-up code:** changed native capture to poll bounded actual uiautomator hierarchies (up to 24 seconds) checking visible `text` or `content-desc`, failing closed with observed native labels and a real diagnostic screenshot rather than fabricating success. Added `python -m py_compile` for capture/validator scripts before emulator. Retest current PR exact HEAD, and if the action is genuinely hidden or inaccessible, fix Android UI rather than weaken the gate. Validation pending after change.

## 2026-10-11 — Android emulator uiautomator malformed XML during CI

- **Failed exact-head run:** [38130274928](https://github.com/michels-lab/LouderMe/actions/runs/38130274928) passed native Windows DSP CI and Android APK building, then visual QA failed with `xml.etree.ElementTree.ParseError: syntax error: line 1, column 0` while reading `uiautomator` hierarchy. Android emulator cold launch can produce an empty/incomplete XML dump; the previous bounded waiter only caught `RuntimeError` and incorrectly aborted on `ET.ParseError`. This is **NOT** evidence that About is correct or incorrect.
- **Action:** classify empty/non-XML/truncated native hierarchy as a bounded retriable `RuntimeError` with a short diagnostic prefix. Retain strict 24-second About/Back assertion and real emulator screenshots; no fake UI pass, no skipped gates. Exact-current-head Android visual test remains pending.

## 2026-10-11 — Explicitly authorized coordinated LouderME v0.1.11 release preparation

- User authorized a release after M2 source-only PR #54 merged to main. Create coordinated version **0.1.11** for both Android (`versionCode=12`) and Windows (assembly/file/product version), with exact versioned release notes. This is a maintenance/source-integration release, **not** a claim that experimental native global processing is production-ready.
- Preserve the released Windows Equalizer APO backend and exclude the unsigned lab virtual driver and user-mode native bridge from installer payloads. Android session-zero compatibility effects and Play/Direct permissions unchanged.
- Required gates: release-branch same-SHA Android Play+Direct builds, installed emulator rendered Home/About, Windows WinForms installed-launch/About/scroll/uninstall test, published SHA-256, Android signed APK matching previously installed certificate. Source repository Android and Windows releases then unified public binaries and matching verified stable feeds.
- **State at preparation time: NOT RELEASED.** GitHub issue #40 records that permanent Android signing Secrets previously failed validation. CI must either find the correct stable signer already configured or fail closed; never use a generated signing key, expose historical private vault material, fake release success or publish an Android APK that breaks updates. No physical native driver installation or audio loopback proof.

## 2026-10-11 — Android v0.1.11 owner-authorized permanent signer migration

- **Owner decision:** only the owner currently uses LouderME Android; owner explicitly authorized adopting the newly supplied Android signing key as the permanent identity from v0.1.11 onward, accepting uninstall/reinstall of existing v0.1.10-or-older APKs. In-place signature-incompatible updates are impossible, and local data may be removed.
- **Evidence:** private ZIP contains an internally consistent JKS/private key and Base64; local `keytool` successfully opened the intended alias. Public certificate SHA-256: `4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3`. Existing Android v0.1.10 release used `e0d497f4872c116632f040e51e08b7beb410f0a3df4d2b959d22fbd7bde48479`. Certificates **differ by design**. No secret values/keystore files are to be committed, quoted or uploaded as CI artifacts.
- **Workflow change:** delete obsolete private Actions signing-vault recovery and old v0.1.9 certificate continuity requirement. Require existing four GitHub Actions repository secrets; decode JKS privately; check alias/password and pin the newly approved public certificate before signing, then independently verify the signed APK's certificate and checksum before release. No signing secrets displayed. Remove transient JKS file from runner.
- **GitHub configuration blocker:** repository-secret values are not readable or writable by the connected GitHub tools. User must replace the four existing repository secrets using `keystore.base64.txt` and the three matching assignments in `github-secret-names.txt` directly in GitHub Settings (check exact `LOUDERME_SIDELOAD_KEY_ALIAS` spelling; screenshots may show a typo). Do not upload the private ZIP itself.
- **State:** Android v0.1.11 remains unpublished until the owner has configured new Secrets and a main-branch CI run signs/publishes the actual APK. Desktop v0.1.11 is already available. Public unified release currently contains signed Android v0.1.10 and Desktop v0.1.11, and must stay honest until Android v0.1.11 is published.

## 2026-10-11 — M2 LAB virtual loopback 440 Hz stimulus-identity evidence

- **Observed gap:** the previous `--probe-virtual` passed if any WASAPI loopback PCM exceeded a non-silent RMS/peak threshold. That could misclassify unrelated media, DC offset or other audio as proof that the rendered virtual device returned **the specific tone sent to it**.
- **Change:** add `native/windows-bridge/LoopbackToneVerifier.cs` streaming, phase-agnostic matched 440 Hz analysis using per-frame sine/cosine projections, RMS, peak and coherence over every callback, preserving sample offset across packet boundaries. The existing lab virtual-only device-name guard and explicit opt-in remain mandatory. A failed signal identity, nonfinite PCM or callback exception fails closed. No new hardware permissions, network, recordings to disk or default output changes.
- **New CI:** `--check-loopback` exercises positive 440 Hz across fragmented streaming float32 44.1/48/96k stereo, and rejects 880 Hz, DC, silence, short and malformed/NaN buffers. Wired into Windows `native-audio-engine-ci.yml`. **Test result pending** until current-HEAD Windows CI passes. The driver must still be installed and exercised in a dedicated signed/test-signed Windows lab before any claim about real virtual PCM.
- **Truth boundary:** phase-coherent 440 Hz from a test stimulus verifies only LAB virtual-device loopback in an isolated environment. It does not prove OS app-origin audio interception, physical render DSP gain, double-playback avoidance, signing/certification, hotplug, clock drift/latency, crash recovery or actual user-device functionality. Stable Windows v0.1.11 remains Equalizer APO-backed and unchanged; **no release from this milestone**.
 
## 2026-10-11 — M2 physical-render WASAPI loopback instrumentation (test-only)

- **Starting point:** PR #58 merged to `main` at `d0a63d815c4639ce5b2bc713fee0aaff45657736` after Linux/Windows DSP CI, Android emulator UI and WDK SYS + official INF/CAT checks passed. None of these jobs installed a virtual driver or used a physical speaker.
- **Implementation in this branch:** `--probe-route` extends the guarded native C++ DSP Windows bridge. It only runs with explicitly selected LouderME virtual source and a distinct active physical render endpoint, `--confirm-experimental`, and safe LAB boost 100–150%. It generates a bounded, quiet 440 Hz 2-second signal on the virtual endpoint, monitors 4 seconds of the *physical* WASAPI loopback, and uses the existing phase-independent 440 Hz coherence verifier on the physical PCM. Empty/corrupt/unrelated/missing physical signal or insufficient virtual capture returns nonzero. No default output change, driver installation, disk recording or network.
- **Test added:** `PhysicalRouteTone.RunSelfTests` / `--check-route-tone` checks PCM16 and IEEE float32 mono/stereo at 44.1/48/96 kHz; numerical output is checked for 440 Hz identity across irregular callbacks in Windows CI. **No real hardware/VM evidence exists yet**. Do not confuse device-free numeric PASS with an installed physical route.
- **Blocked external acceptance:** requires a disposable Windows lab VM with audio endpoint, permitted test signing and installed SysVAD-derived virtual driver. Device installation, non-silent virtual loopback, routed physical output, 100 vs 150 amplification proof, timestamp/latency, test signing + rollback and actual app-origin streams must still be verified in that environment. A GitHub-hosted build worker does not provide physical speaker evidence. Stable releases remain v0.1.11 (legacy APO on Windows); **no release requested**.
 
## 2026-10-11 — Explicitly gated disposable Windows VM driver/route test runner

- **Added:** `native/virtual-driver/run_disposable_lab.ps1` safely automates a *future* live test on a disposable Windows VM: admin and virtual-machine preflight; manually provided (and VM-trusted) signed INF/CAT/SYS; exact LAB hardware ID; WDK devcon root-device installation; healthy device enumeration; physical endpoint selection; `--probe-virtual` and `--probe-route --boost 150`; console-only metrics; PnP device removal in `finally` and explicit snapshot recovery warning.
- **Safety:** mandatory exact `-LabConsent` value, signed CAT `Get-AuthenticodeSignature`, refuses ordinary physical PCs or preexisting LAB devices, never disables security boot/test-sign policies, changes Windows defaults or installs unverified unsigned drivers. A real disposable snapshot is **operator-prepared**, not detectably attested by the script. A real physical audio device is still required.
- **CI scope:** Windows CI parses PowerShell syntax only and executes the device-free PCM stimulus tests. CI **does not** install kernel drivers or prove audio hearing/routing. Documented exact commands for lab operator.
- **Status:** script implementation and CI validation pending at edit time; no VM available to run it here. User-facing hardware proof **blocked**, not completed, and stable Windows v0.1.11 unchanged.
 
## 2026-10-11 — Manual-only isolated Windows audio VM CI entrypoint

- **New separate opt-in workflow:** `.github/workflows/native-physical-lab-manual.yml` accepts explicit disposable-lab consent, local signed package, devcon/bridge paths and the real physical render endpoint ID. Runs only when an explicitly labelled `[self-hosted, Windows, X64, louderme-isolated-audio-lab]` snapshot VM exists, never on GitHub's hosted runner, and never on push/PR. Uploads only console/metadata evidence for seven days, not PCM/raw audio.
- **Safety/availability truth:** no such machine/runner is currently attached or demonstrated; this workflow is a future controlled execution route, **not** a performed lab test. No output-device defaults are changed by our code, but Windows may choose new defaults on driver installation; VM snapshot rollback remains mandatory.
- **Current validation boundary:** documentation and workflow creation do not fulfill kernel driver test signing, reboot/installation, virtual loopback, physical audible or acoustic evidence; issue #48 remains open. Stable Windows+Android releases v0.1.11 unchanged.

## 2026-10-11 — Experimental native bridge: short-packet resampling and mono/stereo compatibility

- **Observed:** the streaming linear adapter previously threw `Resampler crossed an unreceived input frame` for a fractional sample that depended on the *next* WASAPI callback, and refused even deterministic mono→stereo/stereo→mono routing. Audio callbacks can be irregular; aborting on an ordinary block boundary is an avoidable instability.
- **Implementation:** `PcmFormatAdapter.Encode` defers only the incomplete fractional sample to the next callback using the existing previous-frame state; interpolated output remains sample-aligned and preserves accumulated fractional timing. Explicit mono→stereo duplicate-channel mapping and stereo→mono equal-weight averaging added; matched 1–8 channel paths remain unchanged, unsupported 5.1→stereo still fails closed. No change to the native C++ DSP ABI.
- **Regression:** `--check-formats` now includes irregular 1/2/3/7-frame and longer packets across 44.1→48, 48→44.1, 48→96 kHz; checks total output frame count, finite amplitude, inter-channel agreement and discontinuity bounds, plus exact mono/stereo conversion and unsupported multichannel rejection. The existing Windows `native-audio-engine-ci.yml` already runs this command. At edit time, current-HEAD CI must still pass; **no real sound hardware testing is implied**.
- **Limitations:** linear interpolation is not band-limited, and this does not solve sample-clock drift, high-order layouts, underruns, Bluetooth/hotplug or production microphone/acoustic sound validation. Stable LouderME Windows/Android v0.1.11 remains untouched. No release.


## 2026-10-11 — M2 Windows DSP bridge: streaming anti-alias downsampling (no VM)

- **Motivation:** the experimental WASAPI virtual-to-physical adapter resampled only with linear interpolation. When a higher-rate stream is downsampled (e.g. 96 → 48 kHz or 48 → 44.1 kHz), source content above the destination Nyquist frequency aliases into false lower-frequency tones. This is an avoidable audio-quality defect independent of virtual-driver installation.
- **Implementation:** introduce `StreamingDownsampleLowPass.cs` with a per-channel stateful, normalized Hamming-windowed sinc FIR, 47/63/127 fixed taps based on the rate ratio and cutoff at 90% of destination Nyquist, running before existing interleaved PCM resampling for downsampling only. Its memory/coefficients are allocated once at adapter construction; filtering operates in place on the callback-owned float32 copy, retaining history across arbitrarily fragmented callbacks. Matching rates and upsampling remain unchanged.
- **Device-free tests:** `PcmFormatAdapter.RunSelfTests` adds passband-versus-above-Nyquist RMS checks for 1 kHz versus 30 kHz (96 → 48 kHz) and 23.5 kHz (48 → 44.1 kHz), plus stereo channel isolation. Existing short-callback, 44.1/48/96 kHz, saturating PCM16, native C++ DSP and mono/stereo checks remain mandatory.
- **Verification:** Windows/Linux `native-audio-engine-ci.yml` is required at final PR SHA. CI PASS proves synthetic filter behavior only; it does **not** prove hardware latency, calibrated acoustic gain, production signed SysVAD installation or third-party application streaming. Driver issue #48 remains open and Windows v0.1.11 still uses legacy Equalizer APO.
- **Releases:** none requested; do not change installer/updater or Android v0.1.11.

## 2026-10-11 — M2 Windows: bounded clock-drift feedback, processing metering and memory optimization

- **Observed:** independent virtual and physical audio clocks may accumulate source/render queue drift; the bridge previously reported no per-callback processing costs, and logging every 250 ms added unnecessary overhead. `PcmFormatAdapter.Encode` allocated an oversized array then copied/resized it on each packet even on equal-rate no-correction output.
- **Change:** `AdaptiveBufferClock` estimates slow ±250 ppm resampling-frame-step correction from smoothed PCM queue occupancy, target approximately 50 ms after two seconds of warmup; synchronized atomic read/write preserves streaming sample phase across callbacks. `AudioProcessingTelemetry` records wall-clock work spent inside PCM processing callbacks, nominal audio duration, worst callback and number of callbacks taking longer than their audio duration. Metrics are reported once per second alongside queue occupancy and correction. No microphone, network logging, external device change or audio recording.
- **Efficiency:** matching sample-rate/no-correction output path allocates exactly the PCM output size and skips the second array copy/resizing. This removes a redundant allocation/copy in that case; it is not a proven user-visible CPU or latency reduction on a physical device.
- **Device-free tests:** `--check-timing` validates ±250 ppm bounds, rejects NaN buffer observations, tests synthetic independent sink clocks at ±160 ppm over 1,000 simulated seconds and validates timing counters. `--check-formats` tests exact same-rate PCM and ±250 ppm continuity, plus existing fragmented PCM/antialias tests. Both run in Windows .NET native bridge CI.
- **Evidence boundary:** queued PCM milliseconds and callback work-to-audio ratios are NOT measured acoustic latency or Windows total-process CPU%. No real signed driver or physical hardware is available. A true low-latency measurement, clock drift above 250 ppm, prebuffering/underruns, device changes and production driver signing remain open. Stable Windows/Android v0.1.11 unchanged, **no release**.

## 2026-10-11 — Windows experimental bridge: bounded audio overflow and hotplug recovery

- **Starting gap:** a full 250 ms `BufferedWaveProvider` made the capture callback discard the current packet with no burst policy and no monitor of zero-filled render reads. Device invalidation/physical playback interruption aborted the session without an optional bounded restart; reconnection to the wrong default physical output could create feedback or unexpected sound.
- **New `AudioQueueRecovery.cs`:** tracks consecutive overflows; drop-first-two policy, after a third contiguous overflow clear stale PCM and accept the latest signal. At most **two** flushes within a 30-second monotonic window; further bursts throw a recoverable route-interruption exception and stop the session or enter explicitly opted-in bounded restart. Tracks discarded source and flushed output frames separately. The `MonitoredRenderBuffer` wraps both NAudio3 byte-array and `Span<byte>` `IWaveProvider` read methods; it counts insufficient queued PCM before zero-filling, not actual acoustic underrun.
- **New interruption handling:** continuous bridge loop checks original source and output `MMDevice.State`, treats unexpected capture/render interruption as restart-eligible, and never switches to newly selected default devices. Explicit `--auto-reconnect --confirm-experimental` performs **two maximum retries** after a whole-session teardown; re-enumeration selects only the exact original endpoint IDs. Probes `--probe-route` and `--probe-virtual` never retry. Device selection/name mismatch, invalid PCM or native DSP errors fail closed.
- **Diagnostics:** once-per-second summary includes queue starvation reads, overflow packets, flush resets and separately tracked stale output frames. Source/output count and processing work metrics preserved. Queue-flush discontinuity, source silence, real hotplug, suspend/resume, fault handling in real drivers and audible latency are still untested without an isolated Windows hardware/audio endpoint.
- **Numeric CI:** `--check-recovery` enforces exact drop/flush/abort sequencing and 30-second reset, plus NAudio read instrumentation with/without queued PCM; existing C++ Linux/Windows and managed bridge timing, DSP, sample-rate and device-free tests remain. Final PR SHA checks and Android installed emulator regressions must pass before merge.
- **Release:** no driver installer, no VM, no new production version. Android/Windows stable v0.1.11 remains APO-backed.

## 2026-10-11 — Installer welcome, full EULA acceptance and user shortcuts
- Inno Setup desktop installer now shows Welcome and requires interactive acceptance of root `LICENSE` (full LouderMe proprietary terms), instead of relying on the desktop two-line license stub.
- Existing optional unchecked desktop shortcut and separate startup preference preserved. Added an unchecked user-requested postinstall bilingual taskbar-pinning guide via Notepad; no unsupported silent automatic taskbar pins.
- Exact-head desktop Windows installer CI and interactive wizard evidence pending; release/version unchanged. Shared contract authority: `michels-lab/Michel-Software-Standards/standards/WINDOWS_INSTALLER_EXPERIENCE_STANDARD.md`.


## 2026-10-11 — Native DSP EQ bypass de-click regression candidate

- **Found:** EQ enable/disable in the portable C++ audio engine previously switched between fully wet/dry signal in one PCM frame. With a boosted 1 kHz band this can create a sudden amplitude discontinuity even if the separate Boost gain is smoothed.
- **Candidate change:** AudioEngine now mixes dry and filtered PCM using a 5 ms exponential crossfade (existing gain smoothing coefficient), with no allocation or locks in process(). Filters continue updating through fade-out; a fully bypassed path skips biquads and snaps to exact dry output once the residual mix is <1e-7. Stereo/channel processing remains separate.
- **Regression:** New deterministic C++ test exercises wet baseline at a tone crest, first frame after EQ bypass, fully settled dry level, first frame of re-enable and silent right-channel isolation. Existing DSP gain/limiter/EQ/frequency tests remain mandatory in Linux and Windows CTest jobs.
- **Validation status:** PR candidate; current-head GitHub Actions CTest must pass before integration. This is synthetic DSP evidence, NOT real virtual-driver audio routing, physical click measurement, true-peak limiter validation or dynamic seven-band gain-change smoothing. Those remain open in #48.
- **Release:** no installer/APK/update channel changes and no release authorization; stable LouderME v0.1.11 stays unchanged.

## 2026-10-11 — Prevent cross-app Windows installer/update channel leaks

- **Finding:** Desktop Inno Setup still pointed AppSupportURL/AppUpdatesURL to the personal Michel's Life release repository even though LouderMe now publishes to its own `michels-lab/LouderME-Releases`. The updater also silently fell back to a shared legacy `michel-s-life-releases` feed on a canonical-feed error and did not validate `manifest.product` or publisher URLs before installer download.
- **Fix candidate:** Inno Setup publisher/support/update URLs now all belong to Michel's Lab and LouderME-Releases; new Windows builds use only the dedicated LouderMe feed and never silently switch to another app's feed. Manifest validation explicitly gates schema 1, product `LouderMe Desktop`, stable Windows-x64 channel, strictly HTTPS GitHub release asset/tag URLs rooted under `/michels-lab/LouderME-Releases/`, well-formed SHA-256 fields and parseable version. Downloads and opening release pages repeat the trust check (fail closed), without altering already-installed legacy binary behavior.
- **Regression:** Add headless `--check-update-contract` self-test for valid feed and bad product, platform, beta channel, HTTP, wrong host, Michel's Life repository, checksum and version. Add dedicated PowerShell source/installer guard invoked in normal Windows desktop build CI AND future release CI.
- **Validation status:** candidate branch; installed Windows packaging/build/test CI must pass at exact PR head before merge. No release notes version bump or auto-publication requested. Existing live LouderME-Releases desktop manifest was independently checked to declare `product: LouderMe Desktop`, `platform: windows-x64`, `channel: stable`, and v0.1.11 asset URLs in the dedicated repository.


### Android follow-up — dedicated sideload release identity

- **Audit:** Android `DirectUpdateConfig` also retained a fallback to `michels-lab/michel-s-life-releases`. Its JSON parser previously accepted manifest records without checking schema, app ID or channel before a download, although APK package/certificate/sha checks occurred afterward.
- **Fix:** New builds fetch only `michels-lab/LouderME-Releases/main/louderme/latest.json`. `DirectUpdateManifest.fromJson` requires schema=1, appId=`com.michelslab.louderme`, channel=`sideload`, HTTPS GitHub asset URL rooted in this app's dedicated release repo, a positive versionCode, semantic versionName and 64-character hex checksum. Cached manifests are revalidated before downloads/install attempts. Existing Android certificate continuity verification remains intact.
- **Regression:** `DirectUpdateManifestTest` checks the official release URL and rejects Michel's Life repo, wrong owner/host/protocol, URL query, opaque URI, bad digest and malformed versions. CI must run Play + sideload unit tests and installed emulator visual smoke on exact PR SHA; no user hardware evidence or new release claimed.

## 2026-10-11 — Native C++ live EQ slider de-click candidate

- **Finding:** Although EQ on/off now crossfades (PR #65), changing any of the seven EQ band gains still reconfigured the active biquads and cleared their state instantly. A change from +10 dB to -10 dB at 1 kHz could create a hard transition during music playback.
- **Candidate fix:** preallocate a second filter bank at construction, copy old filter coefficients/history into it when an active EQ value changes, reconfigure the primary bank and crossfade the two wet outputs over the same 5 ms exponential envelope. Retain the prior dry/wet on/off ramp; no allocations, mutexes or OS devices in process(). Skip dual processing after ramp reaches unity. Leave unchanged settings alone.
- **Regression:** new deterministic 1 kHz peak-transition test verifies that the first sample remains near the previous +10 dB response, eventual -10 dB settling and no stereo right-channel bleed. Windows/Linux native DSP and managed bridge tests plus Android regressions must remain green at exact PR SHA.
- **Limitations:** only synthetic PCM; very rapid consecutive band changes while a previous 5 ms transition is unfinished can rebase its old bank and are not yet a measured click-free physical guarantee. No Windows system-wide driver/audio-routing claim, no release/installer change.


## 2026-10-12 — Rapid EQ off/on toggle state regression

- **Finding:** EQ bypass already crossfades over 5 ms, but the off→on path reset biquad history even when no audio frames elapsed between the two UI events. Resetting the wet bank while current EQ mix remains nonzero can introduce discontinuities.
- **Change:** Only reset the dormant filter bank when re-enabling from fully settled dry bypass (`currentEqMix_ == 0.0`). A rapid reactivation retains running filter history and reverses the existing wet/dry envelope. No changes to audio callback allocations or locks.
- **Regression:** A deterministic test warms two equalized DSP engines, instantly off/on toggles one without intervening frames and requires sample-by-sample output equality with the untouched reference. Linux and Windows CTest, managed bridge and Android CI remain required before merge.
- **Scope:** Synthetic evidence only; Windows native global endpoint routing, physical click tests and signed driver still pending under #48. No stable release authorized.

## 2026-10-12 — Synchronize shared agent contract 2026-10-12.1

Updated only the managed blocks in AGENTS.md and .github/copilot-instructions.md from the master canonical source; all local constraints remain unchanged. Body SHA256: 67279f7a0ea27a6212d58d81fe985537e830a974f4a9d73e2759f06177a2af6e. This brings shared installer and independent CI work policy to the current version. No product source, version or release changes. Master coordination reconciles expired reservations separately; ongoing native audio development and hardware validation remain independent. Exact-head Android CI is required before integration; desktop workflow excludes documentation-only changes.

## 2026-10-12 — v0.1.12 conditional maintenance release candidate

- **User approval:** explicit authorization to finish applicable repository work and release when safely validated; this is NOT authorization to ship test-signed kernel drivers or claim experimental global audio works.
- **Release scope:** Android Play + Direct v0.1.12, versionCode 13; Windows Desktop v0.1.12. Includes product-specific updater isolation and security validation from PR #66, current Android UI/volume/boost surfaces, and Windows installer metadata. Native C++ DSP changes from PRs #65, #67, #68 remain separate experimental source architecture, not shipped as Windows system-wide audio driver.
- **New final-artifact gate:** the source Android debug emulator capture remains, plus a new main-only release verification after APK signing. CI downloads the actual stable signed APK artifact, confirms its SHA-256 sidecar, installs that exact package on an emulator, captures Home/About at two viewports, verifies screenshots/manifest and source SHA, then verifies every shot's artifact digest against the signed APK. The Android source publisher is blocked until this job succeeds.
- **Windows requirements:** version and notes together, existing Windows build/test/real install/launch/uninstall/visual check and publisher-isolation gate; do not publish unverified installer. No claim of Authenticode signature or an actual native virtual driver.
- **Signer continuity:** Android Direct certificate SHA-256 pinned to owner-approved v0.1.11+ value (public metadata only). Never put keystore/passwords in repository. Android v0.1.10 or older must reinstall due historic signer change; v0.1.11 should update to 0.1.12 in place after this release passes gates.
- **Unresolved issues:** #48 real Windows physical native routing and signed driver; #46 own DSP integration and Android permissible sessions; #40 legacy signing-vault review; #33 review complete only after final signed Android and shipping Windows artifact evidence is proved. Hardware evidence is not implied by emulator/synthetic CI.
- **Release status:** candidate on release branch, not yet public until exact-head CI succeeds, both source releases have matching versioned assets, and dedicated LouderME-Releases unified publisher verifies both packages and updates both stable manifests. Record the final publication results separately once verified.

## 2026-10-12 — Unified LouderME v0.1.12 public release verified

- **Source commit:** `ed0bd6da54c022ac05eba0b3760361f4bce056ff`; source tags `v0.1.12` (Android) and `desktop-v0.1.12` (Windows) both resolve to this commit. Source GitHub Actions runs `38217270239` (Android) and `38217270238` / `38217270242` (Windows) passed. The Android release pipeline installed the **signed APK itself** in emulator at two viewports and passed the Home/About integrity gate.
- **Public publisher:** `michels-lab/LouderME-Releases` workflow run [38231448218](https://github.com/michels-lab/LouderME-Releases/actions/runs/38231448218) passed, publishing [unified Android v0.1.12 + Windows v0.1.12](https://github.com/michels-lab/LouderME-Releases/releases/tag/louderme-android-v0.1.12-desktop-v0.1.12) with all six APK/EXE/SHA-256 assets.
- **Feeds:** `louderme/latest.json` now advertises Direct Android versionName `0.1.12`, versionCode `13`, APK SHA-256 `0947e7f6b21f4c5beb10ebebe914d3420586504aa0bac7de42d1a565625a68a7`. `louderme-desktop/latest.json` now advertises Windows `0.1.12`, Setup SHA-256 `9bc5def136fc35d01317e9892d599f6aefe5701758fafbf20941c0b459707501` and Portable SHA-256 `b18953af1fbd5d023a828c37eb8b691b1ee88297e91fa9ab6e684de670dcd324`. Public release asset digests matched source asset digests.
- **Signing:** the source release gate verifies the owner-approved permanent Android Direct signer from v0.1.11; in-place Direct update v0.1.11→v0.1.12 is expected, while v0.1.10 or older requires uninstall/reinstall due to different signer. Physical installed update path is not yet observed.
- **Outstanding:** real physical Android boost/EQ tests; Realtek/Bluetooth/HDMI Windows APO behavior; hardware driver/latency/CPU tests for experimental native DSP route; Windows Authenticode; user post-release visual review. Do not claim native global routing is shipped. No Play Store publication implied.

## 2026-10-12 — User reports Windows v0.1.12 UI responds but audio does not change (P0)

- **Observed by owner:** Windows LouderMe launches and GUI controls respond, but Boost/EQ audibly do nothing. Treat v0.1.12 as NOT physically verified for sound processing; the successful packaging, visual and installer CI tests did not certify audio effect. User supplied a screenshot; do not infer the exact device routing or Equalizer APO attachment from the screenshot alone.
- **Code-level diagnosis:** Desktop Boost/EQ only write a managed `LouderMe.txt` Equalizer APO include; the app returns early if APO is absent, Program Files permissions may block writing, and the original `GetStatus` marked 'linked' from a substring in `config.txt`, not an attached processing endpoint. The 100–250% number and animated waveform are *target settings*, not evidence of altered sound. Windows endpoint volume remains a separate 0–100% OS control.
- **Fix candidate:** detect active (not commented) Include and exact saved settings on disk, distinguish NOT ACTIVE / NOT APPLIED / CONFIG WRITTEN BUT SOUND NOT VERIFIED, add explicit one-time consented UAC bootstrap for the managed APO include and scoped Windows-user write permission on `LouderMe.txt` only, preserve independent user `config.txt` and first-use backup, keep configuration read-only from UI unless requested. Provide Copy audio diagnostics for physical triage. No system device default, driver, audio recording or native routing changed.
- **CI proof requested:** Windows build, installed launch, compact layout, original installer smoke, independent device-free APO contract test (missing engine, commented include, preservation, stale settings, update, no duplicate include); Android unaffected.
- **Remaining blocker:** an installed Equalizer APO package and its attachment to the real Windows playback endpoint, plus real-device A/B audible/signal loopback proof. Native C++ engine and driver remain experimental and do not process global playback in stable Windows. No source release, public release or version bump until true audio behavior is verified. This branch cannot honestly be described as a complete fix to the user's silence without real-PC evidence.

## 2026-10-12 — User-operated negative-gain audio path proof candidate

- **Motivation:** 'APO configured' cannot prove that a real Windows application plays through the device to which Equalizer APO is attached. The owner reports sound unchanged despite moving the LouderMe controls.
- **Added diagnostic:** explicit opt-in **Test audio path** action, active only with valid LouderMe configuration, temporarily appends -18 dB to the managed APO file for four seconds while the user plays sound and automatically restores the current persisted settings in `finally`. The dialog asks whether attenuation was audible. A negative (quieter) probe limits risk compared with applying an unsolicited positive 250% gain. The routine uses no microphone, recording, network or driver change.
- **Reporting:** user response is included in the Copy audio diagnostics snapshot (reported yes/no, not a measured acoustic value). 'Yes' suggests that the processed APO route is in use; 'No' indicates the actual output route is unproven, not that volume slider mathematics are wrong. Test success in synthetic CI validates file write/restore only, NOT audibility. Manual physical Windows verification remains blocking.
- **Release:** no new release/version until physical owner proof; the app must not claim globally working 250% from this simulated test alone.

## 2026-10-12 — Inactive DSP controls are not presented as functional

- **UI contract fix:** when Equalizer APO is absent, Windows output master volume remains available but Boost enable, its slider/quick levels, EQ enable/preset and all seven bands are disabled; the warning explains which separate backend is missing. Refresh engine restores interaction after detecting an installation.
- **Interaction fix:** selecting non-neutral Boost above 100% automatically checks Enable global boost; choosing an EQ preset or changing an EQ band automatically checks Enable equalizer. The actual file-processing/permission and real endpoint attachment gates remain separate and visible, so selecting a percentage cannot falsely certify audible amplification.
- **Remaining:** Windows actual playback test on the owner's device is still required; do not infer acoustic effect from screenshots, file writes or green CI alone.

## 2026-10-12 — Windows v0.1.13 owner-authorized release candidate

- **Authorization:** owner explicitly directed that functional checking happens by installing from the official GitHub Release, not Actions artifacts. Release Windows test build as soon as exact candidate build, APO contract, installed UI and installer smoke gates pass; do not wait for owner hardware verification **before** making a testable installer available.
- **Scope:** Windows v0.1.13, includes P0 PR #73 (inactive Boost/EQ behavior, explicit APO connection, Windows permission bootstrap, -18 dB temporary audible test and copyable diagnostics). Native Windows system-wide virtual-driver DSP is not shipped. Android stays at v0.1.12.
- **Gate:** Desktop source release workflow must pass installed launch/uninstall and final rendered evidence, exact APO contract checks and checksum generation, then create an immutable `desktop-v0.1.13` source release. Dedicated public publisher combines Android v0.1.12 unchanged with Desktop v0.1.13 and advances only the Desktop updater version.
- **Limitations:** official installer remains dependent on separately installed/attached Equalizer APO. Actual physical audio A/B test requires the owner's PC after release. Mark the audible-audio bug #74 as open until proven; do not claim 250% effective without sound evidence.
- **Status:** release candidate staged for CI; no source/public release claimed until verified links, release assets and manifests are present.


## 2026-10-20 — Orden permanente GRANADA GRANDE (gobernanza; no release)

- El propietario exige bloques **más grandes y ambiciosos**, con entregables relacionados y funcionamiento de extremo a extremo. Vigente el protocolo maestro en `michels-lab/Michel-Software-Standards/standards/GRANADA_CONTINUOUS_BLOCK_PROTOCOL.md` y la copia local `.michelslab/GRANADA.md`.
- Para el siguiente comando GRANADA: leer HEAD, issues, PR abiertos, claims y este log; escoger un paquete de **tres o más entregables sustanciales relacionados cuando haya alcance real**, incluyendo implementación, prueba del flujo principal, errores/reintentos y validación. Un solo P0 indivisible puede ser suficiente; no inventar trabajo artificial.
- Si cualquier parte está ocupada por otra rama, requiere equipo físico, una cuenta de proveedor, resultados científicos que solo existen en el portátil o autorización del propietario, **aparcar con evidencia y pasar a otra subparte independiente**. Conservar identidad visual, datos, credenciales y estándares de publicación.
- Registro obligatorio: A/B/C con estados separados, ruta/PR/sha, QA **ejecutado** (no simplemente escrito), bloqueos, recuperación, reentrada. Esta actualización de política **no significa ninguna funcionalidad del producto implementada, probada ni publicada**.
- Repositorio: `michels-lab/LouderMe`. En los siguientes bloques aplicar este alcance sin modificar los PR activos o trabajos no relacionados.

## 2026-10-21 — Operación Granada: LAB single-use evidence (source change, not released)

- A. Evidence freshness: require previously nonexistent evidence directory and fresh GUID lock before privileged installation; manual VM workflow uses distinct run ID / attempt folder.
- B. Write-once proof: replace overwritable positive/negative, log, metadata and ratio output with atomic FileMode.CreateNew. Probe rejects inconsistent lock or preexisting phase artifact.
- C. Privacy and negative tests: redact WASAPI endpoint IDs from archived/printed runner traces, retain raw IDs in memory for routing; add pure no-device fixture for reuse, collisions, file preservation and redaction.
- Scope: LouderMe Windows audio only; no other app or OpenFOAM research rules. Branch: granada/lab-evidence-single-use-20261021.
- Exact-head CI: pending at writing; do not claim runtime/signed driver hardware PASS. Signed VM, physical audio and universal third-party integration still unverified (#46/#48).
- Public release unchanged: Android v0.1.12 + Windows v0.1.13; no release candidate. PR #95 previously merged at fad832a9af0a3fedde1f72e052d03625a74a5496.

## 2026-10-21 — Granada: 7-band native EQ contract audit (separate source PR)

- Root cause: native C++ EQ centers `60/170/310/600/1000/3000/12000` did not match public Android/Windows UI `60/150/400/1000/2500/6000/12000`. Native 1-kHz synthetic tests also mistakenly used index 4 instead of product index 3.
- A. Align **experimental** native `AudioEngine::CenterHz` only; no shipping APK/EXE UI or Android device EQ changed.
- B. Test real synthetic response at each of seven center frequencies at 48 kHz, preserve existing bypass/live click-attenuation/channel tests; correct Windows bridge C# 1-kHz index.
- C. Add permanent Python source-contract guard comparing three product source arrays and wire into native CI; Android/Desktop EQ source changes also trigger verification. Detailed root-cause and limits: `docs/audits/GRANADA_2026-10-21_NATIVE_EQ_PARITY.md`.
- QA: exact-head checks to be recorded on PR; physical device and driver acceptance not performed. No stable release or version bump. Issue #46/#48 remain open.

## 2026-10-22 — Granada: Windows listening-test interruption recovery (source branch)

- **Defect:** `ApplyDiagnosticAttenuation` appended -18 dB to Equalizer APO and relied entirely on `finally`. A killed/crashed app could leave this attenuation in effect indefinitely until someone manually rewrote `LouderMe.txt`.
- **A — Recovery:** a distinctive tagged, temporary -18 dB segment is restored when the app next opens, but only when the complete file matches generated LouderMe config for saved settings. The old v0.1.13 unmarked suffix is recovered only with an exact entire-file match.
- **B — Data safety:** third-party changes within a tagged probe are never overwritten by automatic recovery, the finally handler, or later ordinary Apply; show a warning rather than claiming processing works.
- **C — Regression:** `--check-apo-contract` covers crash-style omission of finally, repeat no-op, manual-file conflicts, normal Apply refusal, legacy exact match, legacy foreign edit, and ordinary configuration. No physical audio proof.
- **PR / CI:** `granada/windows-apo-probe-crash-recovery-20261022`; Windows desktop CI is mandatory before merge. No version bump, driver release or public update. Details in [audio recovery audit](docs/audits/GRANADA_2026-10-22_APO_DIAGNOSTIC_RECOVERY.md).

- **D — Interacción durante la prueba:** clonar la configuración de Boost y siete bandas al iniciar la atenuación, suspender los controles Boost/EQ durante esos cuatro segundos y restaurar contra la copia exacta. Nueva regresión impide recuperar usando ajustes mutados durante el test.

## 2026-10-22 — Granada: external-app WASAPI route null dereference (source only)

- **Root cause:** external-app `--probe-app-route` creates no internal `sourceToneOutput` or `toneStream`, but the shared `provePhysicalRoute` branch called `sourceToneOutput.Init(toneStream)` anyway, throwing before `EXTERNAL_SOURCE_READY` and blocking both positive and no-source controls.
- **Fix:** `PhysicalRouteStimulus.Initialize` selects exactly one route: internal tone only when `--probe-route`; external app must remain external. The real monitor setup now uses that mode guard.
- **Regression:** `--check-route-tone` runs additional hardware-free lifecycle fixtures: null internal tone for external-app, exactly one internal initialization for own-tone mode, reject missing/mixed modes. Exact-head Windows native CI required.
- **Release:** no public update, device installation, signed virtual driver, physical playback or user-route switch. Hardware integration remains #46/#48. Detailed audit: `docs/audits/GRANADA_2026-10-22_EXTERNAL_AUDIO_NULL_ROUTE.md`.

## 2026-10-22 — Granada: Windows stable maintenance 0.1.14 packaging (proposed release gate)

- Implemented safe crash recovery in PR #101, merged into main at `113fd10c5891053e1207f1ef577f13edb755d8cb`, with Windows installer/APO and Android CI passing at exact PR source.
- Proposed a Windows **stable maintenance** bump from `0.1.13` to `0.1.14` in the existing desktop project version metadata, plus `desktop/releases/v0.1.14.md`. No Android version/signing/app UI change. The experimental native global-audio driver is *not shipped*.
- Release trigger: new `desktop/releases/v0.1.14.md` committed to main would start the source repo's actual Windows stable release workflow. Do not treat proposed notes, source CI or a package artifact as a published release; verify installer self-tests, installed screenshot evidence and checksum sidecars at the exact release source SHA.
- Unified distribution remains Android 0.1.12 + Windows 0.1.13 until an authenticated `desktop-v0.1.14` source tag, four real Windows release assets, and a separately validated public LouderME-Releases bundle exist. Do not update public manifests prematurely or reuse existing tags.
- The −18 dB recovery is deterministic file-level protection, not actual measured loudness, own OS audio routing or signed driver integration. The user-reported issue of no audible global Boost on Windows remains under #46/#48.

## 2026-10-23 — Granada: restore official LouderMe Windows wordmark and icon (v0.1.15 planned)

- **Owner screenshot evidence:** dark main header shows generic four-tile Windows icon and a wave covering the first letter of `LouderMe`, reading `auderMe`. This is NOT the approved product lockup.
- **Confirmed root cause:** header used a manually composed `WaveformMarkControl` (150 px minimum) in an 88 px column alongside an independent Segoe UI title. Product official branding is already in `app/src/main/assets/branding/louderme/official-lockup.svg`, sourced from `Michel-Software-Standards`.
- **Fix:** render exact official lockup SVG to an embedded PNG during existing ImageMagick Windows branding stage, show in Main/Splash/About on a light plate to preserve approved dark-navy wordmark. Explicitly embed and use official `LouderMe.ico` in Main, About, Developer window titlebars.
- **Regression:** real WinForms compact layout at 480 and 600 px must check lockup visible and disjoint from About, PNG dimensions/palette and source logo text/geometry, generated ICO and embedded resources. Installer build and final Home/About screenshots must PASS on the exact SHA before merging/releasing.
- **Release:** unique Windows v0.1.15 maintenance candidate (not RC); current verified stable public bundle remains Android 0.1.12 + Windows 0.1.14 until actual source + unified publishing succeed. Audio engine, EQ, Android source and SysVAD work untouched. Detailed audit: `docs/audits/GRANADA_2026-10-23_OFFICIAL_WINDOWS_LOGO.md`.

## 2026-10-24 — Granada: urgent Windows UI regression repair (About, dark logo, interactive EQ)

- Owner screenshots of public v0.1.15 proved an unacceptable regression: About cards collapsed to narrow blank columns, canonical logo was rendered on a glaring white panel in otherwise dark UI, and seven-band EQ looked like a static numeric form rather than a usable graphic instrument.
- Root cause of About: `FlowLayoutPanel` card `Panel AutoSize=true` enclosing `Dock=Top` AutoSize table caused sizing feedback and a visually empty card. Set explicit card width and measured height from child preferred content, preserve existing scroll and About functions. Added real `AboutForm.AssertVisibleContent` for all 4 sections, called in Windows screenshot CI before any screenshot is accepted.
- Dark brand: **derive** transparent dark logo from the approved official SVG, changing only inaccessible navy text/tagline colors to readable white/muted color. Preserve waveform, geometry, official font and blue. Reuse across Home, About, Splash. New bitmap CI checks dark asset is actually transparent and includes recognizable light + blue.
- EQ: custom graphic becomes an interactive 7-node / 7-track curve with visible 0 dB reference, dB and frequency labels, pointer dragging at 0.5 dB increments. Existing numeric controls, presets, exact frequency mapping, gain persistence, and the current Equalizer APO application logic remain the source of truth.
- QA gate: exact-head Windows compile, interactive About render test at 600/900 viewports, Home/About screenshot artifacts and installer launch, existing APO + Developer checks, Android nonregression; no immediate update to public installer, no phantom image mockup, no native-driver audio proof.

## 2026-10-24 — Granada: Windows v0.1.16 visual maintenance release preflight

- Owner-reported Windows v0.1.15 regressions addressed by main merge PR #107 (commit `796c857e5128d4fbfa120e6e1eb091f0f7dbced8`): collapsed About, white logo sticker and form-like seven-band EQ. Additional QA found first-open scroll clipping and a magenta Michel's Lab author graphic; both were fixed before merge.
- Inspected actual installed WinForms screenshots from exact PR head `0ec3deec251ee90a170c0b9b8f3b46038b13c115` (Windows run `40117533908` successful): Home, About and EQ, each 600×520 and 900×700, with screen-capture manifest SHA-binding. Android emulated install and Developer audit run `40117533782` also passed. Do not use screenshots as evidence of physical audio performance.
- New release packaging assigns Windows-only v0.1.16 with standalone notes `desktop/releases/v0.1.16.md`. No Android version/signing change. This staged source PR must first PASS Windows installer and Android nonregression CI at its **own** exact head. Merging source notes triggers source Windows stable release; verify tag `desktop-v0.1.16` maps to exact merge SHA and all four EXE/SHA-256 assets.
- The official unified distributor must then separately preserve signed Android v0.1.12 and publish a **new** immutable Windows v0.1.16 bundle with six assets/updater feed checks. Existing Windows 0.1.15 and older public releases must not be overwritten. No RC and no native driver shipped.

## 2026-10-26 — Granada: exact Windows About author/logo/header fixes (QA pending)

- Owner's v0.1.16 screenshots show four gaps in shipping UI: About portrait stuck at 96×128 px, Michel's Lab official lockup replaced with plain text, product title crammed/clipped, Home logo visually centered in a 100%-width PictureBox.
- Applied surgical WinForms fixes on branch `granada/windows-about-author-brand-header-20261026`: fixed-width 308px left header logo / flexible spacer / right About; About title gets explicit 70px height and adequate padding; author photo is 196×250 wide or 148×194 compact and studio lockup is restored as approved PNG prepared for the exact dark card surface, not reconstructed from text.
- CI must verify canonical logo not blank/magenta, real portrait and lockup visibility at 600px/900px, no clipping, all existing Home/Equalizer/APO/Developer tests and Android nonregression. Do not merge or release until verified actual runtime screenshot results.
- Full findings and gate: `docs/audits/GRANADA_2026-10-26_WINDOWS_ABOUT_AUTHOR_AND_HEADER.md`. Public stable version remains Windows 0.1.16 / Android 0.1.12. No native audio/hardware claims.

## 2026-10-26 — Granada: package canonical author branding and left-header fixes as Windows 0.1.17 (release gate)

- Visual source fix PR #109 is merged at `70a31562f2fe08955977cfe094411df84b96f4d6`; Windows CI `40397119450` and Android installed emulator/Developer CI `40397119448` completed SUCCESS at exact head. Six real Windows Home/About/EQ screenshots were inspected: large author photo, actual gradient Michel's Lab approved lockup, full product title, LouderMe left header and right About.
- Original parent-brand PNG in private standards repo and vendored LouderMe source have the **same Git blob SHA** `7fd48093968b31ddacd3098f5b15d962de580652`; released builds no longer need cross-repo access. The old damaged PNG is not embedded in Windows EXE.
- Windows-only version metadata staged for **v0.1.17**, notes in `desktop/releases/v0.1.17.md`. Do not merge/release until exact-head Windows installer/portable + six real screenshots and Android QA pass. After source tag and four checksummed Windows binaries, dedicated public bundle may update Windows only while retaining exact original signed Android 0.1.12 bytes.
- Stable public tag continues Android 0.1.12 + Windows 0.1.16 until the full source and distributor gates succeed. Physical Windows output and experimental independent audio driver remain unverified.

## 2026-10-26 — Granada: fix exact Windows v0.1.17 release gate drift after merge

- **A — integrated:** PR #110 Windows-only version 0.1.17 merged into `main` as `e3338e88f68e971a0a4678d165a98afe0a08c3b8` following exact-head Windows `40398535260` and Android `40398535087` full SUCCESS, including actual installed UI evidence. **This merge is not publication.**
- **B — release blocker:** source publishing run `40400827387` failed in the static `Restore, build and validate` contract **after .NET build succeeded without warnings**. Root cause: `.github/workflows/release-louderme-desktop.yml` still used `MichelsLabLockup` old About token and approved the legacy corrupted bitmap, whereas the new About implementation uses the byte-verified `MichelsLabCanonicalLockup.png` plus `MichelsLabDarkLockup`.
- **C — recovery code:** on isolated `fix/granada-windows-017-release-brand-gate-20261026`, synchronize release workflow brand hash/About checks with tested Windows build, add read-only publishing-vs-build parity verifier and negative cases, include in both Windows CI and release CI. Edit v0.1.17 notes to trigger a **new exact-source release attempt only after merge and CI**, preserving unique tag, immutable source, checksum and unchanged Android signer.
- **Safety:** no unverified installer, no public feed update or v0.1.17 release was claimed during failing run. No DSP/physical audio change. PR #111 native experimental evidence QA completed successful on all three workflows after rerunning the unrelated emulator navigation failure; do not let that source-only work alter the v0.1.17 release SHA.
- **Reentry:** PR of this fix must pass Windows PR build + Android real emulator and Developer regression; then merge, require new Windows source release + four assets/SHA-256, verify source tag exact SHA, then separately advance public LouderME-Releases with an identical Android v0.1.12 asset.


## 2026-10-26 — Granada: reject false-positive external-app Windows audio proof (native source branch)

- **Isolation:** branch `granada/native-positive-route-proof-20261026` from LouderMe main `1bcb533c`. Concurrent PR #109 owns Windows About/header/branding, untouched by this block.
- **Defect:** external-app probe accepted generic success text and partial 440 Hz amplitude/coherence data. It did not bind that PASS to a unique bridge readiness, separately completed source process, a full real physical monitor report with virtual/physical PCM frames and zero drops. The final 100/150 ratio manifest gate was also missing those frame counters.
- **A — source completion and signal validation:** new `PositiveSourceCheck.ps1` verifies separate-app exit, full source PCM completion, bridge scoped success, one unique complete WASAPI record, coherent nonzero 440 Hz, RMS/peak and zero drops. Pure parser: no driver/device operations.
- **B — end-to-end lab handoff:** `probe_independent_app_route.ps1` uses that parser to fail closed before persisting PASS; session-bound JSON now includes measured physical and source frames.
- **C — final ratio provenance:** `IndependentGainCheck.ps1` requires coherent frame and source counters for BOTH 100% and 150% phases, preventing fabricated amplitude-only success.
- **D — regression:** `test_PositiveSourceCheck.ps1` covers 27 false positives; extended `test_IndependentGainCheck.ps1` covers missing/fractional/dropped/phantom frames; existing Windows native workflow now triggers on new files and runs these fixtures.
- **Quality status at writing:** code committed on isolated branch. Exact-head Windows GitHub CI and PR reviews still required; do not claim unit tests executed locally (no PowerShell runtime in this environment). No physical VM, signed driver, actual owner device/audio A/B proof, merge or stable release claimed. Hardware acceptance remains #46/#48; public bundle Windows 0.1.16 + Android 0.1.12 unchanged.
- **Reentry:** after CI, inspect positive parser fixtures and physical route logs/manifest contract; only merge when tests pass, then physically qualify signed isolated VM setup separately. Full audit: `docs/audits/GRANADA_2026-10-26_INDEPENDENT_AUDIO_POSITIVE_EVIDENCE.md`.

## 2026-10-26 — Granada: conflict-safe native QA integration after v0.1.17 publication

- The original native-only PR #111 passed DSP, SysVAD derived-driver CI and the Android emulator/Developer test on exact source head `637500ef0cb1cd557649ba90339dbf7a710408b4`, but it conflicted with later Windows v0.1.17 release log entries on `main`. GitHub returned HTTP 405 for the first merge attempt; no force merge was used.
- To retain ALL Windows v0.1.17 publication audit records, the native implementation/test/audit files were copied byte-for-byte from the PR #111 reviewed head into `granada/native-pcm-evidence-rebased-20261026`, newly based on the latest `main`. Existing source-file blob SHAs were checked against PR #111's original base before copying (no native path collisions). The old source log was appended here instead of replacing the product history.
- The official unified public stable release is Android v0.1.12 + Windows v0.1.17, already verified against source and public artifacts and both updater feeds. This PR makes no version, UI, release or public distribution change. Real signed-driver/physical third-party audio acceptance remains open (#46/#48).
- New branch must pass exact-head native DSP/virtual-driver and Android nonregression CI again prior to merging. No additional stable release is needed for source-only LAB validation.


## 2026-10-27 — Operación Granada: honest WASAPI render-consumed PCM proof (#46/#48)

- **Current stable:** Android 0.1.12 + Windows 0.1.17 remains the officially verified unified release. No release/version bump, driver install or user data touched in this block.
- **A — cleanup:** redundant old visual Windows About/EQ PRs #105 and #106 closed as superseded by merged #107 (and final official branding #109). No obsolete code was merged.
- **B — root cause:** bridge label `physicalEmitted` counted PCM at queue insertion (`outputFrames`), not actual WASAPI render-provider consumption. NAudio `ReadFully` can also return zero-padded empty frames; it must not be mistaken for queued PCM. External 100/150 frame checks therefore allowed an ungrounded emission counter.
- **C — functional changes:** `MonitoredRenderBuffer` counts real queued bytes provided to WASAPI callback separately from silence padding for both array/Span reads; full/partial/empty read tests. Physical route now prints distinct `physicalQueued`, `renderConsumed`, `starvedRenderReads`; demands minimum one second of render-consumed output and `renderConsumed <= physicalQueued`.
- **D — chain of evidence:** independent source/physical loopback parser, no-source negative control, session JSON, and final 100/150 gain acceptance enforce the new frame counters. Negative before/after controls reject any unexpected virtual or output PCM; obsolete `physicalEmitted` proofs cannot be used as passes. New adversarial fixtures and Python static regression.
- **E — QA/reentry:** new CI at exact PR head pending at initial coding. Require successful Windows native and PowerShell proof fixtures, virtual driver source/WDK jobs and Android installed emulation/Developer checks before merge. **No actual installed lab hardware, trusted driver signing or acoustic SPL claim.** Audit: `docs/audits/GRANADA_2026-10-27_RENDER_CONSUMPTION_PROVENANCE.md`. Owner's target of real independent system-wide Windows enhancement remains blocked until separate signed-device/physical acceptance.


## 2026-10-27 — Granada continuation: default render route protection for disposable Windows driver lab

- **Merged first:** native source QA PR #115 merged to `main` at `7c145e739005213c2488af5430efc0e2604deeca` after full exact-head native C++/managed Windows+Linux, SysVAD WDK/INF/CAT, Android installed UI/Developer CI all SUCCESS. This corrected the false `physicalEmitted` queue-only metric and added actual render-read provenance. The stable public Windows 0.1.17 / Android 0.1.12 release remains unchanged.
- **New safety hazard:** Windows may reassign its Console, Multimedia and Communications default playback identities automatically when a PnP render driver is installed. The old disposable-VM test explicitly did not call SetDefault, but lacked objective pre/post verification, so its manual "check after uninstall" was not adequate.
- **A — actual metadata sensor:** explicit single-purpose `--default-render-fingerprint` option queries the three real default WASAPI render roles, refuses missing/inactive defaults, and emits one SHA-256 of ordered identities without raw endpoint names, device IDs, sound capture or OS mutation.
- **B — fail closed before playback:** disposable-VM helper captures this hash before `devcon`, checks after PnP before initiating any physical sound, again after the separate-app gain proof, and in `finally` after driver removal. Unknown/changed hash is a hard LAB failure and VM snapshot reversion warning; no automatic replacement of user-selected device.
- **C — safer ownership:** once a root instance is identified, `pnputil` removes only that exact created instance. Ambiguous partial installation of multiple root instances no longer sweeps them all; halt with VM-snapshot recovery.
- **D — QA:** pure guard PowerShell adversarial fixtures (14 cases), native Windows CI hook/script parser, static SysVAD Python contract checks for default guard placement and signed-lab scope. All actual audio/hardware evidence still PENDING; no signed production driver/physical PCM/default device acceptance claimed.
- **Full audit:** `docs/audits/GRANADA_2026-10-27_DEFAULT_RENDER_LAB_GUARD.md`. Reentry: new PR exact-head Windows-native/DSP CI + WDK/INF source CI + Android installed/Developer nonregression; merge only if all pass, without producing a stable installer or changing Android signature.


## 2026-10-27 — Operación Granada: Windows Developer Edition notes backup/restore (#77)

- **Audit of existing features:** Windows Developer already launches separately, shares real app engine/UI, isolates `LouderMe.Developer` local files, offers live section annotations, local read-only audit and reviewed JSON/AI export. Android Developer already has owner-controlled note backup/import with conflict preview. No provider login exists in the public LouderMe app, so no fake sign-in is introduced.
- **A: Real Windows UI parity:** add **Backup notes** and **Restore notes** to the actual Developer WinForms inspector. Backup requires privacy warning and owner-selected save location; JSON retains notes, stable UI target, severity/status and creation/update timestamps exactly. Destination may be a user-selected synced folder: never automatically upload or share.
- **B: No destructive restore:** schema/product/platform/edition and UTF-8 size guards; max 250 notes, max 12K chars per comment, 2 MB input/output. Preview new/identical/conflicts. Yes = owner explicitly replace matching IDs; No = new notes only; Cancel = nothing. Never delete notes missing from a backup, or update the current UI list before successfully writing Developer-only local data. Android export is intentionally not auto-imported into incompatible Windows anchors.
- **C: Corruption and regression:** validate local saved JSON contents (not just syntax) before accepting notes. Validate next list before existing atomic write. Add exact real .NET migration tests to `--check-developer-contract`: Unicode/newline verbatim roundtrip, matching-ID conflict safety, malformed/oversized/wrong-platform files, 13 adversarial cases, isolation from public mode, preserved corrupted files and time consistency. Tests create only a disposable temp directory.
- **Scope:** no Windows/Android stable release or native-driver change; public Windows 0.1.17 / Android 0.1.12 remains unchanged. New source PR requires exact-head Windows compile/UI/installed Developer smoke and Android installed/Developer nonregression. Remaining #77: broader event/crash audit and external/account scopes only if product authentication actually exists.
- **Audit:** `docs/audits/GRANADA_2026-10-27_WINDOWS_DEVELOPER_NOTES_MIGRATION.md`. Reentry after CI: fix compiler/runtime regressions, merge only if all required gates succeed, keep physical #46/#48 open.


## 2026-10-27 — Granada: persistent Developer runtime incident evidence (#77)

- **Baseline audited:** current stable public Windows 0.1.17 / Android 0.1.12, no outstanding PR; independent Developer app surfaces already implement actual Windows/Android annotations, read-only audits and reviewed AI exports. Issue #77 remains open because transient runtime errors were not retained for later inspection. No public auth exists; no fake sign-in/workspace.
- **A — Android actual state observer:** Developer-only Compose watches real AudioEngineUiState/EqualizerUiState changes and records transitions to five strictly allowed error/invariant codes, with bounded count and timestamps; deduplicates an ongoing condition and counts recurrence after recovery. Persist in isolated package prefs, separate from user notes. No engine message, endpoint name, trace, PCM, user content or credentials stored.
- **B — Windows actual caught-fault census:** Developer-only atomic metadata history records closed-code startup faults and failed notes/report save operations; read-only DeveloperAudit includes observed evidence, rejects corrupt runtime ledger without replacing it. Existing crash handlers and public app unchanged.
- **C — QA:** Android JVM tests for observed state, dedup, JSON roundtrip, malicious/free-text rejection; Windows `--check-developer-contract` for persistence, repeated incidents, corruption and whitelist. Build and actual installed UI/Developer smoke workflows mandatory at exact PR head. No microphone/system audio capture or physical driver evidence.
- **Reentry:** run Windows + Android exact-head CI; fix compiler/assertion failures before merge. Source-only change; do not sign/publish Windows/Android release. Audit `docs/audits/GRANADA_2026-10-27_DEVELOPER_RUNTIME_EVIDENCE.md`. Actual renderer crashes, broader asynchronous fault tracing, user acceptance and native global gain remain pending under #77/#46/#48.


## 2026-10-27 — GRANADA: verified live Home/About navigation and safe inspector geometry (#77)

- **Observed root causes:** Android Developer `Locate` previously only set `focused` and switched a declared screen, with no proof its anchor was actually displayed; stale `visibleBounds` survived Compose conditional unmount until a screen change. Windows Developer checked existence/width of About but not clipping in its actual host client area; genuine user About-modal failures were not categorized.
- **A — Android true UI evidence:** `DeveloperNavigationEvidence` only accepts known, same-screen Home/About target requests. `Locate` verification requires the exact live Compose anchor to have finite positive bounds intersecting the real measured screen viewport. Its status remains `Waiting`/not yet observed for missing, offscreen, wrong-screen and unmounted targets; no false verified navigation. Inspector shows current result and structured Developer report records just locator, screen, UTC request/verified times, honest status; no raw note/private data collected by automatic navigation proof.
- **B — lifecycle and negative tests:** `DisposableEffect` unregisters real controls on unmount; invalid geometry does not populate pick targets. `DeveloperNavigationEvidenceTest` covers valid navigation, wrong screen, obsolete target, zero/negative/NaN/Infinity dimensions, offscreen viewport, unknown viewport and time ordering. Existing Android Home/About+Developer app install tests required.
- **C — Windows About diagnostics:** `DeveloperAudit` checks button hit rectangle is fully in the real client viewport and whether a *visible* About form has live mounted controls, no screen scrolling/input. Seven deterministic clipped/invalid geometry fixtures added to existing `--check-developer-contract` test. `OpenAbout` logs allowlisted `ABOUT_NAVIGATION_NOT_SHOWN` or `ABOUT_NAVIGATION_EXCEPTION` from actual unshown/throwing modal paths only in Developer Edition; no exception/stack/path telemetry.
- **QA / limits:** source branch PR pending CI. Require exact-head Windows installer/runtime visuals/Developer launch and Android unit+installed visuals+Developer persistence green before merge. No release/version bump, no claimed signed hardware audio. Public Windows 0.1.17 + Android 0.1.12 remain unchanged. Audit `docs/audits/GRANADA_2026-10-27_LIVE_NAVIGATION_EVIDENCE.md`. Reentry: inspect CI; fix exceptions and merge only when all checks pass; keep #77/#46/#48 open.


## 2026-10-27 — Granada: exact shipping Windows Setup + Portable visual binding and signed Android final APK (#33)

- **Verified baseline without duplication:** Android already runs separate RELEASE-signed APK emulator screenshots *after signing and before publishing*, not merely Debug capture. Windows release and build already install the actual Setup output, capture Home/About/EQ from the installed real WinForms EXE and pass exact source SHA. The real gap is **no enforced installed EXE vs shipped portable EXE byte comparison**, and signed Android evidence lacked strong coverage/image-file/source validation of its own.
- **A — Windows shipping provenance:** new fail-closed gate reads actual installed LouderMe.exe, final Portable vX.exe, tested Setup vX.exe, two SHA256 sidecars and six actual installed WinForms screenshot manifest entries. Requires installed EXE digest == Portable asset digest, all sidecar digests and filenames correct, Setup version matching portable, source SHA and unique compact/wide Home/About/EQ screenshot coverage. Emits shipping-provenance.json with Setup/Portable/installed/manifest hashes and explicit QA status. Both Windows CI + stable release workflow run the same device-free adversarial tests AND the real installed candidate gate before uninstall/release.
- **B — Android final signed evidence:** improve existing post-signing verifier to require source SHA, exact 4 unique signed-emulator Home/About shots, actual image bytes matching hashes, path containment and correct Android installed capture provenance. Existing release visual_release_gate still validates PNG integrity; no fake test certificates, secret access or extra signing in PRs. Add 12 adversarial hermetic cases to routine Android validation so changed verifier code is tested before next release.
- **C — QA/documentation:** permanent audit docs/audits/GRANADA_2026-10-27_FINAL_SHIPPING_VISUAL_PROVENANCE.md, tests tools/test_windows_shipping_provenance.py and tools/tests/test_signed_android_evidence.py. Require exact-head Win installed UI/Developer smoke plus Android Gradle/JVM/emulated Home/About/Developer QA before merge. Keep #33 open until a future actual published version's release assets and after-publication human review are checked; no new Windows/Android stable release or audio driver proof.


## 2026-10-27 — Operación Granada: legacy Android signer-vault exposure and credentials upload prevention (#40)

- **Historical finding:** Issue #40's original 2026-10-08 text was stale. The owner already authorized permanent signing identity migration in Android 0.1.11; subsequent signed APK 0.1.12 and unified Android 0.1.12 / Windows 0.1.17 package exist. Old October 8 Android workflow source wrote a local `signing/github-actions-secrets.txt` and mentioned Actions artifact vault recovery. Subsequent one-time Oct 8 fallback referred to a historical Actions artifact containing old signing material. The current release workflow eliminated this fallback and checks the owner-approved public certificate twice. **Historical archive deletion/access has NOT been verified.**
- **A — no key exfil regression:** add `tools/audit_android_signing_artifacts.py`: mandatory four GitHub Secret variables, both public certificate pins, final APK signature verification and ephemeral keystore cleanup; ban Actions archive fallback and upload-artifact paths matching JKS/private key/vault/backups. No secrets read, extracted, committed or logged.
- **B — safe read-only history audit:** new isolated first-main-merge/weekly/manual workflow `.github/workflows/android-signing-artifact-hygiene.yml` can enumerate GitHub Actions artifact **metadata** with Actions:read, flag suspicious nonexpired names, and optionally query the artifact ID the owner inputs for a known old signing vault. It **never downloads ZIP data, never deletes anything, and never prints archive identifiers**. Incomplete or unauthorized API access fails closed. A clean name scan is not proof old archive contents are safe.
- **C — CI and boundary:** hermetic `tools/test_android_signing_artifact_hygiene.py` exercises old fallback, forged pins, credential upload, 403 failure, paging, active/expired/deleted historical IDs, and no ZIP content access. CI must confirm it passes on exact PR head plus existing Android installed/Developer checks. No public release or version/signature change.
- **Pending external/admin:** repository/org Secrets permission review, inspect old signing/recovery runs, secure deletion of any historical credential artifacts, check sharing/access retention; revoke retired key if its exposure warrants it. Keep #40 OPEN. Full audit: `docs/audits/GRANADA_2026-10-27_SIGNER_VAULT_ARTIFACT_HYGIENE.md`.


## 2026-10-27 — GRANADA: reusable managed PCM scratch in real experimental WASAPI bridge (#46/#48)

- **Measured design gap:** C++ `AudioEngine::process` already avoids heap allocation but the real Windows bridge callback created a new float decoder array and allocated/resized a second render array for every capture packet. Per-packet GC is avoidable and potentially bad for audio continuity.
- **A — Actual callback integration:** reserve PCM16/float32 decoded float and target render scratch ONCE after validating selected virtual/physical device formats. Maximum 500 ms of source PCM, upper bounded by 8 channels/sample-rate and ±250 ppm resampling allowance. Reject larger incoming packets or insufficient scratch safely, instead of implicitly heap allocating on WASAPI callback. No endpoint defaults/driver installation affected.
- **B — Streaming parity:** new `DecodeInto` and `EncodeInto` APIs operate on caller-owned buffers; streaming FIR accepts span with history retained; legacy allocating test convenience wrappers remain. No heap work or resizing in new callback path. Continue finite sample check, frame alignment, anti-clipping and sample-rate drift behavior.
- **C — Device-free QA:** `RunScratchSelfTests()`, CLI `--check-scratch`, Windows native CI: 6 PCM16/float32 44.1/48/96 kHz mono/stereo/8-channel routing matrix scenarios x60 packets, sample-for-sample parity to old API, decode/output undersize fail-closed, GC allocation budget over 256 warmed 10 ms PCM packets. No physical sound/signed virtual driver proof or production release. Documents `docs/audits/GRANADA_2026-10-27_NATIVE_WASAPI_REUSABLE_PCM.md`.
- **Next:** CI exact-head Windows bridge C++ native Linux, existing WDK lab/source testing. Fix any compiler/numeric regression before squash merge. Keep #46/#48 open pending real physical VM and production signing, true-peak limiter and other platform proof.


## 2026-10-27 — GRANADA: prevent binary substitution during isolated native Windows physical-lab proof (#48/#46)

- **Observed issue:** manually provisioned disposable VM lab had coherent 100/150 independent-source session evidence but did not verify that the actual INF/CAT/SYS, NAudio bridge, native C++ DSP DLL, independent tone DLL and WDK devcon.exe stayed byte-identical across every stage. A binary could be replaced between silent controls/physical monitors without invalidating the result. Additionally, VM test console previously printed raw private PnP instance IDs.
- **A — preflight + immutable binary evidence:** added reusable source-only PowerShell `LabInputIntegrity.ps1` taking seven explicit distinct ordinary-file inputs, SHA-256, lengths, rejecting missing/reparse-point/aliased files. The real `run_disposable_lab.ps1` now records only hash metadata and revalidates the exact seven binary contents before installation/discovery/each lab signal and negative/positive phase and AFTER owned-device cleanup. Any mismatch fails closed while retaining bounded owned-device cleanup and the default Windows audio endpoint identity guards.
- **B — privacy and negative verification:** raw root PnP instance IDs no longer printed to shared CI logs. Added offline `test_LabInputIntegrity.ps1` with same-size bridge mutation, shortened C++ DSP, deleted kernel SYS, empty CAT, missing/aliased source/bridge, metadata redaction and source checks for every VM phase. Routed through normal Windows/native audio workflow including PowerShell parser syntax checks. No PnP/install/hardware touched in normal CI.
- **C — limitations:** existing pinned Microsoft SysVAD-derived WDK build is NOT a signed production audio driver; hashes alone are not proof of trusted provenance, hardware output or application-level OS routing. Manual owner-consented trusted and snapshot-backed disposable VM still required for real independent app -> virtual endpoint -> DSP -> physical monitor proof. No new release/version, no refactor of stable fallback. Full audit: `docs/audits/GRANADA_2026-10-27_LAB_INPUT_IDENTITY.md`.
- **Next:** check exact-head native Windows/Linux and virtual WDK CI; fix issues before merge. Keep #48/#46 OPEN.


## 2026-10-27 — Granada: transparent unity native DSP bypass, smooth limiter engagement (#46/#48)

- **Source defect found:** C++ native `AudioEngine::process` always soft-limited input over amplitude 0.88 even with Boost disabled and flat EQ. It compressed legitimate near-full-scale dry PCM, thus bypass was not transparent. Real limiter engagement also happened without transition when switching controls.
- **A — native audio engine:** independent `limiterMix_/targetLimiterMix_` smoothed over existing 5 ms coefficient, enabled only for Boost >100% or nonflat active seven-band EQ. Exact normalized float32 dry samples at unity now pass through without soft-knee compression; nonfinite inputs and >1/-1 samples still safely bounded. With active effects, knee engages progressively and a final [-1,+1] hard bound prevents temporary overload. No new audio memory allocation, driver operation, thread or credential handling.
- **B — regression:** native C++ tests prove bit-exact 0.88–1.0 dry stereo peaks across 44.1/48/96 kHz, 250% setting while Boost disabled, zero-dB EQ, first-frame limiter ramp, controlled soft-knee reduction, disabled effect settling to dry, invalid PCM sanitation, and frame-exact parity with irregular capture packets. Existing Windows C++/Linux C++/native Windows bridge and Android installed Developer tests mandatory.
- **C — limits:** experimental native route only. Stable Windows Equalizer APO unchanged; signed virtual driver/physical playback and intersample true-peak/professional acoustic quality remain UNVERIFIED; no public release. See `docs/audits/GRANADA_2026-10-27_NATIVE_TRANSPARENT_BYPASS.md`.
- **Reentry:** inspect exact-head CI, fix any tests and merge only on green. Keep #46/#48 open for real VM audio.


## 2026-10-27 — GRANADA: bounded physical audio VM child process/pipe handling (#48/#46)

- **Actual risk audited:** disposable VM runner launched long-running native dotnet bridge, Devcon installer and PnPUtil cleanup directly, without per-process timeouts. If any stalled, the overall 25-minute GitHub workflow cancellation might prevent the `finally` PnP cleanup. The independent-app probe redirected stdout and stderr but waited for exit before reading pipes, susceptible to deadlock at OS pipe capacity; bridge stderr could block before the single readiness marker.
- **A — owned child-process lifecycle:** `LabProcessGuard.ps1` starts owned process with `ArgumentList` (not shell string), concurrently drains stdout + stderr, enforces local duration and bounded evidence size, retains exit codes and kills *only* its owned process tree on timeout. Real runner uses it for finite list-devices, two tone probes, Devcon install and PnPUtil remove; fail closed and preserve existing `finally` owned-device cleanup, unchanged physical/default-render role checks and source hash guards.
- **B — independent Windows source:** probe drains bridge stderr from launch, awaits real `EXTERNAL_SOURCE_READY` within 12 sec with bounded log, drains bridge stdout tail before separate source launch, drains source stdout/stderr concurrently and bounds all waits. Source still starts **only** after bridge readiness, uses same separately selected virtual and physical output endpoints, and preserves existing negative controls and PCM proof scopes.
- **C — device-free adverse QA:** Windows native workflow runs `test_LabProcessGuard.ps1` with actual synthetic pwsh children: 70KiB both output pipes, source readiness with full stderr, output ceiling, nonzero exit, hung child local 1.2 sec timeout and owned termination, plus static guard of the real VM callsites. PowerShell parses both new files. No driver, physical sound, permissions, PCM recording or published release. Full audit `docs/audits/GRANADA_2026-10-27_BOUNDED_LAB_PROCESS_LIFECYCLE.md`.
- **Limits/reentry:** Kernel device operations can outlive a killed user-mode command, and external CI cancellation/hypervisor failure still requires VM snapshot restoration. Require exact-head native Windows/Linux, Android installed Developer/Home/About, WDK if triggered. No global audio proof; keep #46/#48 OPEN until actual signed isolated driver, independent app PCM and physical endpoint tests.
