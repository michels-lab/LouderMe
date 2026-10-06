package com.michelslab.louderme.startup

import android.content.Context

object StartupPreferenceStore {
    private const val PREFS = "louderme_startup_preferences"
    private const val KEY_START_WITH_PHONE = "start_with_phone"

    fun isStartWithPhoneEnabled(context: Context): Boolean =
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .getBoolean(KEY_START_WITH_PHONE, false)

    fun setStartWithPhoneEnabled(context: Context, enabled: Boolean) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putBoolean(KEY_START_WITH_PHONE, enabled)
            .apply()
    }
}
