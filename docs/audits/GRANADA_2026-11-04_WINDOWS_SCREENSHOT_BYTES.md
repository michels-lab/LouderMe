# Granada — 2026-11-04: exact Windows screenshots bytes at shipping gate

## Reproduced acceptance gap

Before this change, `verify_windows_shipping_provenance.verify()` checked exact installation EXE == Portable EXE; Setup bytes, version, sidecar SHA256, screenshot-manifest source SHA and six declared surfaces. **It never read the screenshot files or their hashes**. An altered/deleted PNG after the earlier `visual_release_gate.py` succeeded could still yield a shipping `automated_binary_and_visual_identity=PASS` report. The upstream gate is not a safe substitute for verifying the final evidence bundle in this last identity check.

## Engineering correction

The shipping verifier now rereads each of six screenshot files within the exact manifest directory and requires a canonical name (`home-compact.png`, `about-wide.png`, etc.), expected viewport metadata (compact 600x520; wide 900x700), a valid SHA256, matching current file hash and non-reused image bytes across surfaces. It fails before writing `shipping-provenance.json` if any image has moved, disappeared or been swapped. Shipping report includes a keyed digest for every screenshot and a flag that its current file bytes were inspected.

`tools/test_windows_shipping_provenance.py` now builds six independent, deterministic byte fixtures (without claiming they are real PNG screenshots) and checks nine additional negative cases: swapped/delete, invalid digest, file substitution/path traversal, mismatched viewport, duplicate bytes. The actual Windows workflows already run the enhanced tests and verifier alongside image decoding/visual checks, so there is no extra releaser or CI bypass to introduce.

## Acceptance and unchanged gates

**Test source only until the exact PR head's Windows CI succeeds**; hermetic vectors do not assert visually correct pixels. All six live WinForms captures still originate from the actual installed final Setup, which is hashed against the Portable release. Android's independently signed APK screenshots are separately validated. Neither CI proves Authenticode signing, physical boost, audio quality, selected endpoint routing or post-release owner review. Issue #33 remains open pending a future release audit. **No RC and no release.**
