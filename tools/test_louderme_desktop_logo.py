#!/usr/bin/env python3
"""Check LouderMe Windows branding comes from the approved SVG, not a faux header.

Windows CI calls this after prepare-branding.ps1 and before build. The runtime
WinForms smoke separately validates the real main-header geometry at 480/600px.
"""
from pathlib import Path
import hashlib
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
        if abs(image.width / image.height - 1600 / 600) > 0.04:
            raise AssertionError("Official logo was distorted or cropped")
        rgba = image.convert("RGBA")
        actual = [p for p in rgba.getdata() if p[3] > 180]
        if len(actual) < 2500:
            raise AssertionError("Official lockup rendered blank/transparent")
        navy = sum(r < 65 and g < 105 and b < 135 for r, g, b, _ in actual)
        blue = sum(b > 175 and b > r * 1.35 and b > g * 1.06
                   for r, g, b, _ in actual)
        if navy < 600 or blue < 500:
            raise AssertionError("Official LouderMe navy/blue wordmark missing")

    dark_png = ROOT / "desktop/src/LouderMeDesktop/Assets/LouderMeOfficialDarkLockup.png"
    with Image.open(dark_png) as dark:
        if dark.format != "PNG" or dark.size != (image_width := 960, 360):
            raise AssertionError("Dark lockup missing or resized incorrectly")
        colors = [pixel for pixel in dark.convert("RGBA").getdata() if pixel[3] > 180]
        white = sum(r > 190 and g > 190 and b > 190 for r, g, b, _ in colors)
        blue = sum(b > 170 and b > r * 1.3 for r, g, b, _ in colors)
        if white < 400 or blue < 300:
            raise AssertionError("Transparent dark lockup lost white/blue identity")

    canonical_studio = ROOT / "desktop/src/LouderMeDesktop/Assets/MichelsLabCanonicalLockup.png"
    if not canonical_studio.is_file() or canonical_studio.stat().st_size != 526482:
        raise AssertionError("Original verified Michel's Lab studio PNG missing")
    if hashlib.sha256(canonical_studio.read_bytes()).hexdigest() != (
        "d03e8549af2cbcb04c2329029a02f0cd078972ce1b1ff948ef66c3aebe7d10f4"
    ):
        raise AssertionError("Canonical Michel's Lab bytes modified or corrupted")
    with Image.open(canonical_studio) as approved:
        approved.load()
        if approved.size != (2172, 724):
            raise AssertionError("Michel's Lab official lockup dimensions modified")

    studio = ROOT / "desktop/src/LouderMeDesktop/Assets/MichelsLabDarkLockup.png"
    with Image.open(studio) as image:
        if image.format != "PNG" or image.width < 180 or image.height < 60:
            raise AssertionError("Prepared official Michel's Lab logo is missing")
        rgb = image.convert("RGB")
        colors = list(rgb.getdata())
        surface = (12, 20, 34)
        content = sum(sum(abs(p[i] - surface[i]) for i in range(3)) > 45 for p in colors)
        magenta = sum(r > 220 and b > 220 and g < 70 for r, g, b in colors)
        if content < 1000:
            raise AssertionError("Michel's Lab actual logo rendered blank on dark surface")
        if magenta > len(colors) // 10:
            raise AssertionError("Michel's Lab logo retained broken magenta background")

    with Image.open(ICON) as image:
        if image.format != "ICO" or max(image.size) < 32:
            raise AssertionError("Official ICO missing from Windows title bar build")

    project = PROJECT.read_text(encoding="utf-8")
    for required in ("LouderMeOfficialLockup.png", "MichelsLabCanonicalLockup.png",
                     "MichelsLabDarkLockup.png",
                     'LogicalName="LouderMeDesktop.Assets.LouderMe.ico"'):
        if required not in project:
            raise AssertionError(f"Windows did not embed official asset: {required}")

    main_code = MAIN.read_text(encoding="utf-8")
    if "header.Controls.Add(new WaveformMarkControl" in main_code:
        raise AssertionError("Old faux logo would paint over LouderMe title")
    for source in (MAIN, SPLASH, ABOUT):
        text = source.read_text(encoding="utf-8")
        if "Assets.LouderMeOfficialDarkLockup.png" not in text:
            raise AssertionError(f"{source.name} does not render approved lockup")
    if "new[] { 480, 600 }" not in main_code or "lockupBounds.IntersectsWith(about)" not in main_code or "SizeType.Absolute, 308" not in main_code:
        raise AssertionError("Missing left-anchored header layout regression")
    if "MichelsLabDarkLockup.png" not in ABOUT.read_text(encoding="utf-8"):
        raise AssertionError("About must show canonical studio logo")
    if "about-author-portrait" not in ABOUT.read_text(encoding="utf-8"):
        raise AssertionError("About must feature large owner photo")
    print("PASS: approved SVG, dark lockup, canonical Michel's Lab logo,"
          " official ICO, embedded resources, all three real screens and"
          " 480/600px WinForms layout guard")


if __name__ == "__main__":
    main()
