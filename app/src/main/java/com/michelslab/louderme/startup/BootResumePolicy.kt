package com.michelslab.louderme.startup

import com.michelslab.louderme.audio.AudioEngineUiState

/** Restore only a user-opted-in, previously running Boost session. */
internal object BootResumePolicy {
    fun percentToRestore(enabled: Boolean, saved: AudioEngineUiState): Int? =
        if (enabled && saved.isRunning) saved.percent.coerceIn(100, 250) else null
}
