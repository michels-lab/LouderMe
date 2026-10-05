package com.michelslab.louderme.audio

import org.junit.Assert.assertEquals
import org.junit.Test

class EqualizerPresetsTest {
    @Test
    fun everyPreset_hasSevenBands() {
        EqualizerPreset.entries
            .filter { it != EqualizerPreset.CUSTOM }
            .forEach { preset ->
                assertEquals(7, EqualizerPresets.gainsFor(preset).size)
            }
    }

    @Test
    fun sanitize_clampsAndPadsToUiRange() {
        val result = EqualizerPresets.sanitize(listOf(20f, -20f, 1f))

        assertEquals(7, result.size)
        assertEquals(10f, result[0], 0.001f)
        assertEquals(-10f, result[1], 0.001f)
        assertEquals(1f, result[2], 0.001f)
        assertEquals(0f, result[6], 0.001f)
    }

    @Test
    fun targetFrequencies_areStable() {
        assertEquals(
            listOf(60, 150, 400, 1_000, 2_500, 6_000, 12_000),
            EqualizerPresets.frequenciesHz,
        )
    }
}
