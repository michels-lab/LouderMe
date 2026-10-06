package com.michelslab.louderme.startup

import android.Manifest
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import com.michelslab.louderme.audio.AudioBoostContract
import com.michelslab.louderme.audio.AudioBoostService
import com.michelslab.louderme.audio.AudioBoostStateStore

class BootCompletedReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent?) {
        if (intent?.action != Intent.ACTION_BOOT_COMPLETED) return
        if (!StartupPreferences.isStartOnBootEnabled(context)) return

        val savedState = AudioBoostStateStore.read(context)
        if (!savedState.isRunning) return

        if (
            Build.VERSION.SDK_INT >= 33 &&
            context.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) !=
            PackageManager.PERMISSION_GRANTED
        ) {
            return
        }

        val serviceIntent = Intent(context, AudioBoostService::class.java)
            .setAction(AudioBoostContract.ACTION_ENABLE)
            .putExtra(
                AudioBoostContract.EXTRA_PERCENT,
                savedState.percent.coerceIn(100, 250),
            )

        context.startForegroundService(serviceIntent)
    }
}
