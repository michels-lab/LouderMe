package com.michelslab.louderme.startup

import android.content.Context

object StartupPreferences {
    private const val STORE = "louderme_startup"
    private const val KEY_START_ON_BOOT = "start_on_boot"

    fun isStartOnBootEnabled(context: Context): Boolean =
        context.getSharedPreferences(STORE, Context.MODE_PRIVATE)
            .getBoolean(KEY_START_ON_BOOT, false)

    fun setStartOnBootEnabled(context: Context, enabled: Boolean) {
        context.getSharedPreferences(STORE, Context.MODE_PRIVATE)
            .edit()
            .putBoolean(KEY_START_ON_BOOT, enabled)
            .apply()
    }
}
