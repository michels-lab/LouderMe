# LouderMe Android Developer Edition — Granada 2026-10-20

**Status: candidate implementation in an isolated branch. Not published or validated on a physical Android device.**

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
