package com.michelslab.louderme.audio

enum class AudioEngineStatus {
    OFF,
    STARTING,
    ATTACHED,
    DEGRADED,
    UNSUPPORTED,
    ERROR,
}

data class AudioEngineUiState(
    val status: AudioEngineStatus = AudioEngineStatus.OFF,
    val percent: Int = 150,
    val gainDb: Double = BoostMath.percentToDb(150),
    val outputRoute: String = "System output",
    val implementation: String = "Not attached",
    val message: String = "Boost is off",
) {
    val isRunning: Boolean
        get() = status == AudioEngineStatus.STARTING ||
            status == AudioEngineStatus.ATTACHED ||
            status == AudioEngineStatus.DEGRADED
}
