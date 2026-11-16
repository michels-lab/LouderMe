package com.michelslab.louderme.audio

/**
 * One shared range for active Boost, persistent settings and automatic
 * restoration. A stale 250% bound in either restore path silently reduced
 * a user-selected 275–300% level after service or phone restart.
 *
 * This is a requested signal gain, not a guarantee of acoustic loudness.
 */
internal object BoostLevelPolicy {
    const val MIN_PERCENT = 100
    const val MAX_PERCENT = 300

    fun clamp(percent: Int): Int =
        percent.coerceIn(MIN_PERCENT, MAX_PERCENT)

    fun targetGainDb(percent: Int): Double =
        BoostMath.percentToDb(clamp(percent))
}
