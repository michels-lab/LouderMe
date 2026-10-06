# LouderMe Desktop

Native Windows edition of LouderMe.

## Technology
- .NET 10 Windows
- WinForms
- NAudio 3.1.0
- self-contained single-file `.exe`
- Inno Setup installer

No browser or WebView is used for the desktop UI.

## Current validated scope
The first desktop milestone exposes the real Windows default playback endpoint, system master volume, mute state, device refresh and user-controlled **Start LouderMe with Windows** behavior.

The UI intentionally does **not** label the Windows 0–100 endpoint volume as 100–250% boost. System-wide gain above the normal Windows endpoint maximum needs a separately validated audio-processing implementation.

## Startup
Startup is opt-in and stored in:

`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\LouderMe`

This starts LouderMe when the current Windows user signs in and does not require administrator privileges.
