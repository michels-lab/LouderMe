package com.michelslab.louderme.audio

import org.junit.Assert.assertEquals
import org.junit.Test

class BoostQuickLevelsTest {
    @Test
    fun notificationStepsChooseNextActualPreset() {
        assertEquals(150, BoostQuickLevels.next(125))
        assertEquals(150, BoostQuickLevels.next(137))
        assertEquals(125, BoostQuickLevels.previous(137))
        assertEquals(125, BoostQuickLevels.previous(150))
    }

    @Test
    fun notificationStepsClampAtSafeBoundaries() {
        assertEquals(100, BoostQuickLevels.previous(100))
        assertEquals(250, BoostQuickLevels.next(250))
        assertEquals(100, BoostQuickLevels.previous(-999))
        assertEquals(250, BoostQuickLevels.next(999))
    }

    @Test
    fun supportedPresetsRemainReachableInBothDirections() {
        val levels = listOf(100, 125, 150, 175, 200, 225, 250)
        var selected = 100
        for (i in 1 until levels.size) {
            selected = BoostQuickLevels.next(selected)
            assertEquals(levels[i], selected)
        }
        for (i in levels.size - 2 downTo 0) {
            selected = BoostQuickLevels.previous(selected)
            assertEquals(levels[i], selected)
        }
    }
}
