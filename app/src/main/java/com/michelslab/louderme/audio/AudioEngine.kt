package com.michelslab.louderme.audio

data class AudioEngineResult(
    val status: AudioEngineStatus,
    val percent: Int,
    val gainDb: Double,
    val implementation: String,
    val message: String,
)

interface AudioEngine {
    fun enable(percent: Int): AudioEngineResult
    fun disable(): AudioEngineResult
    fun release()
}
