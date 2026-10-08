# LouderMe — Agent Contract

LouderMe is a dual-platform audio utility: Android plus a native Windows Desktop app. Android has separate Google Play and Michel's Lab Direct/sideload distribution paths; Windows has its own installer/portable stable channel. Before editing, read `.michelslab/project.yml`, `MICHELS_LAB_PROJECT.md`, `PROJECT_LOG.md`, `docs/INFRASTRUCTURE_AUDIT.md`, and the platform-specific workflows/build configuration that own the affected behavior.

Shared Michel's Lab rules live in `michels-lab/Michel-Software-Standards`.

## Product constraints

- Preserve separate Android `play` and `sideload` behavior. Do not leak `REQUEST_INSTALL_PACKAGES` into the Play flavor.
- Treat Android and Windows as distinct product surfaces that share LouderMe identity and audio semantics but have different engines, packaging, updater and validation boundaries.
- Windows endpoint volume remains 0–100%. Desktop Global Boost above 100% must use the documented Windows APO processing path; never relabel normal endpoint volume as 250%.
- Desktop currently uses user-installed Equalizer APO 1.4.2 as the local system-effects backend. Preserve LouderMe's managed `LouderMe.txt` include/backup behavior and do not overwrite unrelated user APO configuration.
- ASIO and WASAPI exclusive-mode streams can bypass Windows system effects; do not claim universal system-wide coverage when the platform path is bypassed.
- Preserve Desktop release separation: source tag `desktop-vX.Y.Z`, public tag `louderme-desktop-vX.Y.Z`, and public `louderme-desktop/latest.json` stable feed.
- Desktop updater downloads only a newer published installer and must verify its manifest SHA-256 before Windows is asked to launch it.
- Michel's Lab Direct updates require a stable signing identity, public anonymous feed, SHA-256 verification, package/version validation and signing continuity before Android receives the APK.
- Never replace stable release signing with ephemeral CI debug signing.
- Do not describe target signal gain as measured acoustic dB SPL.
- Equalizer/product-facing bands must remain stable while device-specific band mapping/clamping stays explicit.
- Foreground app chrome must honor Android system insets/safe drawing; do not fix status-bar collisions with one-device magic padding.
- Preserve the current IG Cleaner-family visual language unless redesign is explicitly requested.
- High-gain and EQ audible quality require real target-device validation; repository tests cannot prove acoustic behavior.
- Never commit signing keys/passwords, Play credentials or privileged service secrets.


## Fundamental visual identity and About — mandatory

This is a **core LouderMe product contract**, not optional branding polish.

### Product-wide visual system

The approved flowing-waveform logo geometry is the foundation of the app's visual system. Preserve the defining silhouette, proportions and spatial relationships. Color, monochrome/inverted treatment, glow, glass, outline, translucency, material and motion may adapt to theme/context.

Do not satisfy branding by pasting the source SVG into unrelated screens. Translate the mark's visual DNA into volume/EQ visualization, activity states, separators, loaders, cards, hierarchy, status states, highlights and motion where appropriate. Audio correctness, safe-area behavior and usability remain hard constraints.

### About hierarchy

1. **LouderMe product identity**: official mark, product name, actual version, and waveform-derived visual language.
2. **Paired author/studio composition**: portrait + Michel Duarte + developer role on the left, official Michel's Lab logo/lockup + studio identity + canonical slogan **TOOLS WITH IDENTITY.** on the right where width permits. Stack responsively on narrow screens without splitting their visual grouping.
3. **Social profiles**: every canonical network rendered with recognizable icon AND visible name.

Do not replace the studio slogan with lengthy promotional text. The LouderMe product slogan **SOUND THAT LIFTS YOU** remains separate. Desktop initial About viewport (>=1024×700) must expose five canonical social entries without scrolling. Follow the current master About, Product Identity, Native Interface and Rendered UI Release Gate standards.

## Validation

Inspect the current workflows and run the strongest relevant validation for the changed platform: Gradle/build/tests plus manifest/permission separation for Android; .NET build, APO-contract checks, portable/installer packaging and updater/feed integrity for Windows.

Device-only Android audio quality, output routing, PackageInstaller UX and Play-delivery behavior remain explicitly unvalidated until exercised on the target device/channel. Windows APO endpoint attachment, reboot behavior, SmartScreen/AuthentiCode state and audible 100–250% behavior remain unvalidated until exercised on a real Windows machine.

## Completion

Update `PROJECT_LOG.md` with meaningful fixes, decisions, failures, tests and release evidence. Infrastructure changes also update `docs/INFRASTRUCTURE_AUDIT.md` when relevant.

Do not bump versions or publish releases unless explicitly assigned.


## Intelligent brand adoption

When the user asks to update/adopt the app logo, icon, splash, startup or About:

- use the canonical product assets from `michels-lab/Michel-Software-Standards/shared-assets/product-logos/`;
- follow `standards/PRODUCT_IDENTITY_STANDARD.md` and `standards/BRAND_ADOPTION_PLAYBOOK.md` from the Michel-Software-Standards repository;
- inspect this app's current design system before placing assets;
- replace the real active platform identity references instead of layering the new logo over legacy/generic branding;
- use the app icon for launcher/executable/favicon derivatives, the mark for compact identity, and the lockup for larger splash/About surfaces when appropriate;
- treat the logo geometry as design language where useful, but do not repeat the literal logo across screens;
- build About as Product → paired Author/Michel's Lab → Social, with the canonical studio slogan TOOLS WITH IDENTITY.;
- use the canonical Michel Duarte portrait and Michel's Lab mark in About;
- preserve unrelated product behavior;
- update this repository's project/audit log and validate current build/CI;
- do not publish a release unless the user explicitly authorizes it.

A change that merely pastes the SVG/PNG into an arbitrary card or header is not a completed branding migration.

## Cross-chat claim guard

Michel's Lab uses the master `.michelslab/task-claims.json` / generated queue metadata to prevent multiple chats or agents from editing the same tracked task concurrently.

Before starting a delegated tracked task:
- inspect the claim metadata included in the handoff/current master queue when available;
- if a different owner has an active non-stale claim, **stop and report the collision instead of editing**;
- stale claims require a freshness check before work resumes;
- do not treat a claim as validation or release permission;
- return branch/commit/validation status in the handoff so the master owner can heartbeat, complete or release the claim.

## Structured handoff requirement

For any tracked Michel's Lab task, return enough machine-readable continuation context for the master handoff registry:

- task ID and repository;
- owner/role and branch;
- outcome;
- commits and areas changed;
- validations that **actually ran** and their real result;
- evidence status: `verified`, `inferred`, or `blocked`;
- remaining work;
- blockers/manual evidence still required;
- suggested next owner/role when useful.

Do not list planned tests/builds/device checks as completed validation. If required validation was not performed, the task must be released/handed back with that work pending rather than described as complete.

<!-- MICHELSLAB_SHARED_CONTRACT_BEGIN id=child-agent-core version=2026-10-08.1 -->
# Michel's Lab shared child-agent contract

This managed block is cross-project policy. Repository-specific instructions may add stricter local rules outside this block, but they must not weaken or contradict it.

## Shared authority

- Michel's Lab shared standards, product identity, governance, release and coordination rules are authoritative in `michels-lab/Michel-Software-Standards`.
- Keep product implementation truth and product-specific audit logs in this child repository.
- Do not silently invent a conflicting local Michel's Lab rule.
- Never commit secrets, credentials, signing material, private tokens or passwords.
- Historical green CI is not proof for the current commit.

## Product identity / About

- Preserve the approved product-logo geometry; contextual color, material, lighting and motion may adapt without identity drift.
- Branding is a design language, not sticker placement.
- About hierarchy is **Product identity → paired author/studio composition → social profiles**.
- The paired author/studio composition should show **Michel Duarte** and **Michel's Lab** side by side when width permits: portrait + name + developer role on one side; official Michel's Lab logo/lockup + studio name + canonical slogan **`TOOLS WITH IDENTITY.`** on the other.
- Narrow/mobile layouts may stack responsively, but the author and studio must remain visually grouped as one intentional composition.
- Do not replace the Michel's Lab slogan with a paragraph-length studio review by default.
- Use the current canonical Michel Duarte portrait and the official Michel's Lab parent-brand assets from the master authority when implementing/updating About.
- The canonical portrait file is immutable: child repositories must vendor it byte-for-byte. Never resize, crop, recompress, retouch, regenerate, convert or rewrite the portrait asset itself; use render-time layout/object-fit/masking only.
- Visible social controls use recognizable network icon **and** visible network name with canonical profile URLs.

## Rendered visual brand release gate — mandatory for every app

- For launch, splash, About, launcher or product-identity changes, follow `standards/BRAND_VISUAL_VALIDATION_STANDARD.md`. Checking that an official asset exists/decodes or that a build passes is **not** visual acceptance.
- Inspect computed final UI geometry, theme/text contrast, clipping/overlap and duplicate lockup/heading. CI must fail for known visual violations; require screenshots from the exact candidate and human visual review before release.
- Validate relevant viewport sizes/themes in the actual browser/native/mobile runtime, including the packaged app where possible. If evidence is missing, explicitly report `pending visual review`; never say branding is complete from static tests alone.

## Canonical identity asset precedence

- Product-logo authority is `shared-assets/product-logos/manifest.json` in the master standards repository.
- Michel's Lab parent-brand authority is `shared-assets/michels-lab/manifest.json`.
- Assets explicitly marked legacy/rejected must never supersede the current canonical geometry.
- When `.michelslab/identity-sync.json` marks an identity section `enforced`, child canonical copies must match the master source exactly; hash drift is a governance defect.
- When identity state is `migration_pending`, do not auto-replace local assets. Stop, reconcile the active surfaces explicitly and preserve the approved master geometry.
- A local filename such as `official-*.svg` is not proof of authority by itself; authority comes from the current master manifest and identity-sync policy.

## Cross-chat coordination

- Tracked work follows the master claim lifecycle: `unclaimed → claim → heartbeat → complete/release → structured handoff`.
- If a different owner holds an active non-stale claim, stop instead of duplicating edits.
- A stale claim requires a freshness check before resuming/reclaiming work.
- A claim is coordination state only; it is never validation or release permission.
- Return branch/commit/evidence information so the master claim can be heartbeated, completed or released correctly.

## Structured handoff

For tracked work, return:
- task ID and repository;
- owner/role and branch;
- outcome;
- commits and areas changed;
- validations that **actually ran** and their real result;
- evidence status: `verified`, `inferred`, or `blocked`;
- remaining work;
- blockers/manual evidence still required;
- suggested next owner/role when useful.

Never list a planned build/test/device/store check as completed validation. If required validation was not performed, hand the task back with that work pending instead of claiming completion.

## State truth before status/release claims

Before answering or handing off any question about what is current, released, published, ready, or pending, resolve four independent dimensions from current evidence:

1. **Working HEAD** — current branch/default-branch SHA and relevant PR/branch state.
2. **Latest stable release** — actual published tag/version, publication timestamp, release commit/artifacts.
3. **Same-SHA CI** — validation for the exact commit and affected distribution channel.
4. **External provider state** — Store/Play/cloud/device/provider evidence such as uploaded, certified, published or delivered.

Never collapse these into one status. A newer `main` does not make the latest release newer. A built MSIX/APK/installer is not a Store/Play publication. A GitHub release is not provider publication. If `main` is ahead of the stable release, say so explicitly.

After a merge, release, tag or provider mutation, re-read authoritative state before the final status answer or handoff. If a generated queue/gate conflicts with newer evidence, route reconciliation instead of repeating completed product work.

## Multi-channel distribution and contract-test robustness

- If an app ships through more than one channel (for example direct GitHub Setup/Portable plus Microsoft Store MSIX, or direct APK plus Google Play), treat each channel as a separate validation surface over the shared source.
- A change to shared runtime, version, packaging, updater or identity code must run the affected channel validations on the **current commit**. A Store/Play workflow that only runs on a special distribution branch is insufficient once its shared implementation lives on the default branch.
- Store-managed builds must not also self-update from the direct-download feed unless the product explicitly documents and validates that dual-update design.
- Package creation is not provider publication. Keep evidence states separate: package built/validated → uploaded → certified/approved → published → delivered/installed.
- One authoritative product version must drive all channels. Never use a previous real release number as a runtime/version fallback because it can silently report stale identity; derive from authoritative metadata, fail clearly, or use a neutral non-release sentinel.
- Contract/regression tests must be portable across CI platforms. Normalize or tolerate CRLF/LF and path-separator differences and prefer structural/semantic assertions over exact whitespace or source-format matches.
- Syntax-check executable test/validation scripts before relying on them as semantic gates (for example `node --check` or `python -m py_compile` where applicable).
- When a channel is added or materially changed, update the app audit log and the master store/release gate. Do not describe the channel as published until provider evidence exists.

## Release and evidence boundary

- Do not publish/release unless explicitly authorized.
- Manual/device/store/provider validation remains pending until actually performed.
- Do not fabricate screenshots, device behavior, store status, cloud/provider state or test results.
- Preserve unrelated known-good behavior and keep changes bounded to the assigned task.
- For installable Windows apps, the canonical direct release is built by GitHub Actions from the authorized commit/tag and delivers a real Setup installer as the normal-user artifact.
- Use `<Product>-Setup-vX.Y.Z.exe` for the recommended installer. If a portable build is also shipped, name it explicitly `<Product>-Portable-vX.Y.Z.exe`; never leave the portable filename ambiguous when both exist.
- For installable Windows apps, separate evidence into **BUILD PASS → INSTALL PASS → LAUNCH PASS → FUNCTIONAL PASS**. A green installer/build job is not proof that the installed application starts.
- Smoke-test the generated Windows installer by actually installing it and then **launching the executable from the installed location** before uninstalling. Merely verifying that the EXE exists is insufficient.
- Installed-app LAUNCH PASS requires either a normal GUI process that remains alive long enough to expose a real top-level window, or an app-owned deterministic smoke mode that boots the real installed UI/runtime path and emits explicit success evidence.
- If installed startup fails, preserve process exit/lifetime plus available app logs and Windows Application/.NET crash evidence before failing CI.
- A portable launch PASS and an installed-app LAUNCH PASS are separate claims when both artifacts are shipped.
- Publish SHA-256 for direct Windows binaries. Authenticode/code signing, when available, must happen before final checksum publication. Without a publisher certificate, do not hide or misrepresent Windows Unknown publisher/SmartScreen behavior.
- FoamLens and Michel's Life are the current Windows release references; Michel's Life also demonstrates optional Authenticode and a separate Microsoft Store MSIX path.

## P0 mandatory rendered-UI release gate (master-owned; every app)

- Source scans/XML assertions, build success, CI green, icon/blob hashes and file presence never prove the app **visibly renders**. Every release of a user-facing app MUST open the actual built/installed candidate and capture **Home + About + changed screens** across two supported viewport sizes; the capture must show meaningful nonblank content, visible author portrait, Michel's Lab logo and **`TOOLS WITH IDENTITY.`**, appropriate icons + network names, working scroll and Close/Back. Test native view bounds/visibility and navigation, not just inflation.
- Screenshots must originate from the actual runtime (Android emulator/physical device, Windows installed GUI, real browser), be checksum-validated and bound to the exact release source SHA plus candidate artifact SHA-256. A **human must inspect the rendered screenshots**, approve them in a protected GitHub Environment tied to that release, and record evidence. The app's release workflow MUST fail closed if capture, manifest validation or approval is missing, stale, blank, or rejected—even when the user asks to release or other CI jobs pass.
- Mandatory reference: `michels-lab/Michel-Software-Standards/standards/RENDERED_UI_RELEASE_GATE.md`; validator: `tools/visual_release_gate.py` (vendor/consume with tests). Never fabricate images, attestations or user approval; agents cannot self-certify independent visual review. Do not claim device/store testing from an emulator. A pure no-UI-change release needs documented same-artifact UI identity and bounded exception as defined in the standard.
- Release manager: block and open P0 on omitted/unreviewed or broken About/changed UI. QA agent: inspect actual pixels and record **SOURCE, BUILD, LAUNCH, RENDER, VISUAL REVIEW and DEVICE** independently. App maintainer: implement real UI smoke tests and screenshot capture at appropriate viewports. Static PASS is not visual PASS.
- **Important rollout boundary:** this shared contract does NOT, on its own, modify the child release workflow. Until that repository's workflow demonstrably includes capture + validated image evidence + protected approval, mark it **NOT ENFORCED / P0** and do not represent all apps as protected.
<!-- MICHELSLAB_SHARED_CONTRACT_END id=child-agent-core -->
