# Granada — Windows About author and header corrections

Date: 2026-10-26. Owner supplied actual Windows v0.1.16 screenshots.

## Confirmed four regressions

1. Owner photo is too small in About because its `PictureBox` was hard-coded to 96×128 px and the author-table first column to 118 px.
2. The real Michel's Lab product logo was removed after a magenta raster artifact in an earlier WinForms `DrawToBitmap` screenshot. The substitute cyan text is not the approved brand identity.
3. The main "LouderMe Desktop" title can look clipped/crowded at the top of the About hero. Responsive constraints only checked horizontal clipping, not title height/spacing relative to the lockup.
4. Home header assigned its *entire flexible width* to the official-logo PictureBox (column 0 = 100%), with About in a far-right column. `PictureBoxSizeMode.Zoom` centers the image inside that oversized control, so the logo floats near the middle instead of sitting at the upper left.

## Implementation under review

- Home: fixed 308 px left-logo column, elastic middle space, independently right-anchored About button; use original transparent-dark official lockup without inventing a different logo. Runtime 480/600 px test explicitly guards against midheader drift, overlap, or clipping.
- About: make the canonical developer photo 196×250 px on wide layouts and 148×194 px on narrow layouts, with responsive column widths, font sizes and social rows retained below the author. Add explicit real-layout checks for large visible photo and studio logo.
- About: the previously bundled 7,527-byte `Assets/MichelsLabLockup.png` was a **corrupted legacy PNG**, ImageMagick fails with `PLTE CRC`. The canonical source of truth is `michels-lab/Michel-Software-Standards/shared-assets/michels-lab/official-lockup.png` (2172×724, 526,482 bytes, Git blob `7fd48093968b31ddacd3098f5b15d962de580652`, SHA-256 `d03e8549af2cbcb04c2329029a02f0cd078972ce1b1ff948ef66c3aebe7d10f4`). Windows branding now obtains the **exact original bytes** from pinned standards commit `769d427b5307edb9f142ed36cdb1dc5ea005bbc7`, verifies the SHA-256 and dimensions, embeds the approved unchanged source as `MichelsLabCanonicalLockup.png`, and creates a distinctly named dark-card composited `MichelsLabDarkLockup.png` derivative. Original corrupt legacy asset is not embedded. No substitute text or redraw. Automated raster check rejects empty images and an excessive magenta fill.
- About: increase title area to 70 px, provide text padding and margins, and add live assertion requiring it below the brand hero without clipping.

## Acceptance criteria

1. Source tests on exact PR head confirm original asset hashes and composited studio logo/image is visible, not blank or magenta-filled.
2. Actual installed Windows app renders usable Home, About, and EQ screenshots in 600×520 and 900×700 without clipping; inspect pixels personally rather than trusting CI green alone. At 600×520, portrait and brand logo must both be substantial, and Home logo must be visually left-anchored.
3. Equalizer remains draggable and working; updater, Developer Edition, Equalizer APO, windows volume and all other unaffected functionality must pass existing regression gates. Android remains unchanged.
4. Only after all checks pass merge the visual PR, then prepare a **new unique** Windows-only stable maintenance version with a separately checked source installer and dedicated immutable public distributor. Do not rewrite v0.1.16 or claim physical audio validation.

Current status: **source changes proposed, no version bump or release yet**.
