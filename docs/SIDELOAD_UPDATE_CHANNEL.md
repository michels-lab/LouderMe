# LouderMe Direct / Sideload Update Channel

## Goal
A LouderMe APK installed outside Google Play should detect a newer official version automatically, download it, verify it, and prepare installation without requiring the user to manually browse GitHub.

Android still owns final installation approval.

## Public feed
The source repo is private. The update feed is public and contains no source code:

`realmichelduarte/michel-s-life-releases/louderme/latest.json`

The manifest provides:
- versionCode;
- versionName;
- APK URL;
- SHA-256;
- release notes.

## Verification
A candidate update is accepted only when:
1. SHA-256 matches the official manifest;
2. package is `com.michelslab.louderme`;
3. candidate versionCode matches the manifest;
4. versionCode is newer than the running app;
5. signing certificate matches the installed sideload signing identity.

Android PackageManager then performs its own signature/update checks as well.

## Install flow
- LouderMe auto-checks.
- New version auto-downloads.
- LouderMe verifies it.
- If Android has not granted "install unknown apps" for LouderMe, the app opens the system permission screen.
- On return, LouderMe creates a PackageInstaller session.
- Android displays the required install confirmation.
- The update replaces the existing app only if Android accepts the signature and package identity.

## Why v0.1.4 is a one-time reinstall
v0.1.3 and earlier GitHub releases were debug builds produced on hosted CI.

A hosted debug signing key is not a stable production identity. Android does not allow an APK signed by a new key to update an already-installed APK signed by the old key.

v0.1.4 establishes a dedicated stable sideload key. Therefore the currently installed old debug build must be uninstalled once before installing the stable v0.1.4 sideload baseline.

After that, same-key sideload releases can update in place.

## Key handling
The sideload private key:
- is not committed to Git;
- is not stored in the public feed;
- must be backed up securely;
- must be configured in CI secrets for future automatic releases.

Losing the key breaks same-package sideload updates.


## Bootstrap status

The stable sideload signing identity is bootstrapped exactly once from the designated v0.1.4 release-candidate commit. The resulting private handoff artifact is temporary and must be moved into GitHub Actions secrets plus a secure offline backup before merging v0.1.4 to main.


## Canonical sideload signing identity

Established by bootstrap workflow run `37392415939`.

- Certificate SHA-256: `4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3`
- Certificate DN: `CN=Michel's Lab, OU=Software, O=Michel's Lab, L=Saltillo, ST=Coahuila, C=MX`
- RSA: 4096-bit
- Verified APK signature scheme: v3

The fingerprint is public identity metadata. The private key and passwords remain secret.

The first stable signed baseline APK has SHA-256:
`7eee11c065330d0064378172840cdb5aa067c17463332db8a6c6626e674f9d83`.
