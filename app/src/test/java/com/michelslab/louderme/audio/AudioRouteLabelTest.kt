package com.michelslab.louderme.audio

import android.media.AudioDeviceInfo
import org.junit.Assert.assertEquals
import org.junit.Test

class AudioRouteLabelTest {
    @Test
    fun btAndHeadphonesTakePriorityOverAvailableSpeaker() {
        assertEquals(
            "Bluetooth",
            AudioRouteLabel.describe(
                listOf(AudioDeviceInfo.TYPE_BUILTIN_SPEAKER, AudioDeviceInfo.TYPE_BLUETOOTH_A2DP),
            ),
        )
        assertEquals(
            "Headphones / USB",
            AudioRouteLabel.describe(
                listOf(AudioDeviceInfo.TYPE_USB_HEADSET, AudioDeviceInfo.TYPE_BUILTIN_SPEAKER),
            ),
        )
    }

    @Test
    fun emptyOrUnknownRouteIsNotMistakenForSpeakerOrBluetooth() {
        assertEquals("System output", AudioRouteLabel.describe(emptyList()))
        assertEquals("System output", AudioRouteLabel.describe(listOf(-999)))
        assertEquals(
            "Phone speaker",
            AudioRouteLabel.describe(listOf(AudioDeviceInfo.TYPE_BUILTIN_SPEAKER)),
        )
    }
}
