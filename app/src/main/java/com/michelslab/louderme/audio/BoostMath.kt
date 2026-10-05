package com.michelslab.louderme.audio

import kotlin.math.log10
import kotlin.math.roundToInt

object BoostMath {
    fun percentToDb(percent: Int): Double {
        val safePercent = percent.coerceAtLeast(1)
        return 20.0 * log10(safePercent / 100.0)
    }

    fun percentToMillibels(percent: Int): Int =
        (percentToDb(percent) * 100.0).roundToInt()
}
