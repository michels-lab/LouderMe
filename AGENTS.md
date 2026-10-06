# LouderMe — Agent Contract

LouderMe is a dual-platform audio utility: Android plus a native Windows Desktop app. Android has separate Google Play and Michel's Lab Direct/sideload distribution paths; Windows has its own installer/portable stable channel. Before editing, read `.michelslab/project.yml`, `MICHELS_LAB_PROJECT.md`, `PROJECT_LOG.md`, `docs/INFRASTRUCTURE_AUDIT.md`, and the platform-specific workflows/build configuration that own the affected behavior.

Shared Michel's Lab rules live in `realmichelduarte/Michel-Software-Standards`.

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

About MUST be intentionally designed in this order:

1. **Product identity first** — approved LouderMe mark/lockup, product name, real current version and product-facing composition derived from the app identity.
2. **About the author** — current canonical Michel Duarte portrait, **Michel Duarte**, and appropriate developer copy.
3. **Michel's Lab parent brand** — official Michel's Lab mark/lockup shown as the studio/ecosystem identity without overpowering LouderMe.
4. **Social profiles** — each visible network link shows the recognizable network icon **and** the visible network name together, using canonical URLs from the master `brand/developer-profile.json`.

Do not finish About with text-only social links or icon-only social buttons. Accessibility labels/tooltips supplement the visible network name; they do not replace it.

Treat this hierarchy and the product-wide logo-derived design language as part of product completeness. Visual work must not regress it.

Follow `standards/PRODUCT_IDENTITY_STANDARD.md` and `standards/ABOUT_STANDARD.md` in `realmichelduarte/Michel-Software-Standards`.

## Validation

Inspect the current workflows and run the strongest relevant validation for the changed platform: Gradle/build/tests plus manifest/permission separation for Android; .NET build, APO-contract checks, portable/installer packaging and updater/feed integrity for Windows.

Device-only Android audio quality, output routing, PackageInstaller UX and Play-delivery behavior remain explicitly unvalidated until exercised on the target device/channel. Windows APO endpoint attachment, reboot behavior, SmartScreen/AuthentiCode state and audible 100–250% behavior remain unvalidated until exercised on a real Windows machine.

## Completion

Update `PROJECT_LOG.md` with meaningful fixes, decisions, failures, tests and release evidence. Infrastructure changes also update `docs/INFRASTRUCTURE_AUDIT.md` when relevant.

Do not bump versions or publish releases unless explicitly assigned.


## Intelligent brand adoption

When the user asks to update/adopt the app logo, icon, splash, startup or About:

- use the canonical product assets from `realmichelduarte/Michel-Software-Standards/shared-assets/product-logos/`;
- follow `standards/PRODUCT_IDENTITY_STANDARD.md` and `standards/BRAND_ADOPTION_PLAYBOOK.md` from the Michel-Software-Standards repository;
- inspect this app's current design system before placing assets;
- replace the real active platform identity references instead of layering the new logo over legacy/generic branding;
- use the app icon for launcher/executable/favicon derivatives, the mark for compact identity, and the lockup for larger splash/About surfaces when appropriate;
- treat the logo geometry as design language where useful, but do not repeat the literal logo across screens;
- build About in the hierarchy Product → Author → Michel's Lab → Social;
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

<!-- MICHELSLAB_SHARED_CONTRACT_BEGIN id=child-agent-core version=2026-10-06.1 -->
# Michel's Lab shared child-agent contract

This managed block is cross-project policy. Repository-specific instructions may add stricter local rules outside this block, but they must not weaken or contradict it.

## Shared authority

- Michel's Lab shared standards, product identity, governance, release and coordination rules are authoritative in `realmichelduarte/Michel-Software-Standards`.
- Keep product implementation truth and product-specific audit logs in this child repository.
- Do not silently invent a conflicting local Michel's Lab rule.
- Never commit secrets, credentials, signing material, private tokens or passwords.
- Historical green CI is not proof for the current commit.

## Product identity / About

- Preserve the approved product-logo geometry; contextual color, material, lighting and motion may adapt without identity drift.
- Branding is a design language, not sticker placement.
- About hierarchy is **Product identity → About the author → Michel's Lab → social profiles**.
- Use the current canonical Michel Duarte portrait and the official Michel's Lab parent-brand assets from the master authority when implementing/updating About.
- Visible social controls use recognizable network icon **and** visible network name with canonical profile URLs.

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

## Release and evidence boundary

- Do not publish/release unless explicitly authorized.
- Manual/device/store/provider validation remains pending until actually performed.
- Do not fabricate screenshots, device behavior, store status, cloud/provider state or test results.
- Preserve unrelated known-good behavior and keep changes bounded to the assigned task.
<!-- MICHELSLAB_SHARED_CONTRACT_END id=child-agent-core -->
