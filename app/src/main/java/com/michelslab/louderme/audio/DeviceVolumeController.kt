package com.michelslab.louderme.audio

import android.content.Context
import android.media.AudioManager
import kotlin.math.roundToInt

data class DeviceVolumeUiState(
    val percent: Int = 0,
    val step: Int = 0,
    val maxStep: Int = 0,
    val muted: Boolean = false,
    val available: Boolean = true,
    val message: String = "Android media volume",
)

object DeviceVolumeMath {
    fun stepToPercent(step: Int, maxStep: Int): Int {
        if (maxStep <= 0) return 0
        return ((step.coerceIn(0, maxStep) * 100f) / maxStep).roundToInt()
            .coerceIn(0, 100)
    }

    fun percentToStep(percent: Int, maxStep: Int): Int {
        if (maxStep <= 0) return 0
        return ((percent.coerceIn(0, 100) / 100f) * maxStep).roundToInt()
            .coerceIn(0, maxStep)
    }
}

class DeviceVolumeController(context: Context) {
    private val audioManager =
        context.applicationContext.getSystemService(Context.AUDIO_SERVICE) as AudioManager

    fun read(): DeviceVolumeUiState {
        val maxStep = audioManager.getStreamMaxVolume(AudioManager.STREAM_MUSIC)
        val step = audioManager.getStreamVolume(AudioManager.STREAM_MUSIC)
        val muted = runCatching {
            audioManager.isStreamMute(AudioManager.STREAM_MUSIC)
        }.getOrDefault(step == 0)

        return DeviceVolumeUiState(
            percent = DeviceVolumeMath.stepToPercent(step, maxStep),
            step = step,
            maxStep = maxStep,
            muted = muted,
            available = maxStep > 0,
            message = if (maxStep > 0) {
                "Real Android media volume · system stream"
            } else {
                "Media volume is unavailable on this device."
            },
        )
    }

    fun setPercent(percent: Int): DeviceVolumeUiState {
        val maxStep = audioManager.getStreamMaxVolume(AudioManager.STREAM_MUSIC)
        require(maxStep > 0) { "Android media volume is unavailable." }

        val targetStep = DeviceVolumeMath.percentToStep(percent, maxStep)
        audioManager.setStreamVolume(
            AudioManager.STREAM_MUSIC,
            targetStep,
            0,
        )
        return read()
    }
}
