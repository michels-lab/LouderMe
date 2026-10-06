package com.michelslab.louderme.startup

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.michelslab.louderme.audio.AudioBoostContract
import com.michelslab.louderme.audio.AudioBoostService
import com.michelslab.louderme.audio.AudioBoostStateStore

class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action != Intent.ACTION_BOOT_COMPLETED) return
        if (!StartupPreferenceStore.isStartWithPhoneEnabled(context)) return

        val lastState = AudioBoostStateStore.read(context)
        val serviceIntent = Intent(context, AudioBoostService::class.java)
            .setAction(AudioBoostContract.ACTION_ENABLE)
            .putExtra(AudioBoostContract.EXTRA_PERCENT, lastState.percent)

        runCatching {
            context.startForegroundService(serviceIntent)
        }.onFailure { error ->
            Log.w("LouderMeBoot", "Unable to restore LouderMe after boot.", error)
        }
    }
}
