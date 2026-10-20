"""Hermetic native-hierarchy selection tests for the real installed UI runner.

These fixtures test target selection only, not device navigation or audio.
"""
import sys
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from capture_android_visual import point_for_label


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


if __name__ == "__main__":
    unittest.main()
