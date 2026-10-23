# Operación Granada — Windows About collapse, white sticker and EQ UX

2026-10-23. Exclusive product: LouderMe Windows. Evidence: owner-supplied screenshots of v0.1.15 at full window width.

## Root causes

1. **About empty:** a FlowLayoutPanel with auto-sized Card Panels contained AutoSize TableLayoutPanels with `Dock=Top`. The parent card's preferred size was computed primarily from padding, making visible narrow ~48-pixel empty strips even though children remained in the tree. Prior CI only checked screenshot existence, not meaningful content bounds.
2. **White rectangle:** official `official-lockup.svg` has a dark navy Louder wordmark; Windows rendered that source on a forced light PictureBox background `Color.FromArgb(247,250,254)`. The resulting 800px-wide white strip was visually foreign to the dark theme. The source branding geometry was correct, not its presentation.
3. **EQ looked like a form:** seven vertically stacked NumericUpDown rows dominated and the response line was only 68px tall. User could not edit the plotted curve; this looked like a spreadsheet rather than an audio tool.

## Changes

- About: one scrollable Panel, TableLayoutPanel full-width stack, non-auto-sized cards with sufficient content heights and responsive social rows. A real runtime invariant checks every card width and height, real logo, non-clipped hero text, and scroll ability at 480, 600, 860px; the standard installed Home/About screenshot CI invokes the same checks.
- Official dark logo: derive a **transparent** dark-background variant from the actual approved SVG by substituting navy type `#06182d→#eff6ff` and muted slogan `#486580→#a6bcd4`, leaving the approved path, icon, lockup proportions and blue Me unchanged. Use the same transparent asset in Main/Splash/About. Build-time PNG contract rejects a white opaque rectangle and mismatched source.
- EQ: shaded high-resolution seven-band interactive response curve. Mouse/touch-like click/drag and keyboard arrow adjustments emit band+gain pairs to the original numeric-value change machinery, preserving 0.5 dB increments, presets, persistence and APO activation logic. The precise NumericUpDown panel is available via a disclosure control, not the main view. Disable both systems while APO is unavailable or listening test is running.
- Windows version 0.1.16 proposed after exact-head Windows/Android CI; stable public release remains 0.1.15 until verified source and unified releases exist. Android source untouched.

## Manual QA still important
CI renders live WinForms screenshots and checks structure but cannot judge every aesthetic preference or hardware. Owner visual inspection of installed Home/scroll/About and fader usability remains valuable. Do not imply numerical gain equals measured physical loudness.

## Follow-up after reviewing real CI screenshots
Opened and inspected actual `about-compact.png`, `about-wide.png`, `home-compact.png`, `home-wide.png` from GitHub Actions v0.1.16 candidate. Layout was structurally repaired, logo integrated without white plate, but the compact hero description visibly extended out of bounds and the author card contained a bright opaque magenta bar where the Michel's Lab PNG was drawn. Fixed by applying dynamic wrapping width to the hero summary and replacing the defective raster in that author-only attribution with consistent brand-colored `MICHEL'S LAB` text. Additional acceptance: new `equalizer-compact.png` and `equalizer-wide.png` actual scroll-position screenshots, required in the SHA-bound manifest and both Windows build/release CI gates, so an apparently healthy Home screenshot can no longer mask an unusable EQ.
