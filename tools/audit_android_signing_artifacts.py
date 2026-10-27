#!/usr/bin/env python3
"""Read-only audit of GitHub Actions artifact METADATA and current signing workflow.

This tool NEVER downloads artifact ZIP contents, reads a keystore, or handles
repository signing secrets. A clean name scan CANNOT prove an archive lacks keys.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Callable

SIGNER_CERT = "4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3"
NAMES_SUSPICIOUS = re.compile(
    r"(signing[._-]?vault|legacy[._-]?vault|secret[._-]?(?:backup|vault)|"
    r"keystore|private[._-]?key|github[._-]actions[._-]secrets|"
    r"credential[._-]?(?:archive|backup|vault)|\.jks$)", re.I
)
BAN_UPLOAD = re.compile(
    r"(?i)(?:signing[/\\]|\.jks\b|"
    r"github-actions-secrets\.txt|legacy[-_]vault|secrets[/\\])"
)
REPO = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")


def verify_workflow(workflow: str) -> None:
    """Reject obsolete signer fallback and any artifact upload of credentials."""
    required = (
        "LOUDERME_SIDELOAD_KEYSTORE_BASE64",
        "LOUDERME_SIDELOAD_STORE_PASSWORD",
        "LOUDERME_SIDELOAD_KEY_ALIAS",
        "LOUDERME_SIDELOAD_KEY_PASSWORD",
        "apksigner",
        "EXPECTED_CERT_SHA256",
        SIGNER_CERT,
        "Erase ephemeral keystore after signing",
    )
    for text in required:
        if text not in workflow:
            raise ValueError("Permanent signer hardening or owner-approved cert was removed")
    # Only inspect the upload-artifact steps, not safe sign_sideload local
    # workfiles. Never treat normal published .apk / .sha256 as a private key.
    steps = re.split(r"(?m)^      - name: ", workflow)
    for step in steps:
        if "uses: actions/upload-artifact@" not in step:
            continue
        if BAN_UPLOAD.search(step.split("uses: actions/upload-artifact@", 1)[1]):
            raise ValueError("Signing material might be exported as a GitHub Actions artifact")
    for banned in (
        "signing/legacy-vault", "github-actions-secrets.txt",
        "actions/artifacts/11459101203/zip",
        "legacy signer recovery", "fallback to the private signing vault",
    ):
        if banned.lower() in workflow.lower():
            raise ValueError("Legacy credential artifact fallback has returned to the workflow")


def suspicious_artifacts(artifacts: list[dict]) -> list[dict]:
    return [
        {"id": x.get("id"), "expired": x.get("expired", False)}
        for x in artifacts
        if isinstance(x, dict) and NAMES_SUSPICIOUS.search(str(x.get("name", "")))
        and not x.get("expired", False)
    ]


def fetch_artifacts(
    repo: str,
    api_get: Callable[[str], tuple[int, dict]],
    max_pages: int = 30,
) -> list[dict]:
    if not REPO.fullmatch(repo):
        raise ValueError("GITHUB_REPOSITORY must be owner/repository")
    data = []
    for page in range(1, max_pages + 1):
        status, result = api_get(f"/repos/{repo}/actions/artifacts?per_page=100&page={page}")
        if status != 200 or not isinstance(result, dict) or not isinstance(result.get("artifacts"), list):
            raise RuntimeError("Artifact metadata unavailable: cannot certify retention hygiene")
        items = result["artifacts"]
        data.extend(items)
        if len(items) < 100:
            return data
    raise RuntimeError("Artifact audit exceeded bounded pagination; cannot claim exhaustive coverage")


def audit_legacy_id(
    repo: str,
    id_text: str,
    api_get: Callable[[str], tuple[int, dict]],
) -> bool:
    """Optional owner-supplied historical artifact ID; do NOT print or archive it."""
    if not id_text:
        return False
    if not re.fullmatch(r"[1-9][0-9]{5,19}", id_text):
        raise ValueError("Historical artifact ID must be a positive decimal integer")
    code, record = api_get(f"/repos/{repo}/actions/artifacts/{id_text}")
    if code == 404:
        return False
    if code != 200 or not isinstance(record, dict):
        raise RuntimeError("Legacy artifact metadata cannot be verified with available permissions")
    return not record.get("expired", False)


def github_api(token: str) -> Callable[[str], tuple[int, dict]]:
    if not token:
        raise RuntimeError("No ephemeral Actions read-only token; cannot audit historical artifacts")
    def get(path: str) -> tuple[int, dict]:
        request = urllib.request.Request(
            "https://api.github.com" + path,
            headers={
                "Authorization": "Bearer " + token,
                "Accept": "application/vnd.github+json",
                "X-GitHub-Api-Version": "2022-11-28",
                "User-Agent": "LouderMe-credential-metadata-audit",
            },
        )
        try:
            with urllib.request.urlopen(request, timeout=20) as response:
                return response.status, json.load(response)
        except urllib.error.HTTPError as error:
            if error.code == 404:
                return 404, {}
            raise RuntimeError(
                "Artifact metadata access failed; do not treat an unauthorized scan as clean"
            ) from None
    return get


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--workflow", type=Path,
                        default=Path(".github/workflows/android-ci-release.yml"))
    parser.add_argument("--scan-actions", action="store_true")
    parser.add_argument("--legacy-artifact-id", default="",
                        help="Owner-controlled optional ID; never printed or downloaded")
    args = parser.parse_args()
    verify_workflow(args.workflow.read_text(encoding="utf-8"))
    print("PASS: current Android signer is secret-based, cert pinned and vault fallback forbidden.")
    if not args.scan_actions:
        print("HISTORICAL ACTIONS ARTIFACTS: NOT INSPECTED (source-only contract).")
        return 0
    repo = os.environ.get("GITHUB_REPOSITORY", "")
    api = github_api(os.environ.get("GH_TOKEN", ""))
    entries = fetch_artifacts(repo, api)
    risky = suspicious_artifacts(entries)
    legacy_present = audit_legacy_id(repo, args.legacy_artifact_id, api)
    print(f"Checked {len(entries)} GitHub Actions artifact metadata records; "
          f"{len(risky)} currently nonexpired suspiciously named archives.")
    # No names/identifiers/secret material are output into public Actions logs.
    # Even a clean scan is NOT a content analysis.
    if risky or legacy_present:
        print("::error::Active possible credential archive detected. "
              "A repository admin must inspect and retire historical artifacts "
              "through GitHub UI; never download them to a public CI runner.",
              file=sys.stderr)
        return 2
    print("PASS: No suspicious artifact names / requested historical ID remain active.")
    print("LIMIT: innocently named archives may still contain credentials; "
          "manual historical run audit is necessary.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
