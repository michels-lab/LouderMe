# File steward: repository file audit

All tracked files classified; Markdown/text docs inspected with size cap. Binary/research-content validity and untracked files not assessed.
No file is auto-deleted or declared unused.

## Coverage

Tracked: 200; classified: 196; readable documents: 48; above read cap: 0; findings: 3.

## File roles

| Role | Count |
|---|---:|
| agent | 9 |
| asset | 9 |
| bitacora | 2 |
| build_script | 7 |
| config | 20 |
| document | 40 |
| index | 6 |
| license | 2 |
| source | 75 |
| test | 14 |
| unknown | 3 |
| workflow | 9 |

## Findings (human review required)

- **REVIEW / UNKNOWN_PURPOSE** desktop/installer/TaskbarPinning.txt: No reliable purpose category from path. Next: Declare role after reviewing usage (ID 802201151e86f352)
- **REVIEW / UNKNOWN_PURPOSE** native/audio-engine/CMakeLists.txt: No reliable purpose category from path. Next: Declare role after reviewing usage (ID dcbb59ac3c172b89)
- **REVIEW / UNKNOWN_PURPOSE** native/audio-engine/include/louderme/AudioEngine.hpp: No reliable purpose category from path. Next: Declare role after reviewing usage (ID e27ff08885a0e8fc)

Complete path → purpose/role/certainty list lives in FILE_GOVERNANCE.json. SUMMARY.json has counts only for master polling.
A dated file, duplicate, or suspicious name is an **investigation candidate**, not unwanted material.
No historical science cases, audit logs or evidence may be deleted automatically.
