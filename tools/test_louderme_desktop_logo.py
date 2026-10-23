#!/usr/bin/env python3
"""Check LouderMe Windows branding comes from the approved SVG, not a faux header.

Windows CI calls this after prepare-branding.ps1 and before build. The runtime
WinForms smoke separately validates the real main-header geometry at 480/600px.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SVG = ROOT / "app/src/main/assets/branding/louderme/official-lockup.svg"
PNG = ROOT / "desktop/src/LouderMeDesktop/Assets/LouderMeOfficialLockup.png"
ICON = ROOT / "desktop/src/LouderMeDesktop/Assets/LouderMe.ico"
PROJECT = ROOT / "desktop/src/LouderMeDesktop/LouderMeDesktop.csproj"
MAIN = ROOT / "desktop/src/LouderMeDesktop/MainForm.cs"
SPLASH = ROOT / "desktop/src/LouderMeDesktop/SplashForm.cs"
ABOUT = ROOT / "desktop/src/LouderMeDesktop/AboutForm.cs"


def main() -> None:
    xml = ET.parse(SVG).getroot()
    ns = {"svg": "http://www.w3.org/2000/svg"}
    if xml.attrib.get("viewBox") != "0 0 1600 600":
        raise AssertionError("Unexpected LouderMe canonical lockup viewBox")
    labels = [t.text for t in xml.findall(".//svg:text", ns)]
    if labels != ["Louder", "Me", "SOUND THAT LIFTS YOU"]:
        raise AssertionError(f"Canonical LouderMe lockup changed: {labels!r}")
    if len(xml.findall(".//svg:path", ns)) != 1:
        raise AssertionError("Expected one official flowing waveform")

    with Image.open(PNG) as image:
        if image.format != "PNG" or image.width < 600 or image.height < 200:
            raise AssertionError("Missing/low-resolution official lockup raster")
        if not (1.4 < image.width / image.height < 5.0):
            raise AssertionError("Dark-surface canonical logo crop/aspect ratio invalid")
        rgba = image.convert("RGBA")
        actual = [p for p in rgba.getdata() if p[3] > 180]
        if len(actual) < 2500:
            raise AssertionError("Official lockup rendered blank/transparent")
        light_wordmark = sum(r > 205 and g > 215 and b > 220
                             for r, g, b, _ in actual)
        blue = sum(b > 175 and b > r * 1.35 and b > g * 1.06
                   for r, g, b, _ in actual)
        if light_wordmark < 600 or blue < 500:
            raise AssertionError("Official LouderMe light wordmark/blue mark missing")
        if rgba.getpixel((0, 0))[3] > 30:
            raise AssertionError("Dark-surface lockup must have a transparent background")

    with Image.open(ICON) as image:
        if image.format != "ICO" or max(image.size) < 32:
            raise AssertionError("Official ICO missing from Windows title bar build")

    project = PROJECT.read_text(encoding="utf-8")
    for required in ("LouderMeOfficialLockup.png", 'LogicalName="LouderMeDesktop.Assets.LouderMe.ico"'):
        if required not in project:
            raise AssertionError(f"Windows did not embed official asset: {required}")

    main_code = MAIN.read_text(encoding="utf-8")
    if "header.Controls.Add(new WaveformMarkControl" in main_code:
        raise AssertionError("Old faux logo would paint over LouderMe title")
    for source in (MAIN, SPLASH, ABOUT):
        text = source.read_text(encoding="utf-8")
        if "Assets.LouderMeOfficialLockup.png" not in text:
            raise AssertionError(f"{source.name} does not render approved lockup")
    if "new[] { 480, 600 }" not in main_code or "lockupBounds.IntersectsWith(about)" not in main_code:
        raise AssertionError("Missing live compact-logo layout regression")
    print("PASS: canonical SVG text/geometry, transparent dark-surface lockup,"
          " official ICO, embedded resources, all three real screens and"
          " 480/600px WinForms layout guard")


if __name__ == "__main__":
    main()
