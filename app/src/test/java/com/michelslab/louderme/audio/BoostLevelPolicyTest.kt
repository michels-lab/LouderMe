package com.michelslab.louderme.audio

import com.michelslab.louderme.startup.BootResumePolicy
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class BoostLevelPolicyTest {
    @Test
    fun allSelectableBoostLevelsSurvivePersistedRecoveryWithout250PercentCap() {
        for (level in listOf(100, 125, 150, 175, 200, 225, 250, 275, 300)) {
            assertEquals(level, BoostLevelPolicy.clamp(level))
            assertEquals(
                BoostMath.percentToDb(level),
                BoostLevelPolicy.targetGainDb(level),
                0.000001,
            )
        }
        assertEquals(100, BoostLevelPolicy.clamp(-500))
        assertEquals(100, BoostLevelPolicy.clamp(0))
        assertEquals(300, BoostLevelPolicy.clamp(301))
        assertEquals(300, BoostLevelPolicy.clamp(Int.MAX_VALUE))
        assertEquals(100, BoostLevelPolicy.clamp(Int.MIN_VALUE))
    }

    @Test
    fun serviceRestartAndOptedInBootPreserve275And300Percent() {
        for (level in listOf(275, 300)) {
            val previous = AudioEngineUiState(
                status = AudioEngineStatus.ATTACHED,
                percent = BoostLevelPolicy.clamp(level),
                gainDb = BoostLevelPolicy.targetGainDb(level),
            )
            assertEquals(
                level,
                BootResumePolicy.percentToRestore(enabled = true, saved = previous),
            )
            assertNull(BootResumePolicy.percentToRestore(enabled = false, saved = previous))
        }
        assertEquals(
            300,
            BootResumePolicy.percentToRestore(
                true, AudioEngineUiState(status = AudioEngineStatus.DEGRADED, percent = 999),
            ),
        )
        assertEquals(
            100,
            BootResumePolicy.percentToRestore(
                true, AudioEngineUiState(status = AudioEngineStatus.STARTING, percent = -1),
            ),
        )
    }
}
