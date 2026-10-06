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


## Product identity and About

Desktop is a first-class LouderMe product surface, not a generic utility wrapper.

The native Windows UI now uses:
- the canonical flowing-waveform geometry in the main header and branded startup surface;
- waveform-derived live boost and EQ visual feedback;
- the canonical Michel Duarte portrait;
- the official Michel's Lab parent-brand lockup;
- a dedicated native About surface following Product → Author → Michel's Lab → Social hierarchy;
- canonical Instagram, Facebook, LinkedIn, GitHub and Email contact order.

The executable/shortcut/taskbar icon remains derived from the official LouderMe app-icon source.

## In-app stable updater

LouderMe Desktop reads the public stable manifest:

`https://raw.githubusercontent.com/realmichelduarte/michel-s-life-releases/main/louderme-desktop/latest.json`

Update behavior:
1. compare the published stable version against the running assembly/product version;
2. only expose install action when a newer version exists;
3. download the published installer into the user's local LouderMe update folder;
4. verify the installer SHA-256 from the manifest;
5. ask Windows to launch the verified installer.

The updater never treats an unverified download as installable.

## Windows publisher signing

The updater verifies published SHA-256 integrity, but **Authenticode publisher signing is still a separate release capability**.

Do not describe a Desktop build as signed unless the actual release artifact carries a valid Windows publisher signature. Unsigned/new-reputation builds can still trigger SmartScreen.


## Release trigger discipline

Desktop feature, branding, CI or workflow maintenance changes do not publish a stable release by themselves.

The stable publisher runs from an explicit versioned release note under `desktop/releases/vX.Y.Z.md` or a deliberate manual workflow dispatch. Editing the release workflow alone must not overwrite an already-published Desktop version.
