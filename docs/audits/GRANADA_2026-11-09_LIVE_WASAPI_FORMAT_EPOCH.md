# Granada — 2026-11-09: format-epoch recheck while Windows WASAPI route is live

## Concrete fault
The experimental virtual→native DSP→physical bridge checked selected IDs and DeviceState.Active on every management tick. Windows Sound, USB/HDMI/Bluetooth device properties and audio-engine renegotiation can replace a mix format without changing the endpoint ID or its Active state. Continuing with the old PcmFormatAdapter risks old channel order, stale integer/float conversion, incorrect queue frame alignment and damaged sound.

## Functional fix
Capture immutable source and sink mix-format stamps at session startup, recording sample rate, channels, container/valid bit depth, bytes per frame/second, encoding, extender subtype and speaker mask. Once per second **on the console thread only**, re-query the exact selected MMDevice objects. On difference, throw AudioRouteRecoverableException (75), stop existing capture/render in the existing finally block and require a fresh preflight before retrying; opt-in --auto-reconnect preserves exact endpoint IDs and the existing bounded retry counter. Never choose new default devices or touch audio callback COM access. COM property failures are recoverable, not silent substitution.

## Numeric Windows CI
Actual --check-route-preflight calls the new format-epoch self-tests: format-identical independent objects accepted; source/sink rate changes, mono/stereo/5.1/7.1 changes, float/int and PCM24-in-32 type changes, explicit 5.1 back↔side and stereo position mask switches must invalidate. These are device-free tests, not evidence of actual physical audio.

## Limits
A transient mix-format change and reversal entirely within a 1-second observation interval might not be detected. Windows may stop WASAPI streams before the next poll; existing RecordingStopped/PlaybackState guards still apply. No real signed SysVAD endpoint, operator-authorized driver installation or acoustic gain proof is claimed. No release/RC. #48 remains open.
