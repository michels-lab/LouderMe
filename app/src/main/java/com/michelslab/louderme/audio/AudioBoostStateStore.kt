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

        val percent = prefs.getInt("percent", 150).coerceIn(100, 200)

        return AudioEngineUiState(
            status = status,
            percent = percent,
            gainDb = prefs.getFloat(
                "gain_db",
                BoostMath.percentToDb(percent).toFloat()
            ).toDouble(),
            outputRoute = prefs.getString("route", "System output") ?: "System output",
            implementation = prefs.getString("implementation", "Not attached") ?: "Not attached",
            message = prefs.getString("message", "Boost is off") ?: "Boost is off",
        )
    }

    fun write(context: Context, state: AudioEngineUiState) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putString("status", state.status.name)
            .putInt("percent", state.percent)
            .putFloat("gain_db", state.gainDb.toFloat())
            .putString("route", state.outputRoute)
            .putString("implementation", state.implementation)
            .putString("message", state.message)
            .apply()
    }
}
