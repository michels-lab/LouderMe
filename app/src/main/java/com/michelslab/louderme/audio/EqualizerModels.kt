package com.michelslab.louderme.audio

enum class EqualizerStatus {
    READY,
    APPLYING,
    ATTACHED,
    DEGRADED,
    DISABLED,
    UNSUPPORTED,
    ERROR,
}

enum class EqualizerPreset(val displayName: String) {
    FLAT("Flat"),
    BASS("Bass"),
    DEEP_BASS("Deep Bass"),
    DIALOGUE("Dialogue"),
    TREBLE("Treble"),
    SPEAKER("Speaker"),
    HEADPHONES("Headphones"),
    CUSTOM("Custom"),
}

data class EqualizerUiState(
    val enabled: Boolean = true,
    val preset: EqualizerPreset = EqualizerPreset.FLAT,
    val gainsDb: List<Float> = EqualizerPresets.gainsFor(EqualizerPreset.FLAT),
    val status: EqualizerStatus = EqualizerStatus.READY,
    val implementation: String = "Waiting for audio engine",
    val message: String = "EQ will apply when Global Boost is active.",
)

object EqualizerPresets {
    val frequenciesHz = listOf(60, 150, 400, 1_000, 2_500, 6_000, 12_000)
    val labels = listOf("60", "150", "400", "1k", "2.5k", "6k", "12k")

    fun gainsFor(preset: EqualizerPreset): List<Float> =
        when (preset) {
            EqualizerPreset.FLAT -> listOf(0f, 0f, 0f, 0f, 0f, 0f, 0f)
            EqualizerPreset.BASS -> listOf(6f, 5f, 3f, 0f, -1f, -2f, -2f)
            EqualizerPreset.DEEP_BASS -> listOf(8f, 6f, 3f, 0f, -2f, -3f, -3f)
            EqualizerPreset.DIALOGUE -> listOf(-3f, -2f, 0f, 3f, 4f, 2f, 0f)
            EqualizerPreset.TREBLE -> listOf(-2f, -1f, 0f, 1f, 3f, 5f, 6f)
            EqualizerPreset.SPEAKER -> listOf(2f, 1f, 0f, 2f, 3f, 2f, 1f)
            EqualizerPreset.HEADPHONES -> listOf(1f, 1f, 0f, 0f, 1f, 2f, 2f)
            EqualizerPreset.CUSTOM -> listOf(0f, 0f, 0f, 0f, 0f, 0f, 0f)
        }

    fun sanitize(gainsDb: List<Float>): List<Float> =
        frequenciesHz.indices.map { index ->
            gainsDb.getOrElse(index) { 0f }.coerceIn(-10f, 10f)
        }
}
