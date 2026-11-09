# LouderMe Granada 2026-11-08 — Crash-safe audio preferences

## Verified root cause
The Windows app previously called File.WriteAllText directly on desktop-settings.json. That operation truncates existing preferences immediately; a process crash or storage failure could leave malformed JSON. The next launch then silently reset the owner's seven EQ gains, preset and Boost level to defaults.

## Code
The real settings store now writes complete UTF-8 JSON to a unique same-directory .tmp, flushes that file to disk, and uses File.Move(overwrite: true) to publish atomically. Errors before the move leave prior valid preferences byte-identical and temporary files are removed in finally on handled failures. Out-of-range and non-finite EQ values are normalized before serialization. A corrupt old file is not overwritten by a read. The existing DeveloperMode.DataDirectory selection remains unchanged, preserving stable/developer separation.

## Real executable regression
A new --check-settings-contract mode uses actual filesystem temporary directories. It exercises roundtripping Boost/EQ, an injected IOException **after staging and flush but before rename**, confirms the former JSON is unchanged, checks Developer isolation, sanitization of Infinity/NaN and a read-only fallback for corrupt old JSON. It runs in both build and stable-release CI on Windows and is protected by the release-parity negative mutation test.

## Limits
Windows build CI on the exact PR HEAD is required before merge. CI is not evidence that Equalizer APO is attached or that a signed virtual driver processes physical sound. No RC and no stable release.
