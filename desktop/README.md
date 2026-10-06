# LouderMe Desktop

Native Windows edition of LouderMe.

## Technology
- .NET 10 Windows
- WinForms
- NAudio 3.1.0
- self-contained single-file `.exe`
- Inno Setup installer
- Equalizer APO 1.4.2 for system-wide Windows gain and EQ

No browser or WebView is used for the desktop UI.

## Audio model
LouderMe separates two controls:

1. **System Output 0–100%** controls the Windows endpoint master volume.
2. **Global Boost 100–250%** controls post-mix digital gain through the Windows APO effects path.

Boost uses:

`gain_dB = 20 * log10(percent / 100)`

Examples:
- 100% = 0.00 dB
- 150% = +3.52 dB
- 200% = +6.02 dB
- 250% = +7.96 dB

LouderMe does not relabel the Windows 0–100 volume endpoint as 250%.

## System-wide engine
LouderMe integrates with Equalizer APO 1.4.2 for the Windows system-effect layer.

- Official x64 installer SHA-256:
  `7403be7427bbe1936a40dded082829b6e217fc4f5990fee5cba501f0ae055afa`
- LouderMe opens the official Equalizer APO download page instead of redistributing the installer.
- The playback device must be selected in Equalizer APO Configurator.
- LouderMe manages `LouderMe.txt` plus a single `Include: LouderMe.txt` entry.
- Before changing the main config, LouderMe creates `config.txt.louderme.bak` once.
- ASIO and WASAPI exclusive-mode playback can bypass Windows APO effects.

## Equalizer
Desktop uses the same seven product-facing bands as Android:
60 Hz, 150 Hz, 400 Hz, 1 kHz, 2.5 kHz, 6 kHz and 12 kHz.

Presets: Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones and Custom.

Each band is limited to -10 to +10 dB.

## Headroom
Positive preamp consumes digital headroom. High boost can cause the downstream Windows output path to limit or compress loud material. LouderMe reports requested digital gain, not measured acoustic dB SPL.

## Startup
Startup is opt-in and stored in:

`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\LouderMe`

This starts LouderMe when the current Windows user signs in.

## Validation boundary
CI can validate compilation, packaging, gain math and configuration generation. Endpoint attachment, reboot behavior, audio-driver compatibility and audible high-gain quality require validation on a physical Windows machine.
