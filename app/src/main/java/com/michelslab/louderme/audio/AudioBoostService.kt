package com.michelslab.louderme.audio

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.content.pm.ServiceInfo
import android.media.AudioAttributes
import android.media.AudioDeviceCallback
import android.media.AudioDeviceInfo
import android.media.AudioManager
import android.os.Build
import android.os.Handler
import android.os.IBinder
import android.os.Looper
import com.michelslab.louderme.MainActivity

class AudioBoostService : Service() {
    private var engine: AudioEngine? = null
    private var equalizer: SessionZeroEqualizer? = null
    private var lastState = AudioEngineUiState()
    private var equalizerState = EqualizerUiState()
    private var deviceCallbackRegistered = false
    private val deviceCallback = object : AudioDeviceCallback() {
        override fun onAudioDevicesAdded(addedDevices: Array<out AudioDeviceInfo>?) {
            refreshOutputRoute()
        }

        override fun onAudioDevicesRemoved(removedDevices: Array<out AudioDeviceInfo>?) {
            refreshOutputRoute()
        }
    }

    override fun onCreate() {
        super.onCreate()
        createNotificationChannel()
        lastState = AudioBoostStateStore.read(this)
        equalizerState = EqualizerStateStore.read(this)
        getSystemService(AudioManager::class.java).registerAudioDeviceCallback(
            deviceCallback,
            Handler(Looper.getMainLooper()),
        )
        deviceCallbackRegistered = true
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        val action = intent?.action

        if (action == null) {
            if (lastState.isRunning) {
                activate(lastState.percent)
                return START_STICKY
            }
            stopSelf()
            return START_NOT_STICKY
        }

        when (action) {
            AudioBoostContract.ACTION_ENABLE,
            AudioBoostContract.ACTION_SET_LEVEL -> {
                val percent = intent.getIntExtra(
                    AudioBoostContract.EXTRA_PERCENT,
                    lastState.percent
                )
                activate(percent)
            }

            AudioBoostContract.ACTION_BOOST_UP,
            AudioBoostContract.ACTION_BOOST_DOWN -> {
                // Notification actions must never re-enable a previously
                // stopped session from a stale PendingIntent.
                if (lastState.isRunning) {
                    val next = if (action == AudioBoostContract.ACTION_BOOST_UP) {
                        BoostQuickLevels.next(lastState.percent)
                    } else {
                        BoostQuickLevels.previous(lastState.percent)
                    }
                    if (next != lastState.percent || engine == null) activate(next)
                } else {
                    stopSelf()
                }
            }

            AudioBoostContract.ACTION_SET_EQ -> {
                equalizerState = EqualizerStateStore.read(this)
                if (lastState.isRunning) {
                    if (engine == null) {
                        // The system may recreate this service with only saved
                        // UI state. Reattach Boost before applying its EQ.
                        activate(lastState.percent)
                    } else {
                        applyEqualizer()
                    }
                } else {
                    publishEqualizerState(
                        equalizerState.copy(
                            status = if (equalizerState.enabled) {
                                EqualizerStatus.READY
                            } else {
                                EqualizerStatus.DISABLED
                            },
                            implementation = "Waiting for audio engine",
                            message = if (equalizerState.enabled) {
                                "EQ will apply when Global Boost is active."
                            } else {
                                "EQ is disabled."
                            },
                        )
                    )
                }
            }

            AudioBoostContract.ACTION_DISABLE -> deactivate()
        }

        return if (lastState.isRunning) START_STICKY else START_NOT_STICKY
    }

    override fun onDestroy() {
        if (deviceCallbackRegistered) {
            getSystemService(AudioManager::class.java)
                .unregisterAudioDeviceCallback(deviceCallback)
            deviceCallbackRegistered = false
        }
        engine?.release()
        equalizer?.release()
        engine = null
        equalizer = null
        // Graceful service teardown must not leave a persisted "attached" UI.
        // An abrupt process kill never reaches this callback; Android's
        // START_STICKY restoration path still rechecks actual attachment.
        if (lastState.isRunning) {
            publishState(
                lastState.copy(
                    status = AudioEngineStatus.OFF,
                    implementation = "Not attached",
                    message = "Boost service stopped. Enable Boost to reconnect.",
                )
            )
        }
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun activate(requestedPercent: Int) {
        val percent = requestedPercent.coerceIn(100, 250)
        val starting = AudioEngineUiState(
            status = AudioEngineStatus.STARTING,
            percent = percent,
            gainDb = BoostMath.percentToDb(percent),
            outputRoute = detectOutputRoute(),
            implementation = "Session 0 compatibility probe",
            message = "Attaching audio engine…",
        )

        publishState(starting)
        startAsForeground(starting)

        val result = runCatching {
            val activeEngine = engine ?: SessionZeroAudioEngine().also {
                engine = it
            }
            activeEngine.enable(percent)
        }.getOrElse { error ->
            AudioEngineResult(
                status = AudioEngineStatus.ERROR,
                percent = percent,
                gainDb = BoostMath.percentToDb(percent),
                implementation = "Engine initialization failed",
                message = error.message ?: error::class.java.simpleName,
            )
        }

        val state = AudioEngineUiState(
            status = result.status,
            percent = result.percent,
            gainDb = result.gainDb,
            outputRoute = detectOutputRoute(),
            implementation = result.implementation,
            message = result.message,
        )

        publishState(state)

        if (state.isRunning) {
            applyEqualizer()
            updateNotification(state)
        } else {
            equalizer?.release()
            equalizer = null
            stopForeground(STOP_FOREGROUND_REMOVE)
            stopSelf()
        }
    }

    private fun applyEqualizer() {
        val applying = equalizerState.copy(
            status = EqualizerStatus.APPLYING,
            implementation = "Session 0 EQ probe",
            message = "Applying equalizer…",
        )
        publishEqualizerState(applying)

        val activeEqualizer = equalizer ?: SessionZeroEqualizer().also {
            equalizer = it
        }

        val result = activeEqualizer.apply(equalizerState)

        publishEqualizerState(
            equalizerState.copy(
                status = result.status,
                implementation = result.implementation,
                message = result.message,
            )
        )
        // EQ presets and the disabled state must be reflected in the
        // foreground notification immediately, not only after Boost changes.
        if (lastState.isRunning) updateNotification(lastState)
    }

    private fun deactivate() {
        val rememberedPercent = lastState.percent.coerceIn(100, 250)
        runCatching { engine?.disable() }
        engine?.release()
        equalizer?.release()
        engine = null
        equalizer = null

        val off = AudioEngineUiState(
            status = AudioEngineStatus.OFF,
            percent = rememberedPercent,
            gainDb = BoostMath.percentToDb(rememberedPercent),
            outputRoute = detectOutputRoute(),
            implementation = "Not attached",
            message = "Boost is off",
        )

        publishState(off)
        publishEqualizerState(
            EqualizerStateStore.read(this).let { saved ->
                saved.copy(
                    status = if (saved.enabled) EqualizerStatus.READY else EqualizerStatus.DISABLED,
                    implementation = "Waiting for audio engine",
                    message = if (saved.enabled) {
                        "EQ will apply when Global Boost is active."
                    } else {
                        "EQ is disabled."
                    },
                )
            }
        )

        stopForeground(STOP_FOREGROUND_REMOVE)
        stopSelf()
    }

    private fun publishState(state: AudioEngineUiState) {
        lastState = state
        AudioBoostStateStore.write(this, state)

        sendBroadcast(
            Intent(AudioBoostContract.ACTION_STATE)
                .setPackage(packageName)
                .putExtra(AudioBoostContract.EXTRA_STATUS, state.status.name)
                .putExtra(AudioBoostContract.EXTRA_PERCENT, state.percent)
                .putExtra(AudioBoostContract.EXTRA_GAIN_DB, state.gainDb)
                .putExtra(AudioBoostContract.EXTRA_ROUTE, state.outputRoute)
                .putExtra(AudioBoostContract.EXTRA_IMPLEMENTATION, state.implementation)
                .putExtra(AudioBoostContract.EXTRA_MESSAGE, state.message)
        )
    }

    private fun publishEqualizerState(state: EqualizerUiState) {
        equalizerState = state

        sendBroadcast(
            Intent(AudioBoostContract.ACTION_EQ_STATE)
                .setPackage(packageName)
                .putExtra(AudioBoostContract.EXTRA_EQ_ENABLED, state.enabled)
                .putExtra(AudioBoostContract.EXTRA_EQ_PRESET, state.preset.name)
                .putExtra(
                    AudioBoostContract.EXTRA_EQ_GAINS,
                    state.gainsDb.toFloatArray()
                )
                .putExtra(AudioBoostContract.EXTRA_EQ_STATUS, state.status.name)
                .putExtra(
                    AudioBoostContract.EXTRA_EQ_IMPLEMENTATION,
                    state.implementation
                )
                .putExtra(AudioBoostContract.EXTRA_EQ_MESSAGE, state.message)
        )
    }

    private fun startAsForeground(state: AudioEngineUiState) {
        val notification = buildNotification(state)

        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(
                NOTIFICATION_ID,
                notification,
                ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE,
            )
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }
    }

    private fun updateNotification(state: AudioEngineUiState) {
        getSystemService(NotificationManager::class.java)
            .notify(NOTIFICATION_ID, buildNotification(state))
    }

    private fun actionPendingIntent(action: String, requestCode: Int): PendingIntent =
        PendingIntent.getService(
            this,
            requestCode,
            Intent(this, AudioBoostService::class.java).setAction(action),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )

    private fun buildNotification(state: AudioEngineUiState): Notification {
        val launchIntent = Intent(this, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            launchIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )

        val eqLabel = if (equalizerState.enabled) {
            " · EQ " + equalizerState.preset.displayName
        } else {
            ""
        }

        return Notification.Builder(this, CHANNEL_ID)
            .setSmallIcon(android.R.drawable.ic_lock_silent_mode_off)
            .setContentTitle("LouderMe · " + state.percent + "%")
            .setContentText(
                when (state.status) {
                    AudioEngineStatus.ATTACHED ->
                        "Audio engine attached · " + state.outputRoute + eqLabel
                    AudioEngineStatus.DEGRADED ->
                        "Audio engine running with limited control" + eqLabel
                    AudioEngineStatus.STARTING -> "Starting audio engine…"
                    else -> state.message
                }
            )
            .setOngoing(true)
            .setOnlyAlertOnce(true)
            .setContentIntent(pendingIntent)
            .addAction(
                android.R.drawable.ic_media_rew,
                "Boost −",
                actionPendingIntent(AudioBoostContract.ACTION_BOOST_DOWN, 101),
            )
            .addAction(
                android.R.drawable.ic_media_ff,
                "Boost +",
                actionPendingIntent(AudioBoostContract.ACTION_BOOST_UP, 102),
            )
            .addAction(
                android.R.drawable.ic_media_pause,
                "Stop",
                actionPendingIntent(AudioBoostContract.ACTION_DISABLE, 103),
            )
            .build()
    }

    private fun createNotificationChannel() {
        val channel = NotificationChannel(
            CHANNEL_ID,
            "LouderMe audio boost",
            NotificationManager.IMPORTANCE_LOW,
        ).apply {
            description = "Shows while LouderMe keeps the user-requested audio effect active."
            setSound(null, null)
        }

        getSystemService(NotificationManager::class.java)
            .createNotificationChannel(channel)
    }

    private fun refreshOutputRoute() {
        // On service recreation, persisted ATTACHED isn't a live engine.
        // An initial device callback must not broadcast that stale status.
        if (!lastState.isRunning || engine == null) return
        val route = detectOutputRoute()
        if (route == lastState.outputRoute) return
        val refreshed = lastState.copy(outputRoute = route)
        publishState(refreshed)
        updateNotification(refreshed)
    }

    private fun detectOutputRoute(): String {
        val manager = getSystemService(AudioManager::class.java)
        // Connected outputs are not necessarily the output selected for media.
        // API 33+ reports the routing anticipated for MEDIA; on earlier
        // versions only the available-device approximation is accessible here.
        val devices: Array<out AudioDeviceInfo> =
            if (Build.VERSION.SDK_INT >= 33) {
                manager.getAudioDevicesForAttributes(
                    AudioAttributes.Builder()
                        .setUsage(AudioAttributes.USAGE_MEDIA)
                        .build()
                ).toTypedArray()
            } else {
                manager.getDevices(AudioManager.GET_DEVICES_OUTPUTS)
            }

        return AudioRouteLabel.describe(devices.map { it.type })
    }

    companion object {
        private const val CHANNEL_ID = "louderme_boost"
        private const val NOTIFICATION_ID = 1101
    }
}
