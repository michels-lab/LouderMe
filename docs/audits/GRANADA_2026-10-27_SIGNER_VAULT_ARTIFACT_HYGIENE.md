# Operación Granada — Android legacy signing artifacts: exposure-safe audit and retirement

Date: October 27, 2026. Issue #40.

## Facts independently verified from the connected repository

1. Issue #40 was **stale**: its original description still described the October 8 failure to sign Android 0.1.10. Later issue comments document the owner's authorized **permanent signer migration**, approved certificate fingerprint and actual successful sign of Android 0.1.11 via GitHub Actions on October 11. Public Android 0.1.12 was subsequently released on October 12; the official unified package was last updated October 26 (Windows 0.1.17, Android 0.1.12). Do not pretend permanent signer Secrets remain unconfigured merely because the old issue header says so.
2. Historical version of android-ci-release.yml at the old October 8 source commit *wrote local signing/github-actions-secrets.txt* during signing, including credentials, and described an Actions vault artifact. The one-time recovery code from a later commit referenced a specific historical Actions artifact as a source of old private signing material. That fallback was removed when the new, owner-approved signer was installed. Do NOT reprint archival artifact IDs, download old ZIPs, or embed private values in a public PR.
3. The currently shipped workflow uses the permanent GitHub Actions Secrets, pins the approved PUBLIC certificate SHA-256 both at keystore selection and after signing, and erases ephemeral keystore after signing. It exports only validated sideload APK, digest and public certificate detail as the signed artifact. This is not proof of org-level Secret access policy/rotation or retirement of historical exposed archives.
4. Historical Actions run artifacts inspected by read-only GitHub metadata access are still present from October 8/11. Their *names alone* do not establish whether any archive contains keys. The sensitive legacy artifact's present existence, access and deletion have **not** been verified; do not mark issue #40 completed.

## Current implemented security controls

- tools/audit_android_signing_artifacts.py: source-only fail-closed contract for four Secrets, BOTH permanent certificate pins, APK signature verify step, ephemeral keystore cleanup, and prohibition on archive downloads/vault fallback. Rejects credential or keystore paths in any current GitHub Actions upload-artifact step.
- A separate read-only inventory mode queries GitHub Actions artifact **metadata only**, using the workflow ephemeral token with permissions actions:read and a bounded number of pages. It flags nonexpired suspicious archive names while intentionally exempting legitimate APK/PNG signed-release names. If an owner supplies the old artifact ID in a workflow_dispatch field, it queries that single artifact's metadata WITHOUT downloading it, fails if still available, and never logs the supplied identifier. An inaccessible GitHub API, ambiguous old artifact or incomplete pagination is **not** accepted as clean.
- .github/workflows/android-signing-artifact-hygiene.yml: automated offline PR source/test gate and separate metadata-only first-main-merge/first-main-merge/weekly/manual security inventory. No signing Secrets in the audit, no extraction, no artifacts uploaded and no automatic deletion. A failed security scan requires owner review; it cannot silently retire a legacy archive.
- tools/test_android_signing_artifact_hygiene.py: hermetic negative tests with mock API responses; checks vault reintroduction, secret-file upload attempts, forged signer pin, normal signed APK name exemption, full pagination, 403 fail-closed, present/expired/deleted historical IDs and absence of any ZIP download.

## External steps necessary to finish security issue #40

The owner / GitHub repository admin should use the GitHub Actions web interface to inspect the original historical signing/recovery run and any associated artifact archives. If a retained archive holds the obsolete private JKS, credential handoff or secrets, safely delete that particular Actions artifact using GitHub's UI. Avoid downloading it just to inspect it: the old source itself establishes potential content. Confirm no public releases ever included credential files (release assets are currently APK/EXE and hashes only). Review GitHub repository/org Secrets and Actions read permissions, and rotate/revoke old credentials if a sensitive artifact was accessible; distinguish the retired 0.1.10-or-earlier signer from the current deliberately migrated v0.1.11+ identity. Keep a secure offline backup only in owner-controlled private storage. Never put keys, passwords, signing-Base64, vault artifact URLs or private ZIPs in Issues, PRs, source, bot logs or screenshots.

**Residual uncertainty:** a metadata-only artifact-name scan cannot prove an innocently named ZIP is safe; scanning all runs and manual old-artifact deletion/access review remain necessary even after a green weekly audit.

## Acceptance

CI on the PR head must pass the new offline security check and existing file-governance + Android installed Home/About + Developer regression checks. Source-only security change. No new stable Windows/Android version, no certificate rotation, no automatic release or driver install. Keep issue #40 **OPEN** until historical artifact retirement, org-level Secrets review and access evidence are actually available.
