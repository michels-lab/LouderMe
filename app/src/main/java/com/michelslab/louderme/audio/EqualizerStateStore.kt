package com.michelslab.louderme.audio

import android.content.Context

object EqualizerStateStore {
    private const val PREFS = "louderme_equalizer_state"

    fun read(context: Context): EqualizerUiState {
        val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        val preset = runCatching {
            EqualizerPreset.valueOf(
                prefs.getString("preset", EqualizerPreset.FLAT.name)
                    ?: EqualizerPreset.FLAT.name
            )
        }.getOrDefault(EqualizerPreset.FLAT)

        val stored = prefs.getString("gains_db", null)
            ?.split(",")
            ?.mapNotNull { it.toFloatOrNull() }

        return EqualizerUiState(
            enabled = prefs.getBoolean("enabled", true),
            preset = preset,
            gainsDb = EqualizerPresets.sanitize(
                stored ?: EqualizerPresets.gainsFor(preset)
            ),
            status = EqualizerStatus.READY,
            implementation = "Waiting for audio engine",
            message = "EQ will apply when Global Boost is active.",
        )
    }

    fun write(context: Context, state: EqualizerUiState) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putBoolean("enabled", state.enabled)
            .putString("preset", state.preset.name)
            .putString(
                "gains_db",
                EqualizerPresets.sanitize(state.gainsDb).joinToString(",")
            )
            .apply()
    }
}
