# LouderMe v0.1.5 — High-Gain Peak Protection

## What changed

LouderMe now adds an explicit limiter strategy to the `DynamicsProcessing` fallback while preserving the already-validated `LoudnessEnhancer` path.

- 175%: light peak limiting.
- 200%: medium peak limiting.
- 225%: high peak limiting.
- 250%: maximum LouderMe limiter profile.

The primary LoudnessEnhancer route is unchanged. Android documents that LoudnessEnhancer compresses samples that would exceed the supported sample range, so v0.1.5 surfaces that behavior instead of stacking an unnecessary second processor onto the known-good path.

If a device exposes DynamicsProcessing but rejects the explicit limiter parameters, LouderMe keeps the fallback engine alive and reports that the limiter was unavailable rather than failing the entire boost engine.

## Still to validate

- CI build/tests for both Play and sideload flavors.
- Audible EQ behavior on the target Samsung device.
- Audible high-gain quality at 225% and 250%.

Peak limiting addresses digital overload/clipping behavior. It does not measure or guarantee safe acoustic SPL.
