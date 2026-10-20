"""Hermetic regression tests for installed Developer Edition QA harness.

No fake screenshots are accepted as installed evidence; these only verify
bounded recovery selection and exact package-identity rules for the harness.
"""
from __future__ import annotations

import sys
import unittest
from pathlib import Path
from unittest.mock import patch
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import capture_android_developer as dev


def root(xml: str) -> ET.Element:
    return ET.fromstring(xml)


class DeveloperHarnessTests(unittest.TestCase):
    def tearDown(self):
        dev._launcher_recovered = False

    def test_exact_system_anr_then_actual_developer_ui_must_be_seen(self):
        launcher = root(
            '<hierarchy><node text="Pixel Launcher isn&#39;t responding" '
            'bounds="[0,0][400,500]"><node text="Close app" clickable="true" '
            'bounds="[20,100][180,160]" /></node></hierarchy>'
        )
        live_app = root(
            '<hierarchy><node text="DEV &#183; Audit / Notes" '
            'bounds="[10,10][300,80]" /></hierarchy>'
        )
        with patch.object(dev, "hierarchy", side_effect=[launcher, live_app]), \
             patch.object(dev, "dismiss_system_launcher_anr", return_value=True) as dismiss, \
             patch.object(dev, "adb") as adb, \
             patch.object(dev.time, "sleep"):
            actual = dev.wait_for_text("DEV · Audit / Notes", timeout=15)
        self.assertIs(actual, live_app)
        self.assertTrue(dev._launcher_recovered)
        dismiss.assert_called_once_with(launcher)
        adb.assert_called_once_with("shell", "am", "start", "-W", "-n",
                                    "com.michelslab.louderme.developer/com.michelslab.louderme.MainActivity")

    def test_do_not_mistake_a_public_about_page_for_developer_ui(self):
        ordinary = root('<hierarchy><node text="About" bounds="[0,0][90,90]" /></hierarchy>')
        with patch.object(dev, "hierarchy", return_value=ordinary), \
             patch.object(dev, "dismiss_system_launcher_anr", return_value=False) as dismiss, \
             patch.object(dev, "adb") as adb, \
             patch.object(dev.time, "monotonic", side_effect=[0, 0.1, 1.5]), \
             patch.object(dev.time, "sleep"):
            with self.assertRaisesRegex(RuntimeError, "DEV"):
                dev.wait_for_text("DEV · Audit / Notes", timeout=1)
        dismiss.assert_called_once_with(ordinary)
        adb.assert_not_called()
        self.assertFalse(dev._launcher_recovered)

    def test_real_installed_developer_activity_is_not_public_package_alias(self):
        self.assertEqual("com.michelslab.louderme.developer", dev.PACKAGE)
        self.assertNotEqual(dev.PACKAGE, dev.PUBLIC_PACKAGE)
        self.assertEqual("com.michelslab.louderme.MainActivity", dev.MAIN_ACTIVITY)


if __name__ == "__main__":
    unittest.main()
