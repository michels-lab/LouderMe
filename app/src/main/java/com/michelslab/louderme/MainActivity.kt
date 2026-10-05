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
import com.michelslab.louderme.audio.AudioBoostContract
import com.michelslab.louderme.audio.AudioBoostService
import com.michelslab.louderme.audio.AudioBoostStateStore
import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.EqualizerPreset
import com.michelslab.louderme.audio.EqualizerPresets
import com.michelslab.louderme.audio.EqualizerStateStore
import com.michelslab.louderme.audio.EqualizerStatus
import com.michelslab.louderme.audio.EqualizerUiState
import com.michelslab.louderme.ui.LouderMeApp
import com.michelslab.louderme.update.UpdateCoordinator
import com.michelslab.louderme.update.UpdateStatus

class MainActivity : ComponentActivity() {
    private val updateStatus = mutableStateOf<UpdateStatus>(UpdateStatus.Checking)
    private val audioState = mutableStateOf(AudioEngineUiState())
    private val equalizerState = mutableStateOf(EqualizerUiState())

    private lateinit var updateController: UpdateCoordinator
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
            when (intent?.action) {
                AudioBoostContract.ACTION_STATE -> receiveAudioState(intent)
                AudioBoostContract.ACTION_EQ_STATE -> receiveEqualizerState(intent)
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        audioState.value = AudioBoostStateStore.read(this)
        equalizerState.value = EqualizerStateStore.read(this)

        updateController = UpdateCoordinator(
            activity = this,
            updateLauncher = updateLauncher,
            onStatusChanged = { updateStatus.value = it },
        )

        updateController.checkForUpdates(allowAutomaticPrompt = true)

        setContent {
            LouderMeApp(
                updateStatus = updateStatus.value,
                audioState = audioState.value,
                equalizerState = equalizerState.value,
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
                onEqEnabledChanged = { enabled ->
                    persistEqualizer(
                        equalizerState.value.copy(
                            enabled = enabled,
                            status = if (audioState.value.isRunning) {
                                EqualizerStatus.APPLYING
                            } else if (enabled) {
                                EqualizerStatus.READY
                            } else {
                                EqualizerStatus.DISABLED
                            },
                            message = if (enabled) "EQ ready." else "EQ is disabled.",
                        )
                    )
                },
                onEqPresetSelected = { preset ->
                    persistEqualizer(
                        equalizerState.value.copy(
                            enabled = true,
                            preset = preset,
                            gainsDb = EqualizerPresets.gainsFor(preset),
                            status = if (audioState.value.isRunning) {
                                EqualizerStatus.APPLYING
                            } else {
                                EqualizerStatus.READY
                            },
                            message = "Preset " + preset.displayName + " selected.",
                        )
                    )
                },
                onEqBandChanged = { index, gainDb ->
                    val gains = EqualizerPresets.sanitize(
                        equalizerState.value.gainsDb.toMutableList().also { list ->
                            if (index in list.indices) {
                                list[index] = gainDb
                            }
                        }
                    )

                    persistEqualizer(
                        equalizerState.value.copy(
                            enabled = true,
                            preset = EqualizerPreset.CUSTOM,
                            gainsDb = gains,
                            status = if (audioState.value.isRunning) {
                                EqualizerStatus.APPLYING
                            } else {
                                EqualizerStatus.READY
                            },
                            message = "Custom EQ updated.",
                        )
                    )
                },
            )
        }
    }

    override fun onStart() {
        super.onStart()

        if (!audioReceiverRegistered) {
            val filter = IntentFilter().apply {
                addAction(AudioBoostContract.ACTION_STATE)
                addAction(AudioBoostContract.ACTION_EQ_STATE)
            }

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

        val storedEq = EqualizerStateStore.read(this)
        equalizerState.value = if (audioState.value.isRunning) {
            storedEq.copy(
                status = equalizerState.value.status,
                implementation = equalizerState.value.implementation,
                message = equalizerState.value.message,
            )
        } else {
            storedEq
        }
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

    private fun receiveAudioState(intent: Intent) {
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

    private fun receiveEqualizerState(intent: Intent) {
        val status = runCatching {
            EqualizerStatus.valueOf(
                intent.getStringExtra(AudioBoostContract.EXTRA_EQ_STATUS)
                    ?: EqualizerStatus.ERROR.name
            )
        }.getOrDefault(EqualizerStatus.ERROR)

        val preset = runCatching {
            EqualizerPreset.valueOf(
                intent.getStringExtra(AudioBoostContract.EXTRA_EQ_PRESET)
                    ?: EqualizerPreset.CUSTOM.name
            )
        }.getOrDefault(EqualizerPreset.CUSTOM)

        val gains = intent.getFloatArrayExtra(
            AudioBoostContract.EXTRA_EQ_GAINS
        )?.toList() ?: equalizerState.value.gainsDb

        equalizerState.value = EqualizerUiState(
            enabled = intent.getBooleanExtra(
                AudioBoostContract.EXTRA_EQ_ENABLED,
                equalizerState.value.enabled,
            ),
            preset = preset,
            gainsDb = EqualizerPresets.sanitize(gains),
            status = status,
            implementation = intent.getStringExtra(
                AudioBoostContract.EXTRA_EQ_IMPLEMENTATION
            ) ?: "Unknown",
            message = intent.getStringExtra(
                AudioBoostContract.EXTRA_EQ_MESSAGE
            ) ?: "",
        )
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

    private fun persistEqualizer(state: EqualizerUiState) {
        val sanitized = state.copy(
            gainsDb = EqualizerPresets.sanitize(state.gainsDb)
        )

        EqualizerStateStore.write(this, sanitized)
        equalizerState.value = sanitized

        if (audioState.value.isRunning) {
            startService(
                Intent(this, AudioBoostService::class.java)
                    .setAction(AudioBoostContract.ACTION_SET_EQ)
            )
        }
    }
}
