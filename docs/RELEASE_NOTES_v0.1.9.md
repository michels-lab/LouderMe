# LouderMe v0.1.9

Stable Android navigation hotfix by **Michel's Lab**.

## Fixed
- Android system Back / gesture Back now returns from **About** to the LouderMe main screen instead of closing the app.
- The in-app Back button and the Android system Back action now share the same navigation result.

## Preserved
- Pressing system Back from the main LouderMe screen still exits normally.
- Canonical Michel Duarte portrait and Michel's Lab About branding from v0.1.8.
- 100–250% boost, 7-band EQ, Play / Direct split, updater and Start with phone.

## Distribution
- Package: `com.michelslab.louderme`
- versionName: `0.1.9`
- versionCode: `10`

## Validation
Android CI checks the explicit `BackHandler(enabled = showAbout)` contract and rebuilds Play + Direct variants before publication.
