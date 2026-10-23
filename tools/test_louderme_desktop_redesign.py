#!/usr/bin/env python3
"""Regression for the user-reported blank About + pasted logo + inert EQ.

First validate the actual source wiring. With --evidence, also inspect PNG
captures made by the installed real WinForms process (not generated mockups).
"""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DESKTOP = ROOT / "desktop/src/LouderMeDesktop"


def source_contract() -> None:
    about = (DESKTOP / "AboutForm.cs").read_text(encoding="utf-8")
    main = (DESKTOP / "MainForm.cs").read_text(encoding="utf-8")
    graphics = (DESKTOP / "BrandingControls.cs").read_text(encoding="utf-8")
    prep = (ROOT / "desktop/installer/prepare-branding.ps1").read_text(
        encoding="utf-8")
    for required in (
        "AutoScroll = true",
        "card.AutoSize = false",
        "card.Width = width",
        "card.Height = body.Bottom",
        "body.GetPreferredSize",
        "internal void AssertVisualLayout()",
        '"about-author"',
        '"about-updates"',
        '"about-legal"',
    ):
        if required not in about:
            raise AssertionError(f"About layout fix is missing: {required}")
    for required in (
        "about.AssertVisualLayout()",
        'Name = "eq-vertical-faders"',
        'Name = "eq-precision-inputs"',
        "new EqBandFaderControl",
        "_eqBands[index].Value = (decimal)gain",
        "_settings.EqualizerGainsDb[index] = (float)value.Value",
        "ApplyProcessing();",
        "fader.Enabled = controlsEnabled",
        'equalizer-{viewport}.png',
    ):
        if required not in main:
            raise AssertionError(f"Visual EQ/APO wiring is missing: {required}")
    for required in (
        "internal sealed class EqBandFaderControl",
        "OnMouseMove",
        "OnKeyDown",
        "OnMouseDoubleClick",
        "GainEdited?.Invoke",
        "using var shade",
        'Height = 182;',
    ):
        if required not in graphics:
            raise AssertionError(f"Interactive 7-band drawing missing: {required}")
    if "Color.FromArgb(247, 250, 254)" in main + about + (
            DESKTOP / "SplashForm.cs").read_text(encoding="utf-8"):
        raise AssertionError("Old white logo sticker background returned")
    for required in ('fill="#edf6ff"', "-trim +repage",
                     "Remove-Item $temporary"):
        if required not in prep:
            raise AssertionError(f"Dark official logo derivative missing: {required}")


def check_installed_screenshots(directory: Path) -> None:
    for layout in ("compact", "wide"):
        about_path = directory / f"about-{layout}.png"
        eq_path = directory / f"equalizer-{layout}.png"
        home_path = directory / f"home-{layout}.png"
        for path in (about_path, eq_path, home_path):
            if not path.exists():
                raise AssertionError(f"Real rendered UI evidence missing: {path}")
        with Image.open(about_path) as image:
            img = image.convert("RGB")
            w, h = img.size
            if w < 470 or h < 420:
                raise AssertionError("About screenshot too small")
            crop = img.crop((50, 72, w - 55, min(h - 30, 380)))
            px = list(crop.getdata())
            bright = sum(max(r, g, b) > 178 and r + g + b > 360
                         for r, g, b in px)
            if bright < 250:
                raise AssertionError(
                    f"About {layout} screenshot appears blank "
                    f"({bright} visible text/mark pixels)")
        with Image.open(home_path) as image:
            img = image.convert("RGB")
            w, h = img.size
            crop = img.crop((30, 55, w - 30, min(h, 170)))
            pixels = list(crop.getdata())
            white = sum(min(p) > 237 for p in pixels)
            if white > len(pixels) // 4:
                raise AssertionError(
                    f"Home {layout} still contains pasted white logo panel")
        with Image.open(eq_path) as image:
            img = image.convert("RGB")
            w, h = img.size
            crop = img.crop((40, 145, w - 40, h - 30))
            pixels = list(crop.getdata())
            blue = sum(b > 160 and b > r * 1.3 and b > g * 1.08
                       for r, g, b in pixels)
            if blue < 80:
                raise AssertionError(
                    f"Seven-band EQ {layout} evidence lacks visual active data")
    print("PASS: installed compact/wide Home, About and EQ have rendered pixels "
          "and no white logo sticker")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence", type=Path)
    args = parser.parse_args()
    source_contract()
    if args.evidence:
        check_installed_screenshots(args.evidence)
    else:
        print("PASS: About sizing, transparent logo, live EQ+APO wiring source")


if __name__ == "__main__":
    main()
