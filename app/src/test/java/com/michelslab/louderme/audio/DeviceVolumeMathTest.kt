package com.michelslab.louderme.audio

import org.junit.Assert.assertEquals
import org.junit.Test

class DeviceVolumeMathTest {
    @Test
    fun stepToPercent_mapsEndpointsAndMiddle() {
        assertEquals(0, DeviceVolumeMath.stepToPercent(0, 15))
        assertEquals(100, DeviceVolumeMath.stepToPercent(15, 15))
        assertEquals(53, DeviceVolumeMath.stepToPercent(8, 15))
    }

    @Test
    fun percentToStep_mapsAndClamps() {
        assertEquals(0, DeviceVolumeMath.percentToStep(-20, 15))
        assertEquals(8, DeviceVolumeMath.percentToStep(50, 15))
        assertEquals(15, DeviceVolumeMath.percentToStep(140, 15))
    }

    @Test
    fun zeroMaxVolume_isSafe() {
        assertEquals(0, DeviceVolumeMath.stepToPercent(10, 0))
        assertEquals(0, DeviceVolumeMath.percentToStep(80, 0))
    }
}
