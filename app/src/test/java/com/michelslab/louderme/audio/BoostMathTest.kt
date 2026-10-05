package com.michelslab.louderme.audio

import org.junit.Assert.assertEquals
import org.junit.Test

class BoostMathTest {
    @Test
    fun percentToDb_usesAmplitudeRatio() {
        assertEquals(0.0, BoostMath.percentToDb(100), 0.001)
        assertEquals(1.938, BoostMath.percentToDb(125), 0.002)
        assertEquals(3.522, BoostMath.percentToDb(150), 0.002)
        assertEquals(4.861, BoostMath.percentToDb(175), 0.002)
        assertEquals(6.021, BoostMath.percentToDb(200), 0.002)
    }

    @Test
    fun percentToMillibels_matchesAndroidUnits() {
        assertEquals(0, BoostMath.percentToMillibels(100))
        assertEquals(352, BoostMath.percentToMillibels(150))
        assertEquals(602, BoostMath.percentToMillibels(200))
    }
}
