# LouderMe Privacy Policy — draft for production review

**App:** LouderMe  
**Developer:** Michel's Lab / Michel Duarte  
**Contact:** realmichelduarte@gmail.com  
**Draft date:** 2026-10-05

## Overview
LouderMe is an Android audio-control application. Its current audio-processing implementation uses Android audio-effect APIs on the device.

## Audio
LouderMe does not record microphone audio, capture media audio, upload audio, or store the content being played.

The app does not request microphone permission.

## Local app data
LouderMe may store local settings such as the selected boost percentage and last audio-engine status so the interface can preserve user choices.

## Network / third-party services
Google Play-installed builds use Google Play's in-app update service to check for and install official app updates.

The current LouderMe build does not contain advertising or analytics SDKs.

## Permissions
Current permissions include:
- modify audio settings;
- foreground service;
- foreground service special-use declaration;
- notification permission so the foreground audio service can remain visible to the user.

## Sale / advertising
LouderMe does not sell user data. The current build does not include advertising.

## Changes
This draft must be reviewed again before production if analytics, ads, cloud accounts, audio capture, crash reporting, or additional network services are added.
