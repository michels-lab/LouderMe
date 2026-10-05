package com.realmichelduarte.louderme.update

import android.app.Activity
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.IntentSenderRequest
import com.google.android.play.core.appupdate.AppUpdateInfo
import com.google.android.play.core.appupdate.AppUpdateManager
import com.google.android.play.core.appupdate.AppUpdateOptions
import com.google.android.play.core.install.model.AppUpdateType
import com.google.android.play.core.install.model.UpdateAvailability

class PlayUpdateController(
    private val activity: Activity,
    private val appUpdateManager: AppUpdateManager,
    private val updateLauncher: ActivityResultLauncher<IntentSenderRequest>,
    private val onStatusChanged: (UpdateStatus) -> Unit,
) {
    private var pendingUpdateInfo: AppUpdateInfo? = null
    private var autoPromptedThisProcess = false

    fun checkForUpdates(allowAutomaticPrompt: Boolean) {
        onStatusChanged(UpdateStatus.Checking)

        appUpdateManager.appUpdateInfo
            .addOnSuccessListener { info ->
                when {
                    info.updateAvailability() ==
                        UpdateAvailability.DEVELOPER_TRIGGERED_UPDATE_IN_PROGRESS -> {
                        pendingUpdateInfo = info
                        onStatusChanged(UpdateStatus.UpdateAvailable)
                        startImmediateUpdate(info)
                    }

                    info.updateAvailability() == UpdateAvailability.UPDATE_AVAILABLE &&
                        info.isUpdateTypeAllowed(AppUpdateType.IMMEDIATE) -> {
                        pendingUpdateInfo = info
                        onStatusChanged(UpdateStatus.UpdateAvailable)

                        if (allowAutomaticPrompt && !autoPromptedThisProcess) {
                            autoPromptedThisProcess = true
                            startImmediateUpdate(info)
                        }
                    }

                    else -> {
                        pendingUpdateInfo = null
                        onStatusChanged(UpdateStatus.UpToDate)
                    }
                }
            }
            .addOnFailureListener { error ->
                pendingUpdateInfo = null

                // A sideloaded/debug build is not owned by Google Play, so the
                // Play update API cannot service it. This is expected and is
                // distinct from a real app failure.
                val message = error.message.orEmpty()
                if (
                    message.contains("not owned", ignoreCase = true) ||
                    message.contains("install", ignoreCase = true)
                ) {
                    onStatusChanged(UpdateStatus.PlayStoreUnavailable)
                } else {
                    onStatusChanged(UpdateStatus.Error(error.message))
                }
            }
    }

    fun installAvailableUpdate() {
        val info = pendingUpdateInfo
        if (info == null) {
            checkForUpdates(allowAutomaticPrompt = true)
            return
        }

        startImmediateUpdate(info)
    }

    fun resumeInterruptedUpdate() {
        appUpdateManager.appUpdateInfo
            .addOnSuccessListener { info ->
                if (
                    info.updateAvailability() ==
                    UpdateAvailability.DEVELOPER_TRIGGERED_UPDATE_IN_PROGRESS
                ) {
                    pendingUpdateInfo = info
                    onStatusChanged(UpdateStatus.UpdateAvailable)
                    startImmediateUpdate(info)
                } else {
                    checkForUpdates(allowAutomaticPrompt = !autoPromptedThisProcess)
                }
            }
            .addOnFailureListener {
                onStatusChanged(UpdateStatus.PlayStoreUnavailable)
            }
    }

    private fun startImmediateUpdate(info: AppUpdateInfo) {
        runCatching {
            appUpdateManager.startUpdateFlowForResult(
                info,
                updateLauncher,
                AppUpdateOptions.newBuilder(AppUpdateType.IMMEDIATE).build(),
            )
        }.onFailure { error ->
            onStatusChanged(UpdateStatus.Error(error.message))
        }
    }

    fun markUpdateCancelled() {
        onStatusChanged(UpdateStatus.UpdateCancelled)
    }
}
