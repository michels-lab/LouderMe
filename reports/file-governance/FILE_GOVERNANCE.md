# File steward: repository file audit

All tracked files classified; Markdown/text docs inspected with size cap. Binary/media-content validity and untracked files not assessed.
No file is auto-deleted or declared unused.

## Coverage

Tracked: 312; classified: 308; readable documents: 104; above read cap: 0; findings: 1.

## File roles

| Role | Count |
|---|---:|
| agent | 9 |
| asset | 10 |
| bitacora | 2 |
| build_script | 7 |
| config | 20 |
| document | 96 |
| index | 6 |
| installer_metadata | 1 |
| license | 1 |
| source | 104 |
| test | 39 |
| text_metadata | 2 |
| workflow | 11 |

## Findings (human review required)

- **REVIEW / POTENTIAL_UNLOGGED_CHANGES** PROJECT_LOG.md: 13 implementation/config paths changed since this log's last commit; examples: app/src/main/java/com/michelslab/louderme/audio/AudioBoostContract.kt, app/src/main/java/com/michelslab/louderme/audio/AudioBoostService.kt, app/src/main/java/com/michelslab/louderme/audio/AudioRouteLabel.kt, app/src/main/java/com/michelslab/louderme/audio/BoostQuickLevels.kt, app/src/main/java/com/michelslab/louderme/audio/EqualizerModels.kt, app/src/main/java/com/michelslab/louderme/audio/EqualizerStateStore.kt. Next: Inspect diffs and reconcile only meaningful decisions/results in the canonical log (ID 0b865f8f7f55b125)

Complete path → purpose/role/certainty list lives in FILE_GOVERNANCE.json. SUMMARY.json has counts only for master polling.
A dated file, duplicate, or suspicious name is an **investigation candidate**, not unwanted material.
No historical science cases, audit logs or evidence may be deleted automatically.
