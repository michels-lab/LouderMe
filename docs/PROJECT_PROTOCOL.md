# LouderMe Project Protocol

LouderMe follows the shared **Michel Software Standards**.

## App-specific requirements
- permanent `PROJECT_LOG.md`;
- About from day one;
- proprietary license;
- real build-derived version;
- CI that validates current HEAD;
- release publication only after successful validation;
- automatic update awareness on supported production channels;
- Michel's Lab canonical identity/assets;
- honest engine state — never display a fake working boost.

## Audio-specific release rule
A build may say **Engine attached** when Android reports the effect enabled and under LouderMe control. It may not say **system-wide boost validated** until a real-device A/B test with external app audio passes.

## Platform-specific rule
The foreground audio service is a Google Play `specialUse` case and must be declared honestly in Play Console.
