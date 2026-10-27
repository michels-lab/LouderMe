# Granada — Windows shipping binary provenance + signed Android visual proof

Date: 2026-10-27. Issue #33. No release, no manual owner review and no audio hardware proof.

## Verified baseline
Android already has a separate sign_sideload to signed_visual_qa to private-publish chain. That job installs the FINAL signed APK on an emulator and captures real compact/wide Home/About screenshots; debug screenshot CI cannot substitute for it. Windows build and release jobs already install final Inno Setup and render actual WinForms Home/About/Equalizer screenshots from the installed EXE, with the correct exact source SHA already passed to both capture calls.

Remaining real gap: there was no explicit test that the EXE installed from final Setup was byte-identical to the separately shipped Portable EXE and both exact checksum sidecars. Hence correctly captured screenshots could still refer to a different binary than one of the published choices.

## Block A — actual Windows artifact provenance
New tools/verify_windows_shipping_provenance.py runs after the real installed Home/About/EQ screenshot gate, before uninstall/publication, in BOTH Windows build and release workflows. It hashes the installed EXE, Portable EXE, Setup EXE and screenshot manifest; demands installed==Portable, both .sha256 sidecars matching actual bytes and filenames, six unique compact/wide screenshot surface bindings, correct source SHA and identical version. Generates shipping-provenance.json next to existing screenshot PNGs and manifest, which existing workflow artifact upload includes. States explicitly: automated identity PASS, signature NOT CHECKED, post-release human review PENDING, physical audio NOT TESTED.

## Block B — Android FINAL signed APK proof
The pre-existing signed Android proof verifier now checks the exact 40-character source SHA plus schema, final signed package name and SHA256, four exact Home/About compact/wide coverage points, installed-emulator/device capture method, and on-disk image hashes with safe paths. Still relies on existing visual_release_gate.py to verify PNG decoding, image dimensions and nonblank pixels. Signed visual job verifies before any publish. PR/build validation runs new hermetic adversarial cases WITHOUT Android signing credentials or actual emulator.

## Block C — automated regressions, without new product version
Hermetic Windows tests exercise successful provenance and swapped binaries, wrong sidecars, missing/duplicate/offline screenshot claims, mismatched source or version. Both Windows workflows run the same tests before builds and enforce exact candidate provenance after installed smoke. Android tests cover final signed APK, altered image/source, forged capture methods and traversal. Existing Android full emulator Home/About and Developer CI remains mandatory.

## Limits
This does not certify acoustics, code-signing of Windows executables, hardware device coverage, or owner visual review. Windows screenshot evidence and portable binary identity are valid only for the exact tested installer on that workflow runner; remote redistributed file integrity still depends on publisher-verified checksums. Signed Android release testing only runs on new eligible version main releases, not PRs. No stable release version change. Public Windows 0.1.17 and Android 0.1.12 are unchanged.

## Acceptance/reentry
Require Windows CI and Android installed+Developer CI fully green on exact PR SHA before merging. Do not trigger a new version, sign a driver or overwrite public assets. Issue #33 remains open until an actual future final release's published artifacts/evidence and post-release review are audited separately.