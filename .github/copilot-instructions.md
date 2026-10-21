# Copilot instructions — LouderMe


## Desktop About placement (mandatory)

Every desktop application must display a clearly labeled, usable **About** action in its **fixed top application header** from initial launch and in every workspace. It must remain visible when the window is compact/high-DPI, the body is scrolled or a sidebar is collapsed; footer-only, Home-only, offscreen or hidden-overflow About is forbidden. Keep the header outside the scroll container and validate its actual rendered visibility and click bounds. Source of truth: `michels-lab/Michel-Software-Standards/standards/BRAND_NATIVE_INTERFACE_STANDARD.md`.

Read `AGENTS.md`, `PROJECT_LOG.md` and the relevant Android or Windows build/workflow files before editing.

Keep Android Play and sideload distribution paths separate. Preserve Android stable signing/direct-update integrity and the Windows installer/feed/update contract. Never introduce privileged secrets or CI-debug signing as a production update identity.

Do not confuse digital target gain with acoustic dB SPL. Keep Android EQ mapping/clamping explicit and preserve Android safe-area behavior. On Windows, keep native endpoint volume at 0–100 and route Global Boost above 100% through the documented APO path.

Use current-commit build/test evidence. Android device audio/routing/PackageInstaller/Play-delivery claims and Windows APO attachment/audible behavior/SmartScreen/AuthentiCode claims remain open until actually tested.

Update `PROJECT_LOG.md` for meaningful work. Do not publish a release unless explicitly assigned.

Product identity / About are fundamental product contracts. Treat the approved flowing-waveform geometry as the visual foundation across LouderMe, not as a sticker. About must lead with LouderMe identity/version, then a **paired author/studio** composition (Michel Duarte portrait/name/role beside the official Michel's Lab lockup and **TOOLS WITH IDENTITY.** when wide; grouped responsive stack on mobile), then social links rendered as **network icon + visible network name** using canonical profile URLs. Keep LouderMe's separate SOUND THAT LIFTS YOU product slogan.



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

<!-- MICHELSLAB_SHARED_CONTRACT_BEGIN id=child-agent-core version=2026-10-21.1 -->
# Michel's Lab shared child-agent contract

This managed block is cross-project policy. Repository-specific instructions may add stricter local rules outside this block, but they must not weaken or contradict it.

## Shared authority

- Michel's Lab shared standards, product identity, governance, release and coordination rules are authoritative in `michels-lab/Michel-Software-Standards`.
- Keep product implementation truth and product-specific audit logs in this child repository.
- Do not silently invent a conflicting local Michel's Lab rule.
- Never commit secrets, credentials, signing material, private tokens or passwords.
- Historical green CI is not proof for the current commit.

## Mandatory cross-product isolation

- Share **only** generic governance, branding, evidence/provenance, safe release, privacy and coordination mechanics across applications. Never copy source files, test fixtures, domain taxonomies, scientific solver paths, backend assumptions, integration details, or feature plans from one unrelated product to another.
- Each product owns its functionality, validation suites, pending work and architecture. A role file that is synced from the master is not permission to invent shared domain requirements; where a specific application capability is not present, keep it out of that child contract.
- When adapting a reusable tool, separate its general engine from its per-product policy and negative regression fixtures. Verify the active repository identity and actual file tree before submitting a change or PR title. For cross-product migrations, inspect every target and prove the policy is appropriate for that target.
- **Do not treat incidental appearances of a foreign product name or workflow in another repo as an app integration.** Correct the misplaced material and record the source and destination of the mistaken copy without claiming runtime coupling.

## Product identity / About

- Preserve the approved product-logo geometry; contextual color, material, lighting and motion may adapt without identity drift.
- Branding is a design language, not sticker placement.
- About hierarchy is **Product identity → paired author/studio composition → social profiles**.
- The paired author/studio composition should show **Michel Duarte** and **Michel's Lab** side by side when width permits: portrait + name + developer role on one side; official Michel's Lab logo/lockup + studio name + canonical slogan **`TOOLS WITH IDENTITY.`** on the other.
- Narrow/mobile layouts may stack responsively, but the author and studio must remain visually grouped as one intentional composition.
- Do not replace the Michel's Lab slogan with a paragraph-length studio review by default.
- Use the current canonical Michel Duarte portrait and the official Michel's Lab parent-brand assets from the master authority when implementing/updating About.
- The canonical portrait file is immutable: child repositories must vendor it byte-for-byte. Never resize, crop, recompress, retouch, regenerate, convert or rewrite the portrait asset itself; use render-time layout/object-fit/masking only.
- Visible social controls use recognizable network icon **and** visible network name with canonical profile URLs.


## Brand-native design and workspace architecture (mandatory)

- Product logo geometry is the source of the app's **entire UI design language**; do not paste a canonical SVG in an unrelated sticker card and call branding complete. Use shared typography, spacing, geometry, control and motion tokens from that identity.
- **Every desktop app:** place a visible, labeled **About** action in the fixed top application header on launch and throughout all workspaces. It MUST NOT exist exclusively in a footer, off-screen scroll content, Home, a collapsed sidebar or overflow menu. It must stay accessible at the minimum window size and high DPI; verify actual initial viewport placement and clickable bounds. Header stays fixed while main content scrolls.
- Keep official mark + readable product name and global About/Updates persistently visible independent of collapsible sidebar and selected workspace. No duplicate massive lockup/heading.
- Only require workspaces, navigation and views that genuinely exist in the current product. Product-specific Data/Field/Analysis, 3D scenes and scientific simulation conventions must live in their owning product repository, never in the shared contract.
- Keep product-specific primary workspaces unobstructed: contextual controls should not obscure the app's actual content, audio controls, dashboards, or other functional surfaces.
- At normal desktop widths/heights, the initial About viewport must show portrait, product, studio slogan and all five recognizable social icons **with visible names without scrolling**; test viewport intersections, not just existence or scroll reachability. Review actual screenshot.
- On large imports, progress feedback has clear preparing/loading/stalled/completed/failed states and cannot hang forever at zero progress.
- Apply `standards/BRAND_NATIVE_INTERFACE_STANDARD.md`; never claim a cohesive redesign without actual installed-surface screenshots, **relevant product-specific functional checks** and human review.

## Rendered visual brand release gate — mandatory for every app

- For launch, splash, About, launcher or product-identity changes, follow `standards/BRAND_VISUAL_VALIDATION_STANDARD.md`. Checking that an official asset exists/decodes or that a build passes is **not** visual acceptance.
- Inspect computed final UI geometry, theme/text contrast, clipping/overlap and duplicate lockup/heading. CI must fail for known visual violations; require screenshots from the exact candidate when automated capture is available; Michel performs human visual review after release.
- Validate relevant viewport sizes/themes in the actual browser/native/mobile runtime, including the packaged app where possible. If evidence is missing, explicitly report `pending visual review`; never say branding is complete from static tests alone.

## Canonical identity asset precedence

- Product-logo authority is `shared-assets/product-logos/manifest.json` in the master standards repository.
- Michel's Lab parent-brand authority is `shared-assets/michels-lab/manifest.json`.
- Assets explicitly marked legacy/rejected must never supersede the current canonical geometry.
- When `.michelslab/identity-sync.json` marks an identity section `enforced`, child canonical copies must match the master source exactly; hash drift is a governance defect.
- When identity state is `migration_pending`, do not auto-replace local assets. Stop, reconcile the active surfaces explicitly and preserve the approved master geometry.
- A local filename such as `official-*.svg` is not proof of authority by itself; authority comes from the current master manifest and identity-sync policy.

## Cross-chat coordination

- Tracked work follows the master claim lifecycle: `unclaimed → claim → heartbeat → complete/release → structured handoff`.
- If a different owner holds an active non-stale claim, stop instead of duplicating edits.
- A stale claim requires a freshness check before resuming/reclaiming work.
- A claim is coordination state only; it is never validation or release permission.
- Return branch/commit/evidence information so the master claim can be heartbeated, completed or released correctly.

## Structured handoff

For tracked work, return:
- task ID and repository;
- owner/role and branch;
- outcome;
- commits and areas changed;
- validations that **actually ran** and their real result;
- evidence status: `verified`, `inferred`, or `blocked`;
- remaining work;
- blockers/manual evidence still required;
- suggested next owner/role when useful.

Never list a planned build/test/device/store check as completed validation. If required validation was not performed, hand the task back with that work pending instead of claiming completion.

## State truth before status/release claims

Before answering or handing off any question about what is current, released, published, ready, or pending, resolve four independent dimensions from current evidence:

1. **Working HEAD** — current branch/default-branch SHA and relevant PR/branch state.
2. **Latest stable release** — actual published tag/version, publication timestamp, release commit/artifacts.
3. **Same-SHA CI** — validation for the exact commit and affected distribution channel.
4. **External provider state** — Store/Play/cloud/device/provider evidence such as uploaded, certified, published or delivered.

Never collapse these into one status. A newer `main` does not make the latest release newer. A built MSIX/APK/installer is not a Store/Play publication. A GitHub release is not provider publication. If `main` is ahead of the stable release, say so explicitly.

After a merge, release, tag or provider mutation, re-read authoritative state before the final status answer or handoff. If a generated queue/gate conflicts with newer evidence, route reconciliation instead of repeating completed product work.

## Multi-channel distribution and contract-test robustness

- If an app ships through more than one channel (for example direct GitHub Setup/Portable plus Microsoft Store MSIX, or direct APK plus Google Play), treat each channel as a separate validation surface over the shared source.
- A change to shared runtime, version, packaging, updater or identity code must run the affected channel validations on the **current commit**. A Store/Play workflow that only runs on a special distribution branch is insufficient once its shared implementation lives on the default branch.
- Store-managed builds must not also self-update from the direct-download feed unless the product explicitly documents and validates that dual-update design.
- Package creation is not provider publication. Keep evidence states separate: package built/validated → uploaded → certified/approved → published → delivered/installed.
- One authoritative product version must drive all channels. Never use a previous real release number as a runtime/version fallback because it can silently report stale identity; derive from authoritative metadata, fail clearly, or use a neutral non-release sentinel.
- Contract/regression tests must be portable across CI platforms. Normalize or tolerate CRLF/LF and path-separator differences and prefer structural/semantic assertions over exact whitespace or source-format matches.
- Syntax-check executable test/validation scripts before relying on them as semantic gates (for example `node --check` or `python -m py_compile` where applicable).
- When a channel is added or materially changed, update the app audit log and the master store/release gate. Do not describe the channel as published until provider evidence exists.

## Owner-only testing and official release delivery (all governed applications)

- **Owner-only default:** Until Michel explicitly says this particular app was shared with another person, assume Michel is its sole tester/user. Do not infer an external user base from a public GitHub release. This does not make public assets private. A sharing announcement about a different app never changes this app's status.
- **Explicit release order:** **Standing owner instruction (2026-10-21): a Granada invocation targeting this app conditionally authorizes its next FINAL STABLE release after all actual product gates pass, with no second prompt.** Outside targeted Granada, an explicit per-app publish order is required. 'Continue', CI green, a PR merge or feature completion alone is not authorization. Keep implementing/integrating tested changes without repeatedly asking him to say 'continue'; never auto-publish/tag/activate release workflows merely because main advanced.
- **Owner download rule:** Do NOT direct Michel to install or download from a PR, branch ZIP, GitHub Actions build/artifact or preview as the normal testing/update path. You can report the CI evidence without treating it as a downloadable release. Discuss a developer preview download only if Michel expressly asks for it.
- **Official release delivery:** Under the target-app Granada standing release authorization (or another explicit per-app release order), first satisfy existing automated same-commit signing, build, launch, functional, rendered UI and artifact checks. Publish via the product's official GitHub Releases/designated distribution channel, verify the real downloadable assets, and provide their official release links. Never describe a newer main or PR build as already installed or released.
- **Separate gates:** Owner release permission and his post-release visual review are different. Do not reintroduce a manual pre-release approval gate, or claim physical-device/provider acceptance from CI.

## Release and evidence boundary

- Do not publish/release unless explicitly authorized, **including conditional 2026-10-21 target-app GRANADA authorization after all required gates pass**. No RC/beta/prerelease, no implicit cross-app publication.
- Manual/device/store/provider validation remains pending until actually performed.
- Do not fabricate screenshots, device behavior, store status, cloud/provider state or test results.
- Preserve unrelated known-good behavior and keep changes bounded to the assigned task.
- For installable Windows apps, the canonical direct release is built by GitHub Actions from the authorized commit/tag and delivers a real Setup installer as the normal-user artifact.
- Use `<Product>-Setup-vX.Y.Z.exe` for the recommended installer. If a portable build is also shipped, name it explicitly `<Product>-Portable-vX.Y.Z.exe`; never leave the portable filename ambiguous when both exist.
- For installable Windows apps, separate evidence into **BUILD PASS → INSTALL PASS → LAUNCH PASS → FUNCTIONAL PASS**. A green installer/build job is not proof that the installed application starts.
- Smoke-test the generated Windows installer by actually installing it and then **launching the executable from the installed location** before uninstalling. Merely verifying that the EXE exists is insufficient.
- Installed-app LAUNCH PASS requires either a normal GUI process that remains alive long enough to expose a real top-level window, or an app-owned deterministic smoke mode that boots the real installed UI/runtime path and emits explicit success evidence.
- If installed startup fails, preserve process exit/lifetime plus available app logs and Windows Application/.NET crash evidence before failing CI.
- A portable launch PASS and an installed-app LAUNCH PASS are separate claims when both artifacts are shipped.
- Publish SHA-256 for direct Windows binaries. Authenticode/code signing, when available, must happen before final checksum publication. Without a publisher certificate, do not hide or misrepresent Windows Unknown publisher/SmartScreen behavior.
- FoamLens and Michel's Life are the current Windows release references; Michel's Life also demonstrates optional Authenticode and a separate Microsoft Store MSIX path.

## Android first-run modal + persistent theme acceptance (master shared rule)

- **Every Android/mobile app with first-run onboarding or a blocking form/modal** must keep the modal above underlying app chrome and prevent header/drawer/section gestures from receiving taps until the modal closes.
- The primary Continue/Finish controls must remain *physically reachable* and clickable above the Android bottom navigation/gesture region, even on a compact/tall-form step: use true window/safe-area insets, bounded content scrolling **independent of a persistent action footer**; do not rely on a lone desktop overflow container.
- Add real interaction regression coverage: test the **longest** onboarding step using touch/hit-target verification, tap Continue→Finish and confirm persisted completion; check at least one compact and one materially different viewport, including the effect of system status/navigation bars.
- Selecting goal/focus/category checkboxes must not implicitly change the chosen theme. Validate persisted palette/settings and rendered colors before/after category and navigation interaction.
- Keep original approved themes/backgrounds and desktop layout stable unless redesign is explicitly requested. Capture actual installed-APK UI and distinguish browser-harness PASS from native emulator/physical-device evidence. A fixed source/PR is **not** a corrected customer release until the new signed candidate is built, tested, packaged, and published.
- Canonical authority: `platforms/ANDROID.md`, `standards/UI_UX_STANDARD.md`, `standards/RENDERED_UI_RELEASE_GATE.md`. Originating real-world regression: Michel's Life source PR #37 (2026-10-09).

## P0 mandatory rendered-UI release gate (master-owned; every app)

- Source scans/XML assertions, build success, CI green, icon/blob hashes and file presence never prove the app **visibly renders**. Every release of a user-facing app MUST open the actual built/installed candidate and capture **Home + About + changed screens** across two supported viewport sizes; the capture must show meaningful nonblank content, visible author portrait, Michel's Lab logo and **`TOOLS WITH IDENTITY.`**, appropriate icons + network names, working scroll and Close/Back. Test native view bounds/visibility and navigation, not just inflation.
- Screenshots must originate from the actual runtime (Android emulator/physical device, Windows installed GUI, real browser), be checksum-validated and bound to the exact release source SHA plus candidate artifact SHA-256. The owner reviews rendered screenshots after release. Never make human review or protected Environment approval a prerequisite for publication. Preserve automated build/security/integrity checks; capture screenshots when supported, but mark missing evidence as unverified instead of introducing unconditional publication blockers.
- Mandatory reference: `michels-lab/Michel-Software-Standards/standards/RENDERED_UI_RELEASE_GATE.md`; validator: `tools/visual_release_gate.py` (vendor/consume with tests). Never fabricate images, attestations or user approval; agents cannot self-certify independent visual review. Do not claim device/store testing from an emulator. A pure no-UI-change release needs documented same-artifact UI identity and bounded exception as defined in the standard.
- Release manager: report missing visual evidence and open P0 for confirmed visual regressions; do not block publication solely for pending human review. QA agent: inspect actual pixels and record **SOURCE, BUILD, LAUNCH, RENDER, VISUAL REVIEW and DEVICE** independently. App maintainer: implement real UI smoke tests and screenshot capture at appropriate viewports. Static PASS is not visual PASS.
- **Important rollout boundary:** this shared contract does NOT, on its own, modify the child release workflow. Until a repository demonstrably captures and validates rendered evidence, mark its visual QA **NOT VERIFIED** without blocking releases solely for missing human approval.

## HTML Desktop delivery (conditional standard for local browser apps)

If a child app's Desktop distribution is a local HTML browser application, its **primary GitHub Release Desktop asset MUST be a direct, self-contained, versioned `.html` file**, with embedded required imagery/branding so it works without the ZIP or an `assets/` directory. An additional ZIP is allowed but never sufficient by itself. Enforce via manifest + publisher + CI assertion + actual offline browser launch, verify SHA-256 for the standalone HTML, and check that GitHub actually exposes the HTML next to APK and other assets. See master `standards/REPOSITORY_DISTRIBUTION_STANDARD.md`. Native installer apps are unaffected.

## Windows installer EULA + shortcuts (2026-10-11)
Every native Windows app installer must show a Welcome page, a complete real product license requiring affirmative user acceptance, optional Desktop shortcut, and an honest optional route for taskbar pinning. Windows taskbar pinning requests must occur only from an eligible foreground app on explicit user action, with OS permission/availability and final user consent; **do not call the taskbar pin API from an installer**, force taskbar links or fake completion. A final-page unchecked "Show how to pin to taskbar" option plus installed bilingual instructions is an acceptable fallback while no OS API implementation exists. Inno Setup, MS Store, standalone HTML and Android follow distinct platform rules. See `standards/WINDOWS_INSTALLER_EXPERIENCE_STANDARD.md`.

## Mandatory continuous independent work while CI runs (2026-10-12)
Once a PR or current-commit build, GitHub Actions check, installer test, review or external verification is pending, **do not stop development or ask the owner to prompt "continue"**. Immediately progress another genuinely independent, eligible, unclaimed task in a separate branch/repository/scope, while preserving each pending task's repository, branch, exact SHA, workflow/run ID, outcome and next verification action. Prefer work in another repository while a current repository is claimed. Same-repository parallel work is permitted **only** when the master task coordinator explicitly declares `parallel_safe: true` and distinct `parallel_scope` values for all participants; never steal a non-stale claim, edit colliding files or bypass a reserved task. Return to the original exact-head CI result before merging, claiming a gate PASS, closing the tracked task or publishing. If no genuinely safe task exists, report the actual blocker rather than inventing work. Pending, skipped or missing CI is not PASS, and independent progress does not authorize a release.
<!-- MICHELSLAB_SHARED_CONTRACT_END id=child-agent-core -->
