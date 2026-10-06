# LouderMe Direct / Sideload Update Channel

## Goal

A LouderMe APK installed outside Google Play should discover a newer official version automatically, download it, verify it, and hand the verified update to Android without requiring the user to browse GitHub manually.

Android still owns the final installation approval.

## Distribution split

LouderMe has two explicit Android distribution flavors:

- `play` — Google Play In-App Updates; does not request `REQUEST_INSTALL_PACKAGES`.
- `sideload` — Michel's Lab Direct; checks the public release feed and can request Android's per-source APK-install permission.

The source repository remains private.

## Public update feed

The public feed is:

`realmichelduarte/michel-s-life-releases/louderme/latest.json`

The feed contains only distribution metadata:
- package ID;
- versionCode;
- versionName;
- public APK URL;
- SHA-256;
- short release notes.

Current public baseline:

- version: `0.1.4`
- versionCode: `5`
- public tag: `louderme-v0.1.4`
- APK: `LouderMe-v0.1.4-sideload.apk`
- public APK SHA-256: `7c4231e718fba60a74df0717a68f0f5ff0b9273b79194de501ffc41b4386958a`

No LouderMe source code or private GitHub credential is exposed through this channel.

## Verification contract

A direct-update candidate is accepted only when all of the following pass:

1. the public manifest reports a newer versionCode;
2. downloaded SHA-256 matches the official manifest;
3. Android package ID is `com.michelslab.louderme`;
4. APK versionCode matches the manifest;
5. APK versionCode is newer than the running app;
6. candidate signing identity matches the installed stable sideload identity.

Android PackageManager / PackageInstaller then performs its own package and signature validation as a second enforcement layer.

## Install flow

1. LouderMe checks the public manifest automatically.
2. A newer APK is downloaded.
3. LouderMe verifies checksum, package, version and signing identity.
4. If Android has not granted "install unknown apps" to LouderMe, the system permission screen is opened.
5. LouderMe resumes the pending verified update.
6. LouderMe streams the APK into an Android PackageInstaller session.
7. Android presents the required user confirmation.
8. Android replaces the installed app only if its own security checks pass.

A normal Android app cannot silently replace itself without system/device-owner/root privileges. LouderMe therefore automates discovery, download and verification while leaving the final install confirmation to Android.

## Canonical stable sideload signing identity

The **authoritative** Michel's Lab Direct signing identity established for the final v0.1.4 release is:

- Certificate DN: `CN=Michel's Lab, OU=Software, O=Michel's Lab, L=Saltillo, ST=Coahuila, C=MX`
- Certificate SHA-256: `e0d497f4872c116632f040e51e08b7beb410f0a3df4d2b959d22fbd7bde48479`
- RSA: 4096-bit
- Verified APK signature scheme: v3

Any earlier bootstrap fingerprint recorded during v0.1.4 development is superseded by this final identity.

The final private/main v0.1.4 APK and the public v0.1.4 APK were independently built and therefore have different whole-file SHA-256 values, but both were verified to use this same signing certificate. Android update continuity depends on package/signing identity, not byte-for-byte APK equality.

## Why v0.1.4 needs one reinstall

v0.1.3 and earlier GitHub builds were CI debug-signed. Their signing identity is not the new permanent Michel's Lab Direct identity.

Android will not install an APK signed by a different identity as an in-place update of the existing package.

Therefore migration is intentionally one-time:

1. uninstall the old direct/debug LouderMe;
2. install the stable signed v0.1.4 sideload baseline;
3. allow LouderMe to install updates when Android asks;
4. future same-signer Michel's Lab Direct releases can update in place after Android confirmation.

## Signing-key continuity

The private sideload key is never committed to source and is never published in the update feed.

Normal CI signing uses one permanent identity. The workflow:

1. prefers the repository's configured signing Secrets when they validate correctly;
2. falls back to the private GitHub Actions signing-vault artifact if Secrets are missing or stale;
3. validates the recovered keystore before signing;
4. refreshes the private vault after successful signing;
5. runs a scheduled monthly refresh so the fallback does not expire unnoticed.

The fallback exists because the connected GitHub tooling cannot repair repository Secrets directly. It is a continuity mechanism, not a reason to expose the signing material.

Losing all copies of the canonical key would break in-place Michel's Lab Direct updates.

## Public/private release boundary

The private source repo can publish its own validated release asset, while the public `michel-s-life-releases` repository hosts the anonymous-download APK and `latest.json` used by installed sideload clients.

This separation keeps:
- source private;
- signing key private;
- binary/update metadata public;
- shipped APK free of private GitHub tokens.
