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
- Canonical Michel's Lab developer portrait.
- Official links: Instagram, LinkedIn, GitHub, email.

## Visual family
About follows the same Michel's Lab dark/cyan/gold product-family language as the LouderMe Audio Workspace and current IG Cleaner Pro design, while retaining the canonical Michel's Lab identity hierarchy.

## Required content
- build-derived version and versionCode;
- package ID;
- software-update state;
- Check for updates / Install update action;
- developer identity and portrait;
- privacy statement;
- copyright/license.

## Update behavior
Play-installed builds query Google Play automatically. Update state is visible on both Home and About.

Direct/debug APKs state that the Play update channel is unavailable rather than pretending to be current.

## Privacy summary
LouderMe does not record, capture, store or upload audio content. Boost and EQ configuration remain local.

## Rules
- Never hard-code a production version separately from `BuildConfig`.
- Keep Smart Boost labeled Beta until validated.
- Never claim universal compatibility solely because a session-0 effect attached.
