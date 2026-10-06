package com.michelslab.louderme.audio

data class PeakProtectionProfile(
    val enabled: Boolean,
    val label: String,
    val attackMs: Float,
    val releaseMs: Float,
    val ratio: Float,
    val thresholdDbfs: Float,
)

object PeakProtectionPolicy {
    const val ENABLE_FROM_PERCENT = 175

    fun forPercent(percent: Int): PeakProtectionProfile {
        val safePercent = percent.coerceIn(100, 250)

        return when {
            safePercent >= 250 -> PeakProtectionProfile(
                enabled = true,
                label = "MAX",
                attackMs = 1f,
                releaseMs = 160f,
                ratio = 20f,
                thresholdDbfs = -2.0f,
            )

            safePercent >= 225 -> PeakProtectionProfile(
                enabled = true,
                label = "HIGH",
                attackMs = 2f,
                releaseMs = 140f,
                ratio = 14f,
                thresholdDbfs = -1.5f,
            )

            safePercent >= 200 -> PeakProtectionProfile(
                enabled = true,
                label = "MEDIUM",
                attackMs = 3f,
                releaseMs = 120f,
                ratio = 10f,
                thresholdDbfs = -1.0f,
            )

            safePercent >= ENABLE_FROM_PERCENT -> PeakProtectionProfile(
                enabled = true,
                label = "LIGHT",
                attackMs = 4f,
                releaseMs = 100f,
                ratio = 6f,
                thresholdDbfs = -0.5f,
            )

            else -> PeakProtectionProfile(
                enabled = false,
                label = "OFF",
                attackMs = 4f,
                releaseMs = 100f,
                ratio = 1f,
                thresholdDbfs = 0f,
            )
        }
    }
}
