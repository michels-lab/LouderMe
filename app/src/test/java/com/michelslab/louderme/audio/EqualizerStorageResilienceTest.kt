package com.michelslab.louderme.audio

import org.junit.Assert.assertEquals
import org.junit.Test

class EqualizerStorageResilienceTest {
    @Test
    fun malformedTokensDoNotShiftFrequencyBands() {
        assertEquals(
            listOf(6f, 0f, 3f, 0f, 0f, -10f, 10f),
            EqualizerPresets.fromStoredString(
                "6,not-a-number,3,NaN,Infinity,-20,12",
                EqualizerPreset.CUSTOM,
            ),
        )
        assertEquals(
            listOf(2f, 0f, 4f, 0f, 0f, 0f, 0f),
            EqualizerPresets.fromStoredString("2,,4", EqualizerPreset.BASS),
        )
    }

    @Test
    fun uninitializedStoreKeepsSelectedPresetAndSafeLevels() {
        assertEquals(
            EqualizerPresets.gainsFor(EqualizerPreset.DIALOGUE),
            EqualizerPresets.fromStoredString(null, EqualizerPreset.DIALOGUE),
        )
        assertEquals(
            listOf(0f, 0f, 0f, 0f, 0f, 0f, 0f),
            EqualizerPresets.sanitize(
                listOf(Float.NaN, Float.POSITIVE_INFINITY, Float.NEGATIVE_INFINITY),
            ),
        )
    }
}
