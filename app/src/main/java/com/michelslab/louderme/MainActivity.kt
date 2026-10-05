package com.michelslab.louderme

import android.Manifest
import android.app.Activity
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.mutableStateOf
import com.google.android.play.core.appupdate.AppUpdateManagerFactory
import com.michelslab.louderme.audio.AudioBoostContract
import com.michelslab.louderme.audio.AudioBoostService
import com.michelslab.louderme.audio.AudioBoostStateStore
import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.ui.LouderMeApp
import com.michelslab.louderme.update.PlayUpdateController
import com.michelslab.louderme.update.UpdateStatus

class MainActivity : ComponentActivity() {
    private val updateStatus = mutableStateOf<UpdateStatus>(UpdateStatus.Checking)
    private val audioState = mutableStateOf(AudioEngineUiState())

    private lateinit var updateController: PlayUpdateController
    private var pendingBoostPercent: Int? = null
    private var audioReceiverRegistered = false

    private val updateLauncher =
        registerForActivityResult(ActivityResultContracts.StartIntentSenderForResult()) { result ->
            if (result.resultCode != Activity.RESULT_OK && ::updateController.isInitialized) {
                updateController.markUpdateCancelled()
            }
        }

    private val notificationPermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) {
            pendingBoostPercent?.let { percent ->
                pendingBoostPercent = null
                startBoost(percent)
            }
        }

    private val audioStateReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context?, intent: Intent?) {
            if (intent?.action != AudioBoostContract.ACTION_STATE) return

            val status = runCatching {
                AudioEngineStatus.valueOf(
                    intent.getStringExtra(AudioBoostContract.EXTRA_STATUS)
                        ?: AudioEngineStatus.ERROR.name
                )
            }.getOrDefault(AudioEngineStatus.ERROR)

            audioState.value = AudioEngineUiState(
                status = status,
                percent = intent.getIntExtra(
                    AudioBoostContract.EXTRA_PERCENT,
                    audioState.value.percent,
                ),
                gainDb = intent.getDoubleExtra(
                    AudioBoostContract.EXTRA_GAIN_DB,
                    audioState.value.gainDb,
                ),
                outputRoute = intent.getStringExtra(
                    AudioBoostContract.EXTRA_ROUTE
                ) ?: "System output",
                implementation = intent.getStringExtra(
                    AudioBoostContract.EXTRA_IMPLEMENTATION
                ) ?: "Unknown",
                message = intent.getStringExtra(
                    AudioBoostContract.EXTRA_MESSAGE
                ) ?: "",
            )
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        audioState.value = AudioBoostStateStore.read(this)

        updateController = PlayUpdateController(
            appUpdateManager = AppUpdateManagerFactory.create(this),
            updateLauncher = updateLauncher,
            onStatusChanged = { updateStatus.value = it },
        )

        updateController.checkForUpdates(allowAutomaticPrompt = true)

        setContent {
            LouderMeApp(
                updateStatus = updateStatus.value,
                audioState = audioState.value,
                onCheckForUpdates = {
                    updateController.checkForUpdates(allowAutomaticPrompt = false)
                },
                onInstallUpdate = {
                    updateController.installAvailableUpdate()
                },
                onBoostToggle = { enabled ->
                    if (enabled) {
                        requestBoost(audioState.value.percent)
                    } else {
                        stopBoost()
                    }
                },
                onBoostLevelSelected = { percent ->
                    requestBoost(percent)
                },
            )
        }
    }

    override fun onStart() {
        super.onStart()

        if (!audioReceiverRegistered) {
            val filter = IntentFilter(AudioBoostContract.ACTION_STATE)

            if (Build.VERSION.SDK_INT >= 33) {
                registerReceiver(
                    audioStateReceiver,
                    filter,
                    Context.RECEIVER_NOT_EXPORTED,
                )
            } else {
                @Suppress("DEPRECATION")
                registerReceiver(audioStateReceiver, filter)
            }

            audioReceiverRegistered = true
        }

        audioState.value = AudioBoostStateStore.read(this)
    }

    override fun onStop() {
        if (audioReceiverRegistered) {
            unregisterReceiver(audioStateReceiver)
            audioReceiverRegistered = false
        }
        super.onStop()
    }

    override fun onResume() {
        super.onResume()
        if (::updateController.isInitialized) {
            updateController.resumeInterruptedUpdate()
        }
    }

    private fun requestBoost(percent: Int) {
        val safePercent = percent.coerceIn(100, 250)

        if (
            Build.VERSION.SDK_INT >= 33 &&
            checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) !=
            PackageManager.PERMISSION_GRANTED
        ) {
            pendingBoostPercent = safePercent
            notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
            return
        }

        startBoost(safePercent)
    }

    private fun startBoost(percent: Int) {
        val action = if (audioState.value.isRunning) {
            AudioBoostContract.ACTION_SET_LEVEL
        } else {
            AudioBoostContract.ACTION_ENABLE
        }

        val intent = Intent(this, AudioBoostService::class.java)
            .setAction(action)
            .putExtra(AudioBoostContract.EXTRA_PERCENT, percent)

        startForegroundService(intent)
    }

    private fun stopBoost() {
        startService(
            Intent(this, AudioBoostService::class.java)
                .setAction(AudioBoostContract.ACTION_DISABLE)
        )
    }
}
