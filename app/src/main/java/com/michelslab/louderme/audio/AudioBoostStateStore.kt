package com.michelslab.louderme.audio

import android.content.Context

object AudioBoostStateStore {
    private const val PREFS = "louderme_audio_state"

    fun read(context: Context): AudioEngineUiState {
        val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        val status = runCatching {
            AudioEngineStatus.valueOf(
                prefs.getString("status", AudioEngineStatus.OFF.name)
                    ?: AudioEngineStatus.OFF.name
            )
        }.getOrDefault(AudioEngineStatus.OFF)

        val percent = BoostLevelPolicy.clamp(prefs.getInt("percent", 150))

        return AudioEngineUiState(
            status = status,
            percent = percent,
            // Always derive the requested gain from the recovered level:
            // older versions could persist 300% then read back 250%, leaving
            // percent and gain dB inconsistent on the same screen.
            gainDb = BoostLevelPolicy.targetGainDb(percent),
            outputRoute = prefs.getString("route", "System output") ?: "System output",
            implementation = prefs.getString("implementation", "Not attached") ?: "Not attached",
            message = prefs.getString("message", "Boost is off") ?: "Boost is off",
        )
    }

    fun write(context: Context, state: AudioEngineUiState) {
        val safePercent = BoostLevelPolicy.clamp(state.percent)
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putString("status", state.status.name)
            .putInt("percent", safePercent)
            .putFloat("gain_db", BoostLevelPolicy.targetGainDb(safePercent).toFloat())
            .putString("route", state.outputRoute)
            .putString("implementation", state.implementation)
            .putString("message", state.message)
            .apply()
    }
}
