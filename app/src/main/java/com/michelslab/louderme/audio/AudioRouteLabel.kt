package com.michelslab.louderme.audio

import android.media.AudioDeviceInfo

/** Human-readable anticipated media route, not proof of DSP attachment. */
internal object AudioRouteLabel {
    fun describe(types: Collection<Int>): String = when {
        types.any {
            it == AudioDeviceInfo.TYPE_BLUETOOTH_A2DP ||
                it == AudioDeviceInfo.TYPE_BLUETOOTH_SCO ||
                it == AudioDeviceInfo.TYPE_BLE_HEADSET ||
                it == AudioDeviceInfo.TYPE_BLE_SPEAKER
        } -> "Bluetooth"

        types.any {
            it == AudioDeviceInfo.TYPE_WIRED_HEADPHONES ||
                it == AudioDeviceInfo.TYPE_WIRED_HEADSET ||
                it == AudioDeviceInfo.TYPE_USB_HEADSET ||
                it == AudioDeviceInfo.TYPE_USB_DEVICE
        } -> "Headphones / USB"

        types.contains(AudioDeviceInfo.TYPE_BUILTIN_SPEAKER) -> "Phone speaker"
        else -> "System output"
    }
}
