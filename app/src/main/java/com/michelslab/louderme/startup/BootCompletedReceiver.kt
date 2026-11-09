package com.michelslab.louderme.startup

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import com.michelslab.louderme.audio.AudioBoostContract
import com.michelslab.louderme.audio.AudioBoostService
import com.michelslab.louderme.audio.AudioBoostStateStore
import com.michelslab.louderme.audio.AudioEngineStatus

class BootCompletedReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent?) {
        if (intent?.action != Intent.ACTION_BOOT_COMPLETED) return

        val savedState = AudioBoostStateStore.read(context)
        val percent = BootResumePolicy.percentToRestore(
            StartupPreferences.isStartOnBootEnabled(context),
            savedState,
        ) ?: return

        // POST_NOTIFICATIONS governs visibility, not the ability to start an
        // Android 13+ foreground service. Keep the user-enabled boot setting
        // even if they chose to hide notifications.
        val serviceIntent = Intent(context, AudioBoostService::class.java)
            .setAction(AudioBoostContract.ACTION_ENABLE)
            .putExtra(AudioBoostContract.EXTRA_PERCENT, percent)

        runCatching {
            context.startForegroundService(serviceIntent)
        }.onFailure {
            // Background-start restrictions differ by Android version/OEM.
            // Do not crash the boot broadcast or leave ATTACHED persisted.
            AudioBoostStateStore.write(
                context,
                savedState.copy(
                    status = AudioEngineStatus.ERROR,
                    implementation = "Android boot restart blocked",
                    message = "Android blocked automatic Boost restart. Open LouderMe to enable it manually.",
                ),
            )
        }
    }
}
