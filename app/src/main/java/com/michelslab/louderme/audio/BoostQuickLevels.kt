package com.michelslab.louderme.audio

/** Preset steps exposed by the foreground-service notification. */
internal object BoostQuickLevels {
    private val levels = listOf(100, 125, 150, 175, 200, 225, 250)

    fun next(currentPercent: Int): Int {
        val current = currentPercent.coerceIn(100, 250)
        return levels.firstOrNull { it > current } ?: 250
    }

    fun previous(currentPercent: Int): Int {
        val current = currentPercent.coerceIn(100, 250)
        return levels.lastOrNull { it < current } ?: 100
    }
}
