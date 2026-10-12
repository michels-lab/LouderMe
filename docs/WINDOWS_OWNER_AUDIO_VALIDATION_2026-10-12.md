# Windows audio — owner validation (2026-10-12)

- **Installed release:** LouderMe Desktop v0.1.13, official Michel's Lab unified release (Android v0.1.12 and Windows v0.1.13).
- **Reported result:** owner confirms, in their words, “ya funciona en windows” after previously reporting that Windows v0.1.12 controls responded but playback sound did not change.
- **Evidence class:** owner-reported functional result on a real Windows PC; not an automated CI test, measured acoustic test, or device compatibility certification.
- **Issue:** #74 user-reported silent Boost/EQ defect closed as completed based on owner's feedback. No claim that every boost percentage, EQ preset or output device has been independently verified.
- **Remaining engineering:** stable Windows still depends on Equalizer APO; native Windows virtual endpoint/driver and owned system-wide DSP integration remain in progress in #46 and #48. Physical regression across devices/restarts, actual digital gain and clipping/latency analysis are separate pending tests.
- **Official release:** https://github.com/michels-lab/LouderME-Releases/releases/tag/louderme-android-v0.1.12-desktop-v0.1.13
