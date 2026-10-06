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

## Validation

Inspect the current workflows and run the strongest relevant Gradle/build/test path for the changed flavor/surface. Validate manifest/permission separation when updater/distribution code changes.

Device-only audio quality, output routing, PackageInstaller UX and Play-delivery behavior remain explicitly unvalidated until exercised on the target device/channel.

## Completion

Update `PROJECT_LOG.md` with meaningful fixes, decisions, failures, tests and release evidence. Infrastructure changes also update `docs/INFRASTRUCTURE_AUDIT.md` when relevant.

Do not bump versions or publish releases unless explicitly assigned.
