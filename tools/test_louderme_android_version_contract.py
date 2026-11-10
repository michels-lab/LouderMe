"""Release version regression: stable Direct public APK and Developer override agree.

The old Android CI hardcoded versionCode 13 while Gradle was ready for 14.
This would fail a legitimate new signed release before any shipping QA.
These assertions run in the ordinary Android build pipeline; negative
mutations ensure the original bug cannot silently return.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GRADLE = ROOT / "app/build.gradle.kts"
WORKFLOW = ROOT / ".github/workflows/android-ci-release.yml"


def check_version_contract(gradle: str, workflow: str) -> tuple[str, int]:
    name = re.search(r"\bversionName\s*=\s*\"(\d+\.\d+\.\d+)\"", gradle)
    code = re.search(r"\bversionCode\s*=\s*(\d+)\b", gradle)
    if not name or not code:
        raise AssertionError("Public versionName and versionCode must be explicit")
    if int(code.group(1)) <= 13:
        raise AssertionError("New Direct release must advance versionCode beyond v0.1.12")
    if tuple(map(int, name.group(1).split("."))) <= (0, 1, 12):
        raise AssertionError("New Direct release must advance beyond v0.1.12")
    for required in (
        "RELEASE_VERSION_CODE: ${{ steps.version.outputs.version_code }}",
        "versionCode='$RELEASE_VERSION_CODE'",
        "developerVersionCode=10001",
        "test_louderme_android_version_contract.py",
    ):
        if required not in workflow:
            raise AssertionError("Android CI release package uses a stale/hardcoded version: " + required)
    # Developer's separately pinned signed upgrade fixture legitimately uses
    # code 10001. Reject fixed codes ONLY for the public Direct package.
    if re.search(
        r"package: name='com\.michelslab\.louderme' versionCode='\d+'",
        workflow,
    ):
        raise AssertionError("Do not hardcode the public Direct versionCode in CI assertions")
    return name.group(1), int(code.group(1))


class AndroidVersionContractTests(unittest.TestCase):
    def test_public_release_version_has_automated_identity_check(self):
        version, code = check_version_contract(
            GRADLE.read_text(encoding="utf-8"),
            WORKFLOW.read_text(encoding="utf-8"))
        self.assertGreater(code, 13)
        self.assertGreater(tuple(map(int, version.split("."))), (0, 1, 12))

    def test_original_v13_regression_is_rejected(self):
        gradle = GRADLE.read_text(encoding="utf-8")
        workflow = WORKFLOW.read_text(encoding="utf-8")
        broken = workflow.replace("versionCode='$RELEASE_VERSION_CODE'",
                                  "versionCode='13'")
        self.assertNotEqual(workflow, broken)
        with self.assertRaises(AssertionError):
            check_version_contract(gradle, broken)

    def test_missing_version_binding_is_rejected(self):
        gradle = GRADLE.read_text(encoding="utf-8")
        workflow = WORKFLOW.read_text(encoding="utf-8")
        broken = workflow.replace(
            "RELEASE_VERSION_CODE: ${{ steps.version.outputs.version_code }}",
            "RELEASE_VERSION_CODE: 13")
        self.assertNotEqual(workflow, broken)
        with self.assertRaises(AssertionError):
            check_version_contract(gradle, broken)


if __name__ == "__main__":
    unittest.main()
