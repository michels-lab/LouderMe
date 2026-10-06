# LouderMe Desktop

Native Windows edition of LouderMe.

## Packaging
- .NET 10 WinForms application.
- Self-contained single-file `LouderMe.exe`.
- Inno Setup installer: `LouderMe-Setup-vX.Y.Z.exe`.
- Official LouderMe flowing-waveform identity is used for the Windows executable and installer icon.
- No WebView and no HTML UI.

## Current desktop foundation
- Real Windows default-output master volume control.
- Real mute/unmute.
- Quick 25/50/75/100% endpoint levels.
- LouderMe 100–250% target selector with explicit diagnostics.
- Start with Windows toggle using the current-user startup registry key.
- Startup launches minimized to the system tray.
- Close button hides to tray; the tray menu can reopen or exit.

## Audio-engine boundary
The Windows Core Audio endpoint itself does not expose a portable global gain control above its actual hardware maximum. The desktop foundation therefore does **not** mislabel endpoint 100% as 125–250%.

Targets above 100% are persisted for LouderMe's planned native Windows DSP/APO module. A future release must implement and validate that module before claiming system-wide >100% Windows amplification or EQ.

## Version
Desktop versioning is independent from Android. Initial desktop foundation: **0.1.0**.
