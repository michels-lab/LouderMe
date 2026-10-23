# Operación Granada — LouderMe Windows official-logo regression (2026-10-23)

Owner reported screenshot: title bar displayed a generic WinForms tiles icon, and main header displayed a waveform overlaid across the first letter, reading **“auderMe”** instead of LouderMe.

## Confirmed root cause

- In `MainForm.BuildUi()` the header allocated only 88 pixels for `WaveformMarkControl`, while `WaveformMarkControl` declares `MinimumSize = new Size(150, 60)`. WinForms layout could not respect both; the independently rendered wave painted over the adjacent, separately generated `Label {Text="LouderMe"}`.
- The approved real product identity already existed in `app/src/main/assets/branding/louderme/official-lockup.svg`, with associated `official-app-icon.svg` and `official-mark.svg`, documented as **Option 4 — flowing waveform**. Reconstructing the logo from a vector wave and arbitrary labels was not faithful to its actual typography, colors or layout.
- Desktop branding pipeline generated the ICO for the EXE but the visible Form.Icon was not explicitly assigned, allowing a generic four-squares WinForms titlebar icon.

## Correction

- Convert official SVG lockup into a runtime-embedded PNG during existing ImageMagick preparation. Do not introduce new artist names, media sources, font files or third-party logos.
- Render the complete approved lockup in a single `PictureBox` over a light surface, maintaining SVG aspect ratio and official original navy typography. Render the same resource in splash and About. Change only logo presentation—not Boost/EQ or physical audio logic.
- Embed and load the official source-derived ICO via `BrandAssets.LoadIcon()` for main, About and Developer titlebars.
- Run real compact WinForms check at 480 and 600 pixels to catch missing logo, clipping outside the header and collisions with About; validate brand raster size/palette and installed screenshot proofs in CI.

## Verification gates

Must pass on exact PR head: official SVG source/bitmap/icon generation; .NET compilation; APO and Developer contract; installed app startup, visual Home/About artifacts, icon resources and new live layout assertions. Bump Windows to unique maintenance v0.1.15 only; Android remains 0.1.12. Source and unified public releases are distinct gates. Never claim physical volume enhancement based on screenshot tests.
