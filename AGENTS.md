# LouderMe — Agent Contract

LouderMe is an Android audio utility with separate Google Play and Michel's Lab Direct/sideload distribution paths. Before editing, read `.michelslab/project.yml`, `MICHELS_LAB_PROJECT.md`, `PROJECT_LOG.md`, `docs/INFRASTRUCTURE_AUDIT.md`, and the workflows/Gradle configuration that own the affected behavior.

Shared Michel's Lab rules live in `realmichelduarte/Michel-Software-Standards`.

## Product constraints

- Preserve separate `play` and `sideload` behavior. Do not leak `REQUEST_INSTALL_PACKAGES` into the Play flavor.
- Michel's Lab Direct updates require a stable signing identity, public anonymous feed, SHA-256 verification, package/version validation and signing continuity before Android receives the APK.
- Never replace stable release signing with ephemeral CI debug signing.
- Do not describe target signal gain as measured acoustic dB SPL.
- Equalizer/product-facing bands must remain stable while device-specific band mapping/clamping stays explicit.
- Foreground app chrome must honor Android system insets/safe drawing; do not fix status-bar collisions with one-device magic padding.
- Preserve the current IG Cleaner-family visual language unless redesign is explicitly requested.
- High-gain and EQ audible quality require real target-device validation; repository tests cannot prove acoustic behavior.
- Never commit signing keys/passwords, Play credentials or privileged service secrets.

## Official product identity — mandatory

LouderMe uses the approved Michel's Lab canonical flowing-waveform logo geometry from `realmichelduarte/Michel-Software-Standards`.

**Do not implement branding by simply pasting the source SVG into screens.** The logo is a design language, not a sticker.

Protected identity:
- preserve the defining waveform silhouette, proportions and spatial relationships;
- do not stretch, skew, redraw into another symbol, or alter the geometry until it stops reading as the approved LouderMe mark.

Adaptive expression is expected:
- color may adapt to theme/context;
- monochrome, inverted, glow, glass, outline, translucent and animated treatments are allowed;
- mark-only and mark + product-name compositions are allowed where appropriate;
- the flowing-waveform visual DNA should inform relevant volume/EQ visualization, activity states, separators, loaders and motion.

A screen can be correctly branded without displaying the full logo. Prefer integrated audio visual language over repeated logo placement.

Follow `standards/PRODUCT_IDENTITY_STANDARD.md` in the master standards repository as the authority.

## About identity — mandatory

About is a primary LouderMe brand surface, not a plain metadata/settings page.

It MUST intentionally combine:
- the approved product mark/lockup with prominent visual presence;
- the current canonical Michel Duarte portrait;
- Michel's Lab / developer identity;
- social links using **both the recognizable network icon and the visible network name**.

For social links, render icon + label together (for example Instagram icon + `Instagram`, GitHub icon + `GitHub`). Do not use text-only rows as the finished design, and do not use icon-only controls without a visible/accessibility label.

Use the canonical URLs from the master `brand/developer-profile.json`. Treat the portrait, logo, social controls and metadata as one coherent branded composition derived from the product's visual language.


## Validation

Inspect the current workflows and run the strongest relevant Gradle/build/test path for the changed flavor/surface. Validate manifest/permission separation when updater/distribution code changes.

Device-only audio quality, output routing, PackageInstaller UX and Play-delivery behavior remain explicitly unvalidated until exercised on the target device/channel.

## Completion

Update `PROJECT_LOG.md` with meaningful fixes, decisions, failures, tests and release evidence. Infrastructure changes also update `docs/INFRASTRUCTURE_AUDIT.md` when relevant.

Do not bump versions or publish releases unless explicitly assigned.
