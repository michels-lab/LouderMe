package com.michelslab.louderme.update

import android.app.Activity
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.IntentSenderRequest
import com.google.android.play.core.appupdate.AppUpdateManagerFactory
import com.michelslab.louderme.BuildConfig

class UpdateCoordinator(
    activity: Activity,
    updateLauncher:
        ActivityResultLauncher<IntentSenderRequest>,
    onStatusChanged: (UpdateStatus) -> Unit,
) {
    private val channel =
        if (
            BuildConfig.UPDATE_CHANNEL ==
            "play"
        ) {
            UpdateChannel.PLAY
        } else {
            UpdateChannel.DIRECT
        }

    private val playController =
        PlayUpdateController(
            appUpdateManager =
                AppUpdateManagerFactory
                    .create(activity),
            updateLauncher = updateLauncher,
            onStatusChanged =
                onStatusChanged,
        )

    private val directController =
        DirectUpdateController(
            activity = activity,
            onStatusChanged =
                onStatusChanged,
        )

    fun checkForUpdates(
        allowAutomaticPrompt: Boolean,
    ) {
        when (channel) {
            UpdateChannel.PLAY ->
                playController
                    .checkForUpdates(
                        allowAutomaticPrompt
                    )

            UpdateChannel.DIRECT ->
                directController
                    .checkForUpdates(
                        allowAutomaticPrompt
                    )
        }
    }

    fun installAvailableUpdate() {
        when (channel) {
            UpdateChannel.PLAY ->
                playController
                    .installAvailableUpdate()

            UpdateChannel.DIRECT ->
                directController
                    .installAvailableUpdate()
        }
    }

    fun resumeInterruptedUpdate() {
        when (channel) {
            UpdateChannel.PLAY ->
                playController
                    .resumeInterruptedUpdate()

            UpdateChannel.DIRECT ->
                directController
                    .resumeInterruptedUpdate()
        }
    }

    fun markUpdateCancelled() {
        if (
            channel ==
            UpdateChannel.PLAY
        ) {
            playController
                .markUpdateCancelled()
        }
    }
}
