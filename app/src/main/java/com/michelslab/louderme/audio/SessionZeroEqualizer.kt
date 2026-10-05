package com.michelslab.louderme.audio

import android.media.audiofx.AudioEffect
import android.media.audiofx.Equalizer
import kotlin.math.roundToInt

data class EqualizerApplyResult(
    val status: EqualizerStatus,
    val implementation: String,
    val message: String,
)

class SessionZeroEqualizer {
    private var equalizer: Equalizer? = null

    fun apply(state: EqualizerUiState): EqualizerApplyResult {
        if (!state.enabled) {
            runCatching { equalizer?.setEnabled(false) }
            return EqualizerApplyResult(
                status = EqualizerStatus.DISABLED,
                implementation = equalizer?.descriptor?.name ?: "Equalizer disabled",
                message = "EQ is disabled.",
            )
        }

        return runCatching {
            val effect = equalizer ?: Equalizer(0, 0).also {
                equalizer = it
            }

            val range = effect.bandLevelRange
            val minMb = range[0].toInt()
            val maxMb = range[1].toInt()
            val desired = EqualizerPresets.sanitize(state.gainsDb)
            val mapped = linkedMapOf<Short, MutableList<Pair<Int, Float>>>()

            EqualizerPresets.frequenciesHz.forEachIndexed { index, frequencyHz ->
                val band = effect.getBand(frequencyHz * 1_000)
                mapped.getOrPut(band) { mutableListOf() }
                    .add(frequencyHz to desired[index])
            }

            val mappingText = mapped.entries.joinToString(" · ") { entry ->
                val band = entry.key
                val requested = entry.value
                val averageDb = requested.map { it.second }.average().toFloat()
                val requestedMb = (averageDb * 100f).roundToInt()
                    .coerceIn(minMb, maxMb)
                    .toShort()

                effect.setBandLevel(band, requestedMb)

                val centerHz = effect.getCenterFreq(band) / 1_000
                val requestedLabels = requested.joinToString("/") { it.first.toString() + "Hz" }
                requestedLabels + "→" + centerHz + "Hz " + (requestedMb / 100f) + "dB"
            }

            val enableCode = effect.setEnabled(true)
            val attached = enableCode == AudioEffect.SUCCESS &&
                effect.enabled &&
                effect.hasControl()

            EqualizerApplyResult(
                status = if (attached) {
                    EqualizerStatus.ATTACHED
                } else {
                    EqualizerStatus.DEGRADED
                },
                implementation = "Session 0 EQ · " +
                    effect.descriptor.name +
                    " · " +
                    effect.numberOfBands +
                    " device bands · " +
                    (range[0] / 100f) +
                    "…" +
                    (range[1] / 100f) +
                    " dB",
                message = if (attached) {
                    "EQ attached · " + mappingText
                } else {
                    "EQ opened but LouderMe does not have full control · " + mappingText
                },
            )
        }.getOrElse { error ->
            release()
            EqualizerApplyResult(
                status = EqualizerStatus.UNSUPPORTED,
                implementation = "No usable session-0 Equalizer",
                message = error.message ?: error::class.java.simpleName,
            )
        }
    }

    fun disable() {
        runCatching { equalizer?.setEnabled(false) }
    }

    fun release() {
        runCatching { equalizer?.release() }
        equalizer = null
    }
}
