"""Hermetic native-hierarchy selection tests for the real installed UI runner.

These fixtures test target selection only, not device navigation or audio.
"""
import sys
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from capture_android_visual import point_for_label, dismiss_system_launcher_anr


class ComposeTargetSelectionTests(unittest.TestCase):
    def test_clickable_parent_preferred_to_tiny_compose_text_child(self):
        root = ET.fromstring("""
        <hierarchy>
          <node text="" clickable="false" bounds="[0,0][720,1280]">
            <node text="" clickable="true" bounds="[540,0][710,88]">
              <node text="About" clickable="false" bounds="[623,25][661,52]" />
            </node>
          </node>
        </hierarchy>
        """)
        self.assertEqual((625, 44), point_for_label(root, "About"))

    def test_real_label_without_clickable_parent_uses_own_bounds(self):
        root = ET.fromstring("""
        <hierarchy>
          <node clickable="false">
            <node text="Back" bounds="[4,10][60,50]" clickable="false" />
          </node>
        </hierarchy>
        """)
        self.assertEqual((32, 30), point_for_label(root, "Back"))

    def test_missing_or_empty_hit_target_fails_closed(self):
        root = ET.fromstring("""
        <hierarchy>
          <node text="About" clickable="true" bounds="[1,1][1,1]"/>
        </hierarchy>
        """)
        with self.assertRaises(RuntimeError):
            point_for_label(root, "About")
        with self.assertRaises(RuntimeError):
            point_for_label(root, "Nonexistent")


    def test_known_pixel_launcher_anr_is_dismissed_once(self):
        root = ET.fromstring("""
        <hierarchy>
          <node text="Pixel Launcher isn't responding" clickable="false">
            <node text="Close app" clickable="true" bounds="[10,40][210,92]" />
            <node text="Wait" clickable="true" bounds="[220,40][410,92]" />
          </node>
        </hierarchy>
        """)
        with patch("capture_android_visual.adb") as adb:
            self.assertTrue(dismiss_system_launcher_anr(root))
            adb.assert_called_once_with("shell", "input", "tap", "110", "66")

    def test_never_suppress_louderme_or_unrelated_application_anr(self):
        for title in ("LouderMe isn't responding", "System UI isn't responding"):
            root = ET.fromstring(
                f'<hierarchy><node text="{title}"><node text="Close app" '
                'clickable="true" bounds="[0,0][120,60]" /></node></hierarchy>')
            with patch("capture_android_visual.adb") as adb:
                self.assertFalse(dismiss_system_launcher_anr(root))
                adb.assert_not_called()

    def test_known_launcher_without_close_is_failure_not_pass(self):
        root = ET.fromstring(
            '<hierarchy><node text="Pixel Launcher isn&#39;t responding" /></hierarchy>')
        with self.assertRaisesRegex(RuntimeError, "Close app"):
            dismiss_system_launcher_anr(root)


if __name__ == "__main__":
    unittest.main()
