package com.michelslab.louderme.startup

import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.BoostLevelPolicy

/** Restore only a user-opted-in, previously running Boost session. */
internal object BootResumePolicy {
    fun percentToRestore(enabled: Boolean, saved: AudioEngineUiState): Int? =
        if (enabled && saved.isRunning) BoostLevelPolicy.clamp(saved.percent) else null
}
