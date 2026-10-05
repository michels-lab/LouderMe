package com.michelslab.louderme.update

import android.app.Activity
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.IntentSenderRequest
import com.google.android.play.core.appupdate.AppUpdateManagerFactory

class UpdateCoordinator(
    private val activity: Activity,
    updateLauncher: ActivityResultLauncher<IntentSenderRequest>,
    private val onStatusChanged: (UpdateStatus) -> Unit,
) {
    private val playInstall =
        UpdateChannelDetector.isPlayInstall(activity)

    private val playController = PlayUpdateController(
        appUpdateManager = AppUpdateManagerFactory.create(activity),
        updateLauncher = updateLauncher,
        onStatusChanged = onStatusChanged,
    )

    private val sideloadController = SideloadUpdateController(
        activity = activity,
        onStatusChanged = onStatusChanged,
    )

    fun checkForUpdates(automatic: Boolean) {
        if (playInstall) {
            playController.checkForUpdates(
                allowAutomaticPrompt = automatic
            )
        } else {
            sideloadController.checkForUpdates(
                automatic = automatic
            )
        }
    }

    fun installAvailableUpdate() {
        if (playInstall) {
            playController.installAvailableUpdate()
        } else {
            sideloadController.installAvailableUpdate()
        }
    }

    fun onResume() {
        if (playInstall) {
            playController.resumeInterruptedUpdate()
        } else {
            sideloadController.resumePendingInstall()
            sideloadController.checkForUpdates(
                automatic = true
            )
        }
    }

    fun markPlayUpdateCancelled() {
        if (playInstall) {
            playController.markUpdateCancelled()
        }
    }

    fun shutdown() {
        sideloadController.shutdown()
    }

    fun channelLabel(): String =
        if (playInstall) {
            "Google Play"
        } else {
            "Michel's Lab Direct"
        }
}
