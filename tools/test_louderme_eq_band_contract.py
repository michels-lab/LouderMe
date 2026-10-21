#!/usr/bin/env python3
"""Fail closed when LouderMe's native EQ slider index drifts from shipped platforms.

Source-contract validation only: it does not compare differing device EQ
hardware transfer curves and never claims physical sound is working.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
EXPECTED = (60, 150, 400, 1000, 2500, 6000, 12000)
SOURCES = {
    "Native C++ DSP": (
        "native/audio-engine/include/louderme/AudioEngine.hpp",
        r"CenterHz\s*\{\s*([^}]*)\}",
    ),
    "Windows desktop APO UI": (
        "desktop/src/LouderMeDesktop/EqualizerPresets.cs",
        r"FrequenciesHz\s*=\s*\[([^\]]*)\]",
    ),
    "Android UI": (
        "app/src/main/java/com/michelslab/louderme/audio/EqualizerModels.kt",
        r"frequenciesHz\s*=\s*listOf\(([^)]*)\)",
    ),
}


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def extract(name: str, path: str, expression: str) -> tuple[int, ...]:
    match = re.search(expression, read(path), re.S)
    if not match:
        raise AssertionError(f"{name}: unable to find declared band centers")
    raw = match.group(1)
    # Require all tokens to be numeric literals; do not silently ignore
    # interpolated values or comments in a changed layout.
    tokens = [s.strip() for s in raw.split(",")]
    frequencies = []
    for token in tokens:
        if not re.fullmatch(r"\d[\d_]*(?:\.0+)?f?", token):
            raise AssertionError(f"{name}: unrecognized EQ band literal {token!r}")
        value = float(token.replace("_", "").removesuffix("f"))
        if not value.is_integer():
            raise AssertionError(f"{name}: fractional Hz not expected: {token}")
        frequencies.append(int(value))
    return tuple(frequencies)


def check() -> None:
    for platform, (path, expression) in SOURCES.items():
        actual = extract(platform, path, expression)
        if actual != EXPECTED:
            raise AssertionError(
                f"{platform}: expected {EXPECTED}, found {actual}; "
                "EQ band indexes would affect the wrong nominal frequencies"
            )

    managed = read("native/windows-bridge/Program.cs")
    if not re.search(
        r"ConfigureEqualizer\(true,\s*new float\[\]\s*\{\s*0,\s*0,\s*0,\s*8,\s*0,\s*0,\s*0\s*\}\)",
        managed,
    ):
        raise AssertionError("Windows managed/native 1kHz EQ proof must use band index 3")

    native_test = read("native/audio-engine/tests/AudioEngineTests.cpp")
    if not all(fragment in native_test for fragment in (
        "for (std::size_t band = 0; band < AudioEngine::BandCount; ++band)",
        "gains[band] = 8.0f",
        "productBands[band] * frame",
    )):
        raise AssertionError("C++ device-free response test must exercise all 7 nominal centers")

    print("PASS: native C++ / Windows APO UI / Android EQ share exact seven-band center order")
    print("PASS: Windows native interop uses 1kHz index 3 and C++ checks every center")
    print("LIMIT: this is source and synthetic DSP proof, NOT installed-driver/physical sound")


if __name__ == "__main__":
    check()
