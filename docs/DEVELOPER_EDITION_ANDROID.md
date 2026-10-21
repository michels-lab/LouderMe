# LouderMe Android Developer Edition — Granada 2026-10-20

**Status: Developer source integrated on `main`, with real installed-emulator annotation/restart/audit/JSON and status-filter QA passed. The independently signed owner APK workflow is a NEW source candidate; it is not configured, executed, published, or physically device-tested.**

The Android Developer Edition is a third Gradle product flavor (\`developerDebug\`) installed **alongside** the ordinary \`play\` and \`sideload\` applications as \`com.michelslab.louderme.developer\`. It uses the same current Compose app screens, audio service and EQ implementation but private package-scoped SharedPreferences and application data. The launcher is labeled *LouderMe Developer Edition*. It does not import production settings, cookies, accounts, audio state journals or private files. Backups and boot receiver are disabled; public release update prompts, checks and installation actions are disabled for the developer package.

## Real screen annotation flow

Open Developer Edition. Use the permanent **DEV · Audit / Notes** button, select **Select section / Add note**, then tap an actual on-screen section/control. An intercepting developer-only full-screen touch overlay consumes the tap, so it cannot accidentally alter volume, enable boost or activate a public link. The precise screen and stable section ID are captured from the **actual measured Compose bounds** (not screenshot inference or a detached list). Anchors exist for Home header, updater, volume, boost, each of seven quick levels, fine gain, EQ, all seven EQ frequency controls, engine diagnostics and About header/profile/build details. The owner enters an exact text observation, severity, status; it survives restarts, and can be edited, located or deleted.

The inspection overlay is disabled on the public variants. It allows Back to cancel; saved notes not matching the known live section ID are reported unresolved rather than migrated to an unrelated control.

## Safe audits and export

The inspector provides **Run Audit** for read-only state validity: 100–250 target gain, seven-band EQ array, annotation storage availability, stale anchor identities and an explicit hardware-audio-unverified finding. It does not test hardware output, read audio samples or change audio configuration itself.

**Export JSON** and **AI repair prompt** both contain automatic findings plus the exact owner notes with screen, element locator, comment, timestamps, severity and status. Export opens a review/redaction editor first, followed by Android's explicit ACTION_SEND chooser for manually sharing reviewed *text*. It does not upload automatically, log passwords, store audio captures or synthesize a fake signed-in identity. LouderMe has no ordinary public account sign-in to mirror at this time.

## Acceptance gates

- Android CI must compile/test all three variants on exact branch head, enforce distinct package identity, ensure Developer APK has no REQUEST_INSTALL_PACKAGES and preserve Play/Direct permission split.
- Unit test checks declared Home/About + seven EQ + seven quick targets and distinct developer/public package identity.
- Installed/emulator/phone validation must confirm actual developer launcher, pointer-based note selection without triggering Boost/EQ actions, persistence across close/restart, navigation/locate, audited report and reviewed Android share.
- No source tests alone can confirm audio processing changed the sound output of external applications or acoustic sound pressure level. No release publication authorized.

## Granada tracking

Windows Developer Edition: independent PR #78. Android Developer Edition: separate workstream under issue #77; this document tracks its branch. Native Windows DSP/virtual device remains #46/#48, final shipping binary UI QA #33, Android signing continuation #40. Skip blocked work rather than mark it completed.

## Granada continuation — installed reliability and public coexistence (2026-10-20)

Earlier PR head ba3002a passed Android Play/Direct/Developer compilation and unit tests, but the installed emulator encountered a **Pixel Launcher** ANR above both normal and developer apps. Separate QA PR #81 implemented a narrowly scoped system-only launcher recovery and its real Android UI capture passed on exact QA head before integration to `main` (`d2833bc`).

The Android developer test harness now imports that same **bounded real native recovery** (once per invocation) and still requires the true Developer UI. It launches the actual activity class `com.michelslab.louderme.MainActivity` in the separate `.developer` package. CI additionally installs the real public sideload APK beside Developer and checks both independent package identities, without reading any public preferences. Actual annotation selection, save/restart persistence, local read-only audit and reviewed JSON remain required; failed runs keep an unqualified diagnostic screenshot and never write a success manifest.

A Compose ordering bug was corrected: activate Home/About before registering measured bounds, not in a late SideEffect that could clear anchors after layout. Hermetic Python tests cover true developer UI after exact launcher recovery, public UI rejection, and proper package/activity separation. **These changes are unverified until exact-head CI completes.** Physical Samsung use and actual audible changes remain separate.

## Granada continuation — owner-note status filtering (2026-10-20)

The **Developer Edition inspector only** now offers horizontally scrollable `All / Open / Investigating / Resolved` status filters, displays the visible/total count and keeps each note's exact `Edit / Locate / Delete` actions. Filters never modify the saved notes; JSON/AI repair prompt continue exporting **all** owner observations (including resolved) unless the owner deliberately redacts the reviewed export. Core public audio settings, native App screens and release/update paths are unchanged.

The shared pure filter function has Android unit regression coverage for each status, an unknown status, immutable note counts, preserved timestamps and verbatim observations. The installed Android Developer E2E now also taps the real Resolved filter, requires its visible/total label and absence of the Open note, then taps All and requires the same verbatim note to reappear before the persistence/restart/audit test. Physical owner-phone behavior remains separate from emulator evidence.

## Granada continuation — persistent signed Developer installation channel (2026-10-20)

A separate owner-triggered private-signing workflow TEMPLATE has been prepared at `infra/private-developer-signer/.github/workflows/developer-android-signed-owner-qa.yml`, intended for a separate private signer repository because LouderMe source is PUBLIC, documented in `docs/DEVELOPER_ANDROID_SIGNED_CHANNEL.md`. It requires four **Developer-only** keystore secrets and an independently pinned public certificate SHA-256; it explicitly does not access the public sideload signing identity or create a GitHub Release. It builds N-1/N Developer release APKs, signs both with the same pinned identity and requires actual emulator **in-place** update with the same persisted owner note and reviewed export. It retains the signed final candidate for seven days **only if** installed QA passes.

The normal 3-flavor CI also compiles the Developer unsigned release APK and checks these boundary rules. This workstream is **not** a signed APK already created or available. Existing debug-signed Developer apps require careful note export before a signer switch because Android prohibits in-place updates with incompatible certificates. Physical owner-device acceptance remains pending.

## Granada — reversible annotation backup and migration between signing identities (2026-10-20)

Developer now exposes **Save notes backup** and **Restore notes backup** inside the existing *DEV · Audit / Notes* panel. These use Android's native Storage Access Framework (CreateDocument/OpenDocument), so the owner chooses the exact destination/source and no app-wide storage permission, automatic upload, or public LouderMe data access is needed. A backup is UTF-8 JSON (`michelslab-developer-note-backup-v1`) containing all original exact owner comments, stable IDs/locators, status, severity and timestamps. The user must choose a trusted file provider; selecting a cloud-backed document provider may synchronize those private comments.

A restore accepts a backup or an earlier full Developer JSON report **only when the product/edition/platform and separate `com.michelslab.louderme.developer` package identity match**. Reads are limited to 2 MB and 250 validated annotations; duplicate IDs, unsupported fields, broken timestamps and foreign-product reports are rejected atomically without writing anything. Before changing storage, the owner sees counts of new, identical, conflicting and outdated-locator notes. **Import new only** never overwrites matching IDs; **Replace matching IDs** is a distinct, deliberate confirmation, and neither mode deletes notes missing from the imported backup. Unknown old locators are preserved exactly and disclosed in read-only audit. Local commits are synchronous; on failure original notes remain.

**Correct signer-migration procedure:** while the OLD debug-signed Developer app still opens, save a note backup and confirm that file exists in a location that survives uninstall; optionally retain an independent reviewed JSON report. Only then deliberately uninstall the old debug-signed package, install the separately signed permanent Developer APK when available, and use Restore notes backup. Android **cannot perform an in-place upgrade across mismatching signing certificates**; this manual file transfer does not claim otherwise. Do not expect audio/user settings or accounts to migrate, and do not uninstall until you have verified the external backup. The ability to restore is supported by deterministic parser/merge tests and real installed UI control checks; it is **not yet a physical device file-round-trip acceptance**.
