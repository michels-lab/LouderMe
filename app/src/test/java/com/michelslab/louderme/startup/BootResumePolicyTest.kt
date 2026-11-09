package com.michelslab.louderme.startup

import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class BootResumePolicyTest {
    @Test
    fun bootRestoresOnlyOptedInRunningBoost() {
        assertEquals(
            200,
            BootResumePolicy.percentToRestore(
                true,
                AudioEngineUiState(status = AudioEngineStatus.ATTACHED, percent = 200),
            ),
        )
        assertNull(
            BootResumePolicy.percentToRestore(
                false,
                AudioEngineUiState(status = AudioEngineStatus.ATTACHED, percent = 200),
            ),
        )
    }

    @Test
    fun stoppedOrErroredBoostNeverRestartsAtBoot() {
        for (status in listOf(
            AudioEngineStatus.OFF,
            AudioEngineStatus.ERROR,
            AudioEngineStatus.UNSUPPORTED,
        )) {
            assertNull(
                BootResumePolicy.percentToRestore(
                    true,
                    AudioEngineUiState(status = status, percent = 175),
                ),
            )
        }
    }

    @Test
    fun restoredPercentIsAlwaysBounded() {
        assertEquals(
            100,
            BootResumePolicy.percentToRestore(
                true,
                AudioEngineUiState(status = AudioEngineStatus.STARTING, percent = -20),
            ),
        )
        assertEquals(
            250,
            BootResumePolicy.percentToRestore(
                true,
                AudioEngineUiState(status = AudioEngineStatus.DEGRADED, percent = 999),
            ),
        )
    }
}
