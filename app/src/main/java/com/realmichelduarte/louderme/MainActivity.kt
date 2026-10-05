package com.realmichelduarte.louderme

import android.app.Activity
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.mutableStateOf
import com.google.android.play.core.appupdate.AppUpdateManagerFactory
import com.realmichelduarte.louderme.ui.LouderMeApp
import com.realmichelduarte.louderme.update.PlayUpdateController
import com.realmichelduarte.louderme.update.UpdateStatus

class MainActivity : ComponentActivity() {
    private val updateStatus = mutableStateOf<UpdateStatus>(UpdateStatus.Checking)
    private lateinit var updateController: PlayUpdateController

    private val updateLauncher =
        registerForActivityResult(ActivityResultContracts.StartIntentSenderForResult()) { result ->
            if (result.resultCode != Activity.RESULT_OK && ::updateController.isInitialized) {
                updateController.markUpdateCancelled()
            }
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        updateController = PlayUpdateController(
            activity = this,
            appUpdateManager = AppUpdateManagerFactory.create(this),
            updateLauncher = updateLauncher,
            onStatusChanged = { updateStatus.value = it },
        )

        updateController.checkForUpdates(allowAutomaticPrompt = true)

        setContent {
            LouderMeApp(
                updateStatus = updateStatus.value,
                onCheckForUpdates = {
                    updateController.checkForUpdates(allowAutomaticPrompt = false)
                },
                onInstallUpdate = {
                    updateController.installAvailableUpdate()
                },
            )
        }
    }

    override fun onResume() {
        super.onResume()
        if (::updateController.isInitialized) {
            updateController.resumeInterruptedUpdate()
        }
    }
}
