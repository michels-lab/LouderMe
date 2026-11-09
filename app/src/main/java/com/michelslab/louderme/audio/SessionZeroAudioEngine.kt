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
                status = if (SessionZeroAttachmentPolicy.isAttached(
                    enableCode == AudioEffect.SUCCESS,
                    enabled,
                    hasControl,
                )) {
                    AudioEngineStatus.ATTACHED
                } else {
                    AudioEngineStatus.DEGRADED
                },
                percent = safePercent,
                gainDb = gainDb,
                implementation = "Session 0 · ${descriptor.name} · ${descriptor.implementor}",
                message = if (SessionZeroAttachmentPolicy.isAttached(
                    enableCode == AudioEffect.SUCCESS,
                    enabled,
                    hasControl,
                )) {
                    if (safePercent >= PeakProtectionPolicy.ENABLE_FROM_PERCENT) {
                        "Engine attached. Android LoudnessEnhancer compresses samples that would exceed the supported sample range; this path was audibly validated on the target Samsung device."
                    } else {
                        "Engine attached. This path was audibly validated on the target Samsung device."
                    }
                } else {
                    "Effect exists, but LouderMe does not have full control of the engine."
                },
            )
        }

        if (loudnessResult.getOrNull()?.status == AudioEngineStatus.ATTACHED) {
            runCatching { dynamicsProcessing?.release() }
            dynamicsProcessing = null
            return loudnessResult.getOrThrow()
        }

        // A created effect that reports no ownership is not a working Boost.
        // Release it before trying DynamicsProcessing to avoid stacking effects.
        runCatching { loudnessEnhancer?.setEnabled(false) }
        runCatching { loudnessEnhancer?.release() }
        loudnessEnhancer = null

        val dynamicsResult = runCatching {
            val effect = dynamicsProcessing ?: DynamicsProcessing(0).also {
                dynamicsProcessing = it
            }

            effect.setInputGainAllChannelsTo(gainDb.toFloat())

            val protection = PeakProtectionPolicy.forPercent(safePercent)
            val protectionNote = if (protection.enabled) {
                runCatching {
                    effect.setLimiterAllChannelsTo(
                        DynamicsProcessing.Limiter(
                            true,
                            true,
                            0,
                            protection.attackMs,
                            protection.releaseMs,
                            protection.ratio,
                            protection.thresholdDbfs,
                            0f,
                        )
                    )
                }.fold(
                    onSuccess = {
                        "Peak limiter ${protection.label.lowercase()} · ${protection.thresholdDbfs} dBFS · ${protection.ratio}:1"
                    },
                    onFailure = { error ->
                        "Peak limiter unavailable (${error.message ?: error::class.java.simpleName})"
                    }
                )
            } else {
                "Peak limiter off below ${PeakProtectionPolicy.ENABLE_FROM_PERCENT}%"
            }

            val enableCode = effect.setEnabled(true)
            val hasControl = effect.hasControl()
            val enabled = effect.enabled
            val descriptor = effect.descriptor

            AudioEngineResult(
                status = if (SessionZeroAttachmentPolicy.isAttached(
                    enableCode == AudioEffect.SUCCESS,
                    enabled,
                    hasControl,
                )) {
                    AudioEngineStatus.ATTACHED
                } else {
                    AudioEngineStatus.DEGRADED
                },
                percent = safePercent,
                gainDb = gainDb,
                implementation = "Session 0 fallback · ${descriptor.name} · ${descriptor.implementor}",
                message = if (SessionZeroAttachmentPolicy.isAttached(
                    enableCode == AudioEffect.SUCCESS,
                    enabled,
                    hasControl,
                )) {
                    "Dynamics engine attached. $protectionNote. Compatibility can still vary by device."
                } else {
                    "Dynamics effect opened, but LouderMe does not have full control. $protectionNote."
                },
            )
        }

        if (dynamicsResult.getOrNull()?.status == AudioEngineStatus.ATTACHED) {
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
                append("Neither session-0 effect confirmed an enabled, controllable Boost. ")
                append("This device rejected the public session-0 compatibility path. ")
                append("Advertised effects: ")
                append(advertised.ifBlank { "none reported" })
                loudnessResult.getOrNull()?.let {
                    append(". Loudness opened without full control: ")
                    append(it.message)
                }
                dynamicsResult.getOrNull()?.let {
                    append(". Dynamics opened without full control: ")
                    append(it.message)
                }
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

    /**
     * Check that this service still owns an enabled session-0 effect after
     * Android changes the playback route. This does not certify audible gain.
     * If an implementation throws during device teardown, treat it as lost.
     */
    fun hasActiveEffect(): Boolean =
        runCatching {
            loudnessEnhancer?.let { it.enabled && it.hasControl() } == true ||
                dynamicsProcessing?.let { it.enabled && it.hasControl() } == true
        }.getOrDefault(false)

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
