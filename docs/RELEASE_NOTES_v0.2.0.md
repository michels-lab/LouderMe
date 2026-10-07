# LouderMe Android v0.2.0

Stable-candidate Android feature release by **Michel's Lab**.

## Device Volume
- Adds a first-class **Device / Media Volume** control from 0–100%.
- Controls Android's real `AudioManager.STREAM_MUSIC` volume, the same media stream affected by the phone volume buttons.
- Displays the actual discrete system volume step and maximum step.
- Resyncs from Android every 750 ms while LouderMe is active so hardware-button/system-UI changes do not leave a stale slider.
- Device Volume remains explicitly separate from LouderMe Global Boost 100–250%.

## Reliability
- Adds tested percent ↔ Android volume-step mapping.
- Handles unavailable device-volume access without crashing the UI.
- Keeps About system Back behavior from v0.1.9.

## Bug fixes
- Restores the current canonical Michel's Lab lockup asset in the Android About surface and updates CI to reject the obsolete/corrupt interim asset.

## Distribution
- package: `com.michelslab.louderme`
- versionName: `0.2.0`
- versionCode: `11`

Publication is not implied by this candidate file; release still requires explicit authorization.
