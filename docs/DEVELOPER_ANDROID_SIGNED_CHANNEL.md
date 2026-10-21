# LouderMe Android Developer — independent signed installation (Granada)

**Status: public-source candidate containing a signing-workflow TEMPLATE intended for a separate PRIVATE GitHub repository. The LouderMe source repo is PUBLIC. No Developer signing Secrets/pin were configured here; no APK was signed, no private signing workflow was executed, and no physical phone was tested.**

## Why the separate Developer signer matters

Gradle's normal `developerDebug` APK uses a runner/debug signer. Different machines/runs can use different debug certificates. Android only upgrades an installed package in-place if its package ID and signing identity remain compatible and the new versionCode increases. Debug builds are useful for emulator CI, but they **do not provide a durable owner installation** or safe updates.

The dedicated `developer` flavor remains `com.michelslab.louderme.developer`, separate from the public `com.michelslab.louderme`. The signing workflow TEMPLATE at `infra/private-developer-signer/.github/workflows/developer-android-signed-owner-qa.yml` uses **only** a separate owner-managed Developer keystore; it never accesses `LOUDERME_SIDELOAD_*` secrets or publishes a public release. Public Play/Direct Android versionName `0.1.12` and versionCode `13` remain unchanged.

## Permanent owner setup (must be performed securely outside source control)

The same Developer certificate and key must be preserved securely across ALL subsequent signed Developer versions. Create a dedicated private keystore using the normal Android/JDK `keytool` process on a trusted computer; **never reuse the stable public LouderMe Direct key**. Back up the key in a secure password manager or encrypted offline vault. Losing it means updates of the already-installed Developer package cannot continue without reinstall and potential note loss.

Create a separate private Michel's Lab signing repository and copy the reviewed template to its root `.github/workflows/developer-android-signed-owner-qa.yml`. In the **PRIVATE signing repository's** **Settings → Secrets and variables → Actions**, configure **four separate repository Secrets**:

- `LOUDERME_DEVELOPER_KEYSTORE_BASE64` — Base64 contents of the keystore, NOT an existing public Direct keystore
- `LOUDERME_DEVELOPER_STORE_PASSWORD`
- `LOUDERME_DEVELOPER_KEY_ALIAS`
- `LOUDERME_DEVELOPER_KEY_PASSWORD`

Configure one repository **Variable** `LOUDERME_DEVELOPER_CERT_SHA256`: exactly 64 hexadecimal characters from the SHA-256 digest of the DER-encoded public certificate exported by that Developer keystore. The workflow checks the certificate from the keystore **and** both finished APK signers against this value. It fails closed on missing data or mismatch. Do not share passwords, private key material, encoded keystore or screenshots of secrets in chat/issues/commits.

**Critical public-repository boundary:** GitHub reports `michels-lab/LouderMe` as PUBLIC. Never configure Developer signing Secrets or distribute private owner-signed APKs in that public source repository. This source repo keeps only the reviewed workflow TEMPLATE and unsigned CI. The available GitHub connector cannot create the separate private signer repository or provision its Secrets; authorized owner/admin setup remains required. No signed APK can be claimed solely because this template exists.

## Controlled workflow

The signing workflow is deliberately NOT executable from the public product repository. Only after copying the template into a separate PRIVATE signing repository will it appear there as **PRIVATE signing runner - LouderMe Developer Android (owner opt-in)**. It is *opt-in* (`workflow_dispatch`), runs only on `main` in a **private** repository, and takes the exact 40-character public LouderMe `source_commit_sha` (required to be reachable from the source repo `main`) and an independent Developer `developer_version_code` in the range **10001..2099999999**. The owner should choose a strictly higher value than the last installed dedicated Developer APK, e.g. initial `10001`, then `10002`. The private workflow checks out that exact pinned source commit and rejects code outside public LouderMe `main`. It never chooses a new key, overwrites the public APK, triggers the existing release publisher or changes the official public update manifest.

It performs:

1. Build an **unsigned Developer release** APK with versionCode N-1 and another with N, as well as a disposable public **debug** APK for co-installation in the emulator.
2. Verify that both real Android manifests declare the independent `.developer` package and correct incremental versionCodes, and that the public debug package retains its current public identity/code. The Developer must not request APK installer permission.
3. Use the same dedicated private Developer key to zipalign, sign and `apksigner verify` both APKs, comparing **both** signer certificate hashes to the pinned Developer certificate. The key is not uploaded with the artifacts and is deleted from the runner after signing.
4. Install the previous **signed** Developer APK in an Android emulator, select a real Compose section, save a verbatim note and exercise status filtering; install the new signed Developer APK **in place** without uninstalling, check that its installed versionCode increased and the note is still present; run its read-only audit and review JSON with the note. Preserve source-hash/APK-hash-linked screenshots or real failure pixels.
5. Only after all steps pass, upload a seven-day `LouderMe-Developer-QA-PASSED-...` Actions artifact containing the **current signed Developer APK**, its SHA-256 checksum and pinned certificate hash. The earlier unverified staging artifacts expire after three days. There is **no GitHub Release or public installation link**.

This validates update continuity between two same-source version codes; it does **not** prove migration across future code/schema changes, real Samsung behavior, hardware gain, signed Windows releases, or Google Play distribution. Each subsequent source update must undergo the same signed installed checks, and the owner must test on the target phone.

**Important migration boundary:** An already installed `developerDebug` APK is signed with a different key and cannot be upgraded in place into this new dedicated signer. If notes exist there, manually export and safely retain them **before** any uninstall. This workflow does not attempt to import prior notes, silently clear data, or bypass Android signer compatibility. Do not mistake a successful install on a fresh emulator for proof that old debug-installed notes survived a signer change.

## Acceptance before owner installation

Keep issue #77 open until the owner explicitly provisions the separate key/pin, the manual signed workflow passes on the exact desired source commit, the owner's approved distribution channel is set up, and the signed installed APK is tested on the real device. Hardware sound/Bluetooth and native Windows endpoint issues #46/#48 remain independently open. The public stable version is not bumped or republished by this infrastructure work.

## Avoid annotation loss when moving off a debug signer

A different Android signing identity cannot preserve installed package data through a normal APK update. **Before uninstalling** an existing debug-signed `.developer` install, open the Developer audit panel and choose **Save notes backup** if available. Older builds without that button can use **Export JSON → Share reviewed report**, keeping the whole reviewed JSON including `ownerAnnotations` in a trusted file. The new Developer importer accepts both a backup and a correctly identified old full Developer report. Confirm that the external file exists and survives uninstall. Consider separately reviewing and exporting the full read-only JSON report for diagnosis. After the owner-approved permanently signed Developer APK is available and installed, use **Restore notes backup** on that app, review the preview counts, and confirm import of new notes. Existing note-ID conflicts require a separate explicit replace action, never an automatic overwrite. All annotation timestamps and original section locators are retained; outdated locators remain diagnosable.

The backup contains verbatim owner observations that may include private information. Android's system file picker can target cloud-backed folders; use a destination that meets your privacy needs. No public LouderMe settings or installed audio configuration are part of the backup. A validated parser and in-app restore controls do **not** prove a successful physical Samsung file-round-trip until the owner actually performs one.
