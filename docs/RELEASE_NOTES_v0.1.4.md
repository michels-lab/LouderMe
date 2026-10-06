# LouderMe v0.1.4 — Direct Auto-Update Foundation

v0.1.4 establishes the permanent Michel's Lab direct/sideload update channel for Android builds installed outside Google Play.

## What changes

Direct builds now:
- check a public Michel's Lab update manifest automatically;
- compare versionCode against the installed build;
- automatically download a newer official APK;
- verify SHA-256;
- verify package ID;
- verify the downloaded APK version;
- verify Android signing identity against the installed LouderMe build;
- use Android PackageInstaller for the final update handoff;
- resume the pending update after the user grants Android's "install unknown apps" permission.

Google Play builds continue to use Play In-App Updates and do not request `REQUEST_INSTALL_PACKAGES`.

## Stable signing

v0.1.4 establishes a dedicated stable signing identity for the Michel's Lab direct channel.

Older GitHub APKs were debug-signed by hosted CI and do not have a permanent signing identity. Android therefore cannot treat the old debug install and the new stable-signed install as the same update chain.

**One-time migration:** uninstall the old GitHub/debug LouderMe build and install the stable v0.1.4 sideload APK once.

After that baseline is installed, future same-key direct releases can update in place through LouderMe.

## Public feed

Source stays private.

The update feed and signed direct APKs are published through the public Michel's Lab release hub:

`realmichelduarte/michel-s-life-releases/louderme/`

No private GitHub token is embedded in the app.

## Still true

- Global Boost 100–250% remains.
- 7-band EQ remains.
- IG Cleaner-family Audio Workspace remains.
- Android still requires system/user confirmation for a normal APK update; LouderMe cannot silently replace itself like a privileged system app.
