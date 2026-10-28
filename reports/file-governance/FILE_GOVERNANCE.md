# File steward: repository file audit

All tracked files classified; Markdown/text docs inspected with size cap. Binary/media-content validity and untracked files not assessed.
No file is auto-deleted or declared unused.

## Coverage

Tracked: 260; classified: 256; readable documents: 74; above read cap: 0; findings: 1.

## File roles

| Role | Count |
|---|---:|
| agent | 9 |
| asset | 10 |
| bitacora | 2 |
| build_script | 7 |
| config | 20 |
| document | 66 |
| index | 6 |
| installer_metadata | 1 |
| license | 1 |
| source | 90 |
| test | 31 |
| text_metadata | 2 |
| workflow | 11 |

## Findings (human review required)

- **REVIEW / POTENTIAL_UNLOGGED_CHANGES** PROJECT_LOG.md: 2 implementation/config paths changed since this log's last commit; examples: native/virtual-driver/IndependentGainCheck.ps1, native/virtual-driver/test_IndependentGainCheck.ps1. Next: Inspect diffs and reconcile only meaningful decisions/results in the canonical log (ID 353a78cd94fb5509)

Complete path → purpose/role/certainty list lives in FILE_GOVERNANCE.json. SUMMARY.json has counts only for master polling.
A dated file, duplicate, or suspicious name is an **investigation candidate**, not unwanted material.
No historical science cases, audit logs or evidence may be deleted automatically.
