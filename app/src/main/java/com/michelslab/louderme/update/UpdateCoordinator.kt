package com.michelslab.louderme.update

import android.app.Activity
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.IntentSenderRequest
import com.google.android.play.core.appupdate.AppUpdateManagerFactory
import com.michelslab.louderme.BuildConfig

class UpdateCoordinator(
    activity: Activity,
    updateLauncher: ActivityResultLauncher<IntentSenderRequest>,
    onStatusChanged: (UpdateStatus) -> Unit,
) {
    private val isPlayChannel =
        BuildConfig.UPDATE_CHANNEL == "play"

    private val playController = PlayUpdateController(
        appUpdateManager = AppUpdateManagerFactory.create(activity),
        updateLauncher = updateLauncher,
        onStatusChanged = onStatusChanged,
    )

    private val directController = DirectUpdateController(
        activity = activity,
        onStatusChanged = onStatusChanged,
    )

    fun checkForUpdates(automatic: Boolean) {
        if (isPlayChannel) {
            playController.checkForUpdates(
                allowAutomaticPrompt = automatic
            )
        } else {
            directController.checkForUpdates(
                allowAutomaticPrompt = automatic
            )
        }
    }

    fun installAvailableUpdate() {
        if (isPlayChannel) {
            playController.installAvailableUpdate()
        } else {
            directController.installAvailableUpdate()
        }
    }

    fun onResume() {
        if (isPlayChannel) {
            playController.resumeInterruptedUpdate()
        } else {
            directController.resumeInterruptedUpdate()
        }
    }

    fun markPlayUpdateCancelled() {
        if (isPlayChannel) {
            playController.markUpdateCancelled()
        }
    }

    fun shutdown() {
        directController.shutdown()
    }

    fun channelLabel(): String =
        if (isPlayChannel) {
            "Google Play"
        } else {
            "Michel's Lab Direct"
        }
}
