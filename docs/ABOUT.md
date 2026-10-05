# LouderMe About specification

## Product
- App: **LouderMe**
- Studio: **Michel's Lab**
- Tagline: **Make everything louder.**
- Package: `com.michelslab.louderme`

## Developer identity
- Display name: **Michel Duarte**
- Formal/legal name: **Michel Armando Duarte Flores**
- Role: **Developer · Michel's Lab**
- Canonical portrait: shared Michel's Lab developer portrait.
- Official links: Instagram, LinkedIn, GitHub, email.

## Required About content
- build-derived version and versionCode;
- package ID;
- software-update state;
- Check for updates / Install update action;
- developer identity and portrait;
- privacy statement;
- copyright/license.

## Update behavior
Play-installed builds query Google Play for newer versions. Direct/debug APKs must state that the Play update channel is unavailable instead of pretending the app is current.

## Privacy summary
LouderMe does not record, capture, store, or upload audio content. The first engine stores only local effect state/settings.

## Rules
- Never hard-code a production version separately from `BuildConfig`.
- Keep Smart Boost labeled Beta while experimental.
- Never claim global amplification solely because the session-0 engine attached.
