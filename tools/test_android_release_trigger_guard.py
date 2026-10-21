"""Regression: auditor/document-only merges never publish LouderMe Android.

This inspects a GitHub Actions workflow statically; it does not trigger releases.
App source/Gradle changes still trigger the normal Android build pipeline.
"""
import re
import unittest
from pathlib import Path

WORKFLOW=Path(__file__).resolve().parents[1]/".github/workflows/android-ci-release.yml"
NON_PRODUCT_FILES={
    "tools/repository_file_auditor.py",
    "tools/test_repository_file_auditor.py",
    "tools/test_android_release_trigger_guard.py",
    ".michelslab/agents/file-steward.md",
    ".michelslab/file-audit-policy.json",
    ".github/workflows/file-governance-audit.yml",
    ".github/workflows/file-steward-pr-validation.yml",
    ".github/workflows/android-ci-release.yml",
    "docs/audits/FILE_GOVERNANCE_LOG.md",
}

class ReleaseTriggerGuard(unittest.TestCase):
    def setUp(self):
        self.source=WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("  push:\n",self.source)
        self.assertIn("  pull_request:\n",self.source)
        self.push=self.source.split("  push:\n",1)[1].split("  pull_request:\n",1)[0]
        self.pr=self.source.split("  pull_request:\n",1)[1].split("  workflow_dispatch:\n",1)[0]

    def test_governance_only_pushes_do_not_trigger_direct_release(self):
        self.assertIn("    paths-ignore:\n",self.push)
        self.assertIn("      - 'reports/file-governance/**'\n",self.push)
        ignored=set(re.findall(r"^\s+- '([^']+)'\s*$",self.push,re.M))
        self.assertTrue(NON_PRODUCT_FILES.issubset(ignored),
                        NON_PRODUCT_FILES-ignored)
        self.assertIn("      - main\n",self.push)

    def test_pull_request_still_tests_workflow_change(self):
        # PR validation must NOT silently vanish just because workflow YAML was edited.
        self.assertIn("    branches:\n",self.pr)
        self.assertIn("      - main\n",self.pr)
        self.assertNotIn("      - '.github/workflows/android-ci-release.yml'\n",self.pr)
        self.assertIn("  validate:\n",self.source)

    def test_real_app_sources_remain_build_eligible(self):
        ignored=set(re.findall(r"^\s+- '([^']+)'\s*$",self.push,re.M))
        for source in ("app/build.gradle.kts","app/src/main/java/com/michelslab/louderme/ui/LouderMeApp.kt",
                       "native/audio-engine/include/louderme/AudioEngine.hpp"):
            self.assertNotIn(source,ignored)
        self.assertIn("  release_guard:\n",self.source)
        self.assertIn("  publish_private_release:\n",self.source)

if __name__=="__main__":
    unittest.main()
