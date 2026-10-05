package com.michelslab.louderme.audio

import android.media.audiofx.AudioEffect
import android.media.audiofx.DynamicsProcessing
import android.media.audiofx.LoudnessEnhancer

/**
 * Experimental compatibility engine.
 *
 * Android's public AudioEffect documentation explicitly says that attaching
 * insert effects to the global output mix through audio session 0 is deprecated.
 * Some vendor builds still support this path. LouderMe therefore treats a
 * successful attachment as "attached", not as proof that every external app is
 * being amplified. The target phone must be tested with real external playback.
 */
class SessionZeroAudioEngine : AudioEngine {
    private var loudnessEnhancer: LoudnessEnhancer? = null
    private var dynamicsProcessing: DynamicsProcessing? = null

    override fun enable(percent: Int): AudioEngineResult {
        val safePercent = percent.coerceIn(100, 250)
        val gainDb = BoostMath.percentToDb(safePercent)

        val loudnessResult = runCatching {
            val effect = loudnessEnhancer ?: LoudnessEnhancer(0).also {
                loudnessEnhancer = it
            }

            effect.setTargetGain(BoostMath.percentToMillibels(safePercent))
            val enableCode = effect.setEnabled(true)
            val hasControl = effect.hasControl()
            val enabled = effect.enabled
            val descriptor = effect.descriptor

            AudioEngineResult(
                status = if (
                    enableCode == AudioEffect.SUCCESS &&
                    enabled &&
                    hasControl
                ) {
                    AudioEngineStatus.ATTACHED
                } else {
                    AudioEngineStatus.DEGRADED
                },
                percent = safePercent,
                gainDb = gainDb,
                implementation = "Session 0 · ${descriptor.name} · ${descriptor.implementor}",
                message = if (enabled && hasControl) {
                    "Engine attached. This path was audibly validated on the target Samsung device."
                } else {
                    "Effect exists, but LouderMe does not have full control of the engine."
                },
            )
        }

        if (loudnessResult.isSuccess) {
            dynamicsProcessing?.release()
            dynamicsProcessing = null
            return loudnessResult.getOrThrow()
        }

        val dynamicsResult = runCatching {
            val effect = dynamicsProcessing ?: DynamicsProcessing(0).also {
                dynamicsProcessing = it
            }

            effect.setInputGainAllChannelsTo(gainDb.toFloat())
            val enableCode = effect.setEnabled(true)
            val hasControl = effect.hasControl()
            val enabled = effect.enabled
            val descriptor = effect.descriptor

            AudioEngineResult(
                status = if (
                    enableCode == AudioEffect.SUCCESS &&
                    enabled &&
                    hasControl
                ) {
                    AudioEngineStatus.ATTACHED
                } else {
                    AudioEngineStatus.DEGRADED
                },
                percent = safePercent,
                gainDb = gainDb,
                implementation = "Session 0 fallback · ${descriptor.name} · ${descriptor.implementor}",
                message = if (enabled && hasControl) {
                    "Dynamics engine attached. Compatibility can still vary by device."
                } else {
                    "Dynamics effect opened, but LouderMe does not have full control."
                },
            )
        }

        if (dynamicsResult.isSuccess) {
            loudnessEnhancer?.release()
            loudnessEnhancer = null
            return dynamicsResult.getOrThrow()
        }

        val advertised = runCatching {
            AudioEffect.queryEffects()
                .filter {
                    it.type == AudioEffect.EFFECT_TYPE_LOUDNESS_ENHANCER ||
                        it.type == AudioEffect.EFFECT_TYPE_DYNAMICS_PROCESSING
                }
                .joinToString { "${it.name} (${it.implementor})" }
        }.getOrDefault("none reported")

        release()

        return AudioEngineResult(
            status = AudioEngineStatus.UNSUPPORTED,
            percent = safePercent,
            gainDb = gainDb,
            implementation = "No usable session-0 engine",
            message = buildString {
                append("This device rejected the public session-0 compatibility path. ")
                append("Advertised effects: ")
                append(advertised.ifBlank { "none reported" })
                loudnessResult.exceptionOrNull()?.message?.let {
                    append(". Loudness: ")
                    append(it)
                }
                dynamicsResult.exceptionOrNull()?.message?.let {
                    append(". Dynamics: ")
                    append(it)
                }
            },
        )
    }

    override fun disable(): AudioEngineResult {
        runCatching { loudnessEnhancer?.setEnabled(false) }
        runCatching { dynamicsProcessing?.setEnabled(false) }
        release()

        return AudioEngineResult(
            status = AudioEngineStatus.OFF,
            percent = 150,
            gainDb = BoostMath.percentToDb(150),
            implementation = "Not attached",
            message = "Boost is off",
        )
    }

    override fun release() {
        runCatching { loudnessEnhancer?.release() }
        runCatching { dynamicsProcessing?.release() }
        loudnessEnhancer = null
        dynamicsProcessing = null
    }
}
