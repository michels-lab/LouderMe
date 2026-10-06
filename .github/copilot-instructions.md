# Copilot instructions — LouderMe

Read `AGENTS.md`, `PROJECT_LOG.md` and the relevant Gradle/workflow files before editing.

Keep Play and sideload distribution paths separate. Preserve stable signing and direct-update integrity. Never introduce privileged secrets or CI-debug signing as a production update identity.

Do not confuse digital target gain with acoustic dB SPL. Keep EQ mapping/clamping explicit and preserve Android safe-area behavior.

Use current-commit build/test evidence. Real-device audio quality, routing, PackageInstaller and Play-delivery claims remain open until actually tested.

Update `PROJECT_LOG.md` for meaningful work. Do not publish a release unless explicitly assigned.

Product identity / About are fundamental product contracts. Treat the approved flowing-waveform geometry as the visual foundation across LouderMe, not as a sticker. About must lead with LouderMe identity/version, then About the author with the canonical Michel Duarte portrait, then the official Michel's Lab parent-brand mark, then social links rendered as **network icon + visible network name** using canonical profile URLs.



For logo/About/branding work, follow the Michel-Software-Standards Product Identity Standard and BRAND_ADOPTION_PLAYBOOK. Replace actual platform identity references, adapt the official geometry to this app's existing visual language, avoid sticker-style logo placement, preserve unrelated behavior, validate the build, and do not release without explicit authorization.

## Structured handoff requirement

For any tracked Michel's Lab task, return enough machine-readable continuation context for the master handoff registry:

- task ID and repository;
- owner/role and branch;
- outcome;
- commits and areas changed;
- validations that **actually ran** and their real result;
- evidence status: `verified`, `inferred`, or `blocked`;
- remaining work;
- blockers/manual evidence still required;
- suggested next owner/role when useful.

Do not list planned tests/builds/device checks as completed validation. If required validation was not performed, the task must be released/handed back with that work pending rather than described as complete.


