package com.michelslab.louderme.ui

import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.EqualizerStatus
import com.michelslab.louderme.audio.EqualizerUiState
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertThrows
import org.junit.Assert.assertTrue
import org.junit.Test

class DeveloperRuntimeLedgerTest {
    @Test fun capturesOnlyRealAllowedEngineAndEqualizerStates() {
        val healthy = DeveloperRuntimeLedger.detect(AudioEngineUiState(), EqualizerUiState())
        assertFalse(healthy.contains("AUDIO_ENGINE_ERROR_OBSERVED"))
        assertFalse(healthy.contains("EQUALIZER_ENGINE_ERROR_OBSERVED"))
        val badAudio = AudioEngineUiState(status = AudioEngineStatus.ERROR, percent = 99)
        val badEq = EqualizerUiState(status = EqualizerStatus.ERROR,
            gainsDb = listOf(Float.NaN))
        val codes = DeveloperRuntimeLedger.detect(badAudio, badEq)
        assertTrue(codes.contains("AUDIO_ENGINE_ERROR_OBSERVED"))
        assertTrue(codes.contains("EQUALIZER_ENGINE_ERROR_OBSERVED"))
        assertTrue(codes.contains("BOOST_STATE_OUT_OF_RANGE"))
        assertTrue(codes.contains("EQUALIZER_BAND_COUNT_INVALID"))
        assertTrue(codes.contains("EQUALIZER_NONFINITE_GAIN"))
    }

    @Test fun firstAndLastObservationAndOccurrenceCountSurviveJsonRoundTrip() {
        val a = DeveloperRuntimeLedger.append(emptyList(), "AUDIO_ENGINE_ERROR_OBSERVED", 1000L)
        val b = DeveloperRuntimeLedger.append(a, "AUDIO_ENGINE_ERROR_OBSERVED", 2000L)
        assertEquals(1, b.size)
        assertEquals(2, b[0].occurrences)
        assertEquals(1000L, b[0].firstUtcMs)
        assertEquals(2000L, b[0].lastUtcMs)
        assertEquals(b, DeveloperRuntimeLedger.decode(DeveloperRuntimeLedger.encode(b)))
    }

    @Test fun neverAcceptsFreeTextMessagesOrUnboundedHistory() {
        val clean = DeveloperRuntimeLedger.append(emptyList(), "EQUALIZER_ENGINE_ERROR_OBSERVED", 100L)
        val valid = DeveloperRuntimeLedger.encode(clean)
        listOf(
            valid.replace("EQUALIZER_ENGINE_ERROR_OBSERVED", "path=/home/user/secret"),
            valid.replace("\"occurrences\":1", "\"occurrences\":0"),
            valid.replace("EQUALIZER_ENGINE_ERROR_OBSERVED", "EQUALIZER_MESSAGE=raw"),
            "{",
            " ".repeat(16_385),
        ).forEach { input ->
            assertThrows(Exception::class.java) { DeveloperRuntimeLedger.decode(input) }
        }
        assertThrows(IllegalArgumentException::class.java) {
            DeveloperRuntimeLedger.append(clean, "user-email@example.com", 300L)
        }
        assertThrows(IllegalArgumentException::class.java) {
            DeveloperRuntimeLedger.append(clean, "AUDIO_ENGINE_ERROR_OBSERVED", -1L)
        }
    }
}
