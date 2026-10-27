# GRANADA — Windows Developer Edition owner notes backup and safe restoration

Date: 2026-10-27
Area: issue #77, Windows Developer Edition only. This is SOURCE work, not a public release.

## Existing behavior verified

- The actual Windows Developer Edition launches from an independent Start Menu shortcut (`--developer`), uses the *same real WinForms engine* and local `LouderMe.Developer` storage, and already supports live control inspection, annotations, read-only APO audit and human-review-before-export JSON/AI prompt.
- Android Developer Edition already has a limited-size JSON notes backup/restore preview and conflict review; Windows previously had **no equivalent backup/restore UI**. Existing Windows notes lived in `%LOCALAPPDATA%/Michel's Lab/LouderMe.Developer/annotations.json`. A reinstall/machine change or inability to read that file could leave notes unrecoverable. Developer signing/migration may differ between systems.
- The public LouderMe app does not offer a provider login/account/workspace screen; Developer must **not** fabricate one. There is no audio hardware proof in this task.

## Implemented (3 linked functional capabilities)

**A — Windows owner backup:** real launched Developer Diagnostics UI now provides **Backup notes** via Windows SaveFileDialog. Warns explicitly that verbatim comments may contain personal information and destination folders can sync to a cloud. No automatic upload, permission escalation, production settings access or forced export. File is Windows-platform Developer-only JSON with exact target locator, timestamps, comment, severity and status, UTF-8. Before opening the picker, validates at most 250 records / 12,000 characters per comment / 2 MB total output, preventing backups that cannot be restored.

**B — safe, reviewable restore:** **Restore notes** opens a user-selected JSON (never silently imports). Rejects wrong product, wrong platform, public edition, bad schema, duplicate GUIDs, unsupported severity/status/screen, malformed timestamps, missing/blank text or oversized documents. Previews number of **new**, **exact duplicates**, and **same-ID conflicts**. The user must explicitly choose: import **new only**, replace notes with the **same existing IDs**, or cancel. Notes missing from a backup are **never deleted**, and the original list is not mutated before persistence succeeds. No old backups are ever silently merged across Android/Windows locator formats.

**C — durable corruption safety and regression:** the existing Windows `LoadNotes` / `SaveNotes` now run the same semantic validation before reading or overwriting real files. A semantically malformed but JSON-valid local file is preserved and editing is disabled; a rejected save cannot destroy existing content. Existing atomic temp-file rename retained. The normal `--check-developer-contract` now runs actual Windows C# JSON roundtrip with multiline/Unicode verbatim comments, conflict resolution, 13 adversarial import controls, public edition access denial, corrupted file preservation and timestamp validation. The installed Developer smoke check remains required for a real launch.

## Important acceptance distinctions

- Implemented in a branch is not released or user-accepted; require **exact-head Windows build**, `--check-developer-contract`, installed `--developer-smoke-test`, runtime Home/About/branding and Android installed/Developer non-regression, prior to merge.
- No release bump; stable public Windows 0.1.17 + Android 0.1.12 remain unchanged. Do not claim owner observed feature on their PC.
- Pending issue #77 scope still includes broader automatic runtime/renderer audit, per-account/workspace logic **only if real public authentication is later implemented**, and end-user Developer installer UX acceptance. Android backup continues to use Android's platform-bound schema. No user files, credentials, accounts or private notes are accessed by these test fixtures.
