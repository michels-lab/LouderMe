# LouderMe Privacy Policy — draft for production review

**App:** LouderMe  
**Developer:** Michel's Lab / Michel Duarte  
**Contact:** realmichelduarte@gmail.com  
**Draft date:** 2026-10-05

## Overview
LouderMe is an Android audio-control application. Its boost and equalizer implementation uses Android audio-effect APIs locally on the device.

## Audio
LouderMe does not record microphone audio, capture media audio, upload audio, or store the audio content being played.

The app does not request microphone permission.

## Local app data
LouderMe stores local configuration such as:
- selected boost percentage;
- equalizer preset;
- equalizer enabled state;
- requested equalizer band levels;
- audio/update state required for the local UI;
- a temporarily downloaded APK when a sideload update is pending.

## Network

### Google Play flavor
Uses Google Play's in-app update service.

### Michel's Lab direct flavor
Makes HTTPS requests to a public Michel's Lab/GitHub-hosted update manifest and APK URL to:
- check the current release version;
- download an official update.

No private GitHub token is shipped in the app.

The updater validates the APK checksum, package identity, version and signing identity before handing it to Android.

## Permissions
Common permissions:
- Internet;
- modify audio settings;
- foreground service;
- foreground service special-use;
- notifications.

The sideload flavor additionally declares `REQUEST_INSTALL_PACKAGES` so Android can allow the user to approve installation of a verified downloaded update.

The Google Play flavor does not declare that sideload permission.

## Analytics / advertising
The current build contains no advertising or analytics SDKs.

## Sale of data
LouderMe does not sell user data.

## Changes
Re-audit before production if analytics, ads, cloud accounts, audio capture, crash reporting or additional network services are added.
