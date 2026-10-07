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
import android.os.Handler
import android.os.Looper
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.mutableStateOf
import com.michelslab.louderme.audio.AudioBoostContract
import com.michelslab.louderme.audio.AudioBoostService
import com.michelslab.louderme.audio.AudioBoostStateStore
import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.DeviceVolumeController
import com.michelslab.louderme.audio.DeviceVolumeUiState
import com.michelslab.louderme.audio.EqualizerPreset
import com.michelslab.louderme.audio.EqualizerPresets
import com.michelslab.louderme.audio.EqualizerStateStore
import com.michelslab.louderme.audio.EqualizerStatus
import com.michelslab.louderme.audio.EqualizerUiState
import com.michelslab.louderme.ui.LouderMeApp
import com.michelslab.louderme.startup.StartupPreferences
import com.michelslab.louderme.update.UpdateCoordinator
import com.michelslab.louderme.update.UpdateStatus

class MainActivity : ComponentActivity() {
    private val updateStatus = mutableStateOf<UpdateStatus>(UpdateStatus.Checking)
    private val audioState = mutableStateOf(AudioEngineUiState())
    private val equalizerState = mutableStateOf(EqualizerUiState())
    private val deviceVolumeState = mutableStateOf(DeviceVolumeUiState())
    private val startOnBoot = mutableStateOf(false)

    private lateinit var updateCoordinator: UpdateCoordinator
    private lateinit var deviceVolumeController: DeviceVolumeController
    private val deviceVolumeSyncHandler = Handler(Looper.getMainLooper())
    private val deviceVolumeSync = object : Runnable {
        override fun run() {
            refreshDeviceVolume()
            deviceVolumeSyncHandler.postDelayed(this, 750L)
        }
    }
    private var pendingBoostPercent: Int? = null
    private var audioReceiverRegistered = false

    private val updateLauncher =
        registerForActivityResult(ActivityResultContracts.StartIntentSenderForResult()) { result ->
            if (result.resultCode != Activity.RESULT_OK && ::updateCoordinator.isInitialized) {
                updateCoordinator.markPlayUpdateCancelled()
            }
        }

    private val notificationPermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
            val pending = pendingBoostPercent
            pendingBoostPercent = null

            if (granted && pending != null) {
                startBoost(pending)
            } else if (!granted && pending != null && !audioState.value.isRunning) {
                audioState.value = audioState.value.copy(
                    status = AudioEngineStatus.OFF,
                    implementation = "Notification permission required",
                    message = "Boost was not started because notification permission was denied.",
                )
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
        deviceVolumeController = DeviceVolumeController(this)
        refreshDeviceVolume()
        startOnBoot.value = StartupPreferences.isStartOnBootEnabled(this)

        updateCoordinator = UpdateCoordinator(
            activity = this,
            updateLauncher = updateLauncher,
            onStatusChanged = { updateStatus.value = it },
        )

        updateCoordinator.checkForUpdates(automatic = true)

        setContent {
            LouderMeApp(
                updateStatus = updateStatus.value,
                audioState = audioState.value,
                equalizerState = equalizerState.value,
                deviceVolumeState = deviceVolumeState.value,
                startOnBoot = startOnBoot.value,
                onCheckForUpdates = {
                    updateCoordinator.checkForUpdates(automatic = false)
                },
                onInstallUpdate = {
                    updateCoordinator.installAvailableUpdate()
                },
                onStartOnBootChanged = { enabled ->
                    StartupPreferences.setStartOnBootEnabled(this, enabled)
                    startOnBoot.value = enabled
                },
                onDeviceVolumeChanged = { percent ->
                    setDeviceVolume(percent)
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
        refreshDeviceVolume()
        deviceVolumeSyncHandler.removeCallbacks(deviceVolumeSync)
        deviceVolumeSyncHandler.postDelayed(deviceVolumeSync, 750L)

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
        deviceVolumeSyncHandler.removeCallbacks(deviceVolumeSync)
        if (audioReceiverRegistered) {
            unregisterReceiver(audioStateReceiver)
            audioReceiverRegistered = false
        }
        super.onStop()
    }

    override fun onResume() {
        super.onResume()
        if (::updateCoordinator.isInitialized) {
            updateCoordinator.onResume()
        }
        refreshDeviceVolume()
    }

    override fun onDestroy() {
        deviceVolumeSyncHandler.removeCallbacks(deviceVolumeSync)
        if (::updateCoordinator.isInitialized) {
            updateCoordinator.shutdown()
        }
        super.onDestroy()
    }

    private fun refreshDeviceVolume() {
        if (!::deviceVolumeController.isInitialized) return

        runCatching {
            deviceVolumeController.read()
        }.onSuccess { state ->
            deviceVolumeState.value = state
        }.onFailure { error ->
            deviceVolumeState.value = deviceVolumeState.value.copy(
                available = false,
                message = "Device volume unavailable: " + (error.message ?: error.javaClass.simpleName),
            )
        }
    }

    private fun setDeviceVolume(percent: Int) {
        if (!::deviceVolumeController.isInitialized) return

        runCatching {
            deviceVolumeController.setPercent(percent)
        }.onSuccess { state ->
            deviceVolumeState.value = state
        }.onFailure { error ->
            deviceVolumeState.value = deviceVolumeState.value.copy(
                available = false,
                message = "Could not change device volume: " + (error.message ?: error.javaClass.simpleName),
            )
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

        runCatching {
            startForegroundService(intent)
        }.onFailure { error ->
            audioState.value = audioState.value.copy(
                status = AudioEngineStatus.ERROR,
                implementation = "Foreground service start failed",
                message = error.message ?: error.javaClass.simpleName,
            )
        }
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
