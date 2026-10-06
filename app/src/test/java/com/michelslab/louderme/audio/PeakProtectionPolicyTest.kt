package com.michelslab.louderme.audio

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class PeakProtectionPolicyTest {
    @Test
    fun protection_staysOffBelowThreshold() {
        assertFalse(PeakProtectionPolicy.forPercent(100).enabled)
        assertFalse(PeakProtectionPolicy.forPercent(174).enabled)
    }

    @Test
    fun protection_strengthIncreasesWithHighGain() {
        val light = PeakProtectionPolicy.forPercent(175)
        val medium = PeakProtectionPolicy.forPercent(200)
        val high = PeakProtectionPolicy.forPercent(225)
        val max = PeakProtectionPolicy.forPercent(250)

        assertTrue(light.enabled)
        assertTrue(medium.enabled)
        assertTrue(high.enabled)
        assertTrue(max.enabled)

        assertEquals("LIGHT", light.label)
        assertEquals("MEDIUM", medium.label)
        assertEquals("HIGH", high.label)
        assertEquals("MAX", max.label)

        assertTrue(light.ratio < medium.ratio)
        assertTrue(medium.ratio < high.ratio)
        assertTrue(high.ratio < max.ratio)

        assertTrue(light.thresholdDbfs > medium.thresholdDbfs)
        assertTrue(medium.thresholdDbfs > high.thresholdDbfs)
        assertTrue(high.thresholdDbfs > max.thresholdDbfs)
    }

    @Test
    fun policy_clampsInputsToSupportedBoostRange() {
        assertEquals(
            PeakProtectionPolicy.forPercent(100),
            PeakProtectionPolicy.forPercent(-50),
        )
        assertEquals(
            PeakProtectionPolicy.forPercent(250),
            PeakProtectionPolicy.forPercent(999),
        )
    }
}
