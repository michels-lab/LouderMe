# Perspective Decision Records (PDR)

**Status:** proposed analyses only, never evidence that source/runtime/release work has been implemented. Canonical policy: `michels-lab/Michel-Software-Standards/standards/PERSPECTIVE_COUNCIL_STANDARD.md`.

A `Perspective Council` agent may create `YYYY-MM-DD-short-topic.md` here when an owner-requested review is in scope and no other worker owns the file. The independent lenses must be identified separately; absent a live delegation tool, mark as *one internal multi-lens analysis*, not three independent agents that allegedly ran.

## Copyable record structure

```markdown
# PDR: <topic>
- Date / author / target repo / source SHA or inspected revision:
- Status: PROPOSED (not approved, implemented, tested or released)
- Owner's actual goal and current workflow:
- Evidence: file paths, runtime reports, screenshots or issues; note unknowns

## Independent lens: Contrarian
- Strongest objection, opportunity cost, simpler/do-nothing alternative:
## Independent lens: User Advocate
- Journey, onboarding, friction, accessibility, empty/error/recovery states:
## Independent lens: Intelligence Architect
- Automation/data/AI alternatives and technical/privacy limits:
## Cross-check: scientific/domain + security/privacy + differentiation
- Actual limitations and risks:

## Opportunity matrix
| ID | Proposal / smaller alternative | Evidence vs hypothesis | Impact 1–5 | Confidence 1–5 | Effort 1–5 | Risk 1–5 | Reversibility 1–5 | Priority | Owner/task | Acceptance experiment |
|---|---|---|---:|---:|---:|---:|---:|---|---|---|

## Disagreements and intentionally rejected ideas
- What the lenses disagree on; one idea we should NOT build:
## Questions that would change the decision
- 2–5 specific questions; not blockers for safe independent work:
## Next bounded action
- Proposed / approved / deferred / rejected; code owner; existing claims/PR conflicts; follow-up metric and exact validation still required:
```

Review artefacts must not contain private accounts, uploaded datasets, raw social exports, auth secrets, credentials or personally identifying information. Link to appropriately permissioned evidence instead. App-specific records stay in the app; promote only genuinely reusable patterns to the master. Any implementation or publication must follow the existing repo-specific claim, CI, audit and stable-release gates.
