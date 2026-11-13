# File steward: repository file audit

All tracked files classified; Markdown/text docs inspected with size cap. Binary/media-content validity and untracked files not assessed.
No file is auto-deleted or declared unused.

## Coverage

Tracked: 343; classified: 339; readable documents: 115; above read cap: 0; findings: 2.

## File roles

| Role | Count |
|---|---:|
| agent | 9 |
| asset | 10 |
| bitacora | 2 |
| build_script | 7 |
| config | 20 |
| document | 107 |
| index | 6 |
| installer_metadata | 1 |
| license | 1 |
| source | 116 |
| test | 45 |
| text_metadata | 2 |
| workflow | 13 |

## Findings (human review required)

- **CONFIRMED / BROKEN_RELATIVE_LINK** docs/INFRASTRUCTURE_AUDIT.md: Missing: docs/docs/audits/GRANADA_2026-11-12_OWNER_ACCEPTANCE_LOUDERME_300PCT.md. Next: Fix link or restore target (ID add32cf1f0e74b20)
- **CONFIRMED / BROKEN_RELATIVE_LINK** docs/RELEASE_NOTES_v0.1.14.md: Missing: docs/docs/audits/GRANADA_2026-11-12_OWNER_ACCEPTANCE_LOUDERME_300PCT.md. Next: Fix link or restore target (ID e899946628b4703e)

Complete path → purpose/role/certainty list lives in FILE_GOVERNANCE.json. SUMMARY.json has counts only for master polling.
A dated file, duplicate, or suspicious name is an **investigation candidate**, not unwanted material.
No historical science cases, audit logs or evidence may be deleted automatically.
