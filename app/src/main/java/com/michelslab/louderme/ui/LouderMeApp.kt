package com.michelslab.louderme.ui

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.dp
import com.michelslab.louderme.BuildConfig
import com.michelslab.louderme.R
import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.BoostMath
import com.michelslab.louderme.update.UpdateStatus
import java.util.Locale
import kotlin.math.roundToInt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LouderMeApp(
    updateStatus: UpdateStatus,
    audioState: AudioEngineUiState,
    onCheckForUpdates: () -> Unit,
    onInstallUpdate: () -> Unit,
    onBoostToggle: (Boolean) -> Unit,
    onBoostLevelSelected: (Int) -> Unit,
) {
    var showAbout by remember { mutableStateOf(false) }

    MaterialTheme {
        if (showAbout) {
            AboutScreen(
                updateStatus = updateStatus,
                onCheckForUpdates = onCheckForUpdates,
                onInstallUpdate = onInstallUpdate,
                onBack = { showAbout = false },
            )
        } else {
            HomeScreen(
                audioState = audioState,
                onAbout = { showAbout = true },
                onBoostToggle = onBoostToggle,
                onBoostLevelSelected = onBoostLevelSelected,
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun HomeScreen(
    audioState: AudioEngineUiState,
    onAbout: () -> Unit,
    onBoostToggle: (Boolean) -> Unit,
    onBoostLevelSelected: (Int) -> Unit,
) {
    val levels = listOf(100, 125, 150, 175, 200, 225, 250)
    var sliderValue by remember(audioState.percent) {
        mutableFloatStateOf(audioState.percent.toFloat())
    }
    var showDiagnostics by remember { mutableStateOf(false) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("LouderMe")
                        Text(
                            "Michel's Lab",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                },
                actions = {
                    TextButton(onClick = onAbout) {
                        Text("About")
                    }
                },
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .padding(horizontal = 18.dp, vertical = 12.dp)
                .fillMaxSize()
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(16.dp),
        ) {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceContainerHigh
                ),
            ) {
                Column(
                    modifier = Modifier.padding(18.dp),
                    verticalArrangement = Arrangement.spacedBy(14.dp),
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween,
                    ) {
                        Column {
                            Text(
                                "Global Boost",
                                style = MaterialTheme.typography.headlineSmall,
                            )
                            Text(
                                engineStatusText(audioState),
                                style = MaterialTheme.typography.bodySmall,
                                color = statusColor(audioState.status),
                            )
                        }

                        Switch(
                            checked = audioState.isRunning,
                            onCheckedChange = onBoostToggle,
                        )
                    }

                    Text(
                        "${audioState.percent}%",
                        style = MaterialTheme.typography.displayMedium,
                    )
                    Text(
                        String.format(
                            Locale.US,
                            "%+.2f dB target signal gain",
                            audioState.gainDb,
                        ),
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )

                    Column(
                        verticalArrangement = Arrangement.spacedBy(6.dp),
                    ) {
                        levels.chunked(4).forEach { rowLevels ->
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(5.dp),
                            ) {
                                rowLevels.forEach { level ->
                                    FilterChip(
                                        modifier = Modifier.weight(1f),
                                        selected = audioState.percent == level,
                                        onClick = { onBoostLevelSelected(level) },
                                        label = {
                                            Text(
                                                "$level%",
                                                maxLines = 1,
                                                style = MaterialTheme.typography.labelSmall,
                                            )
                                        },
                                    )
                                }
                                repeat(4 - rowLevels.size) {
                                    Spacer(modifier = Modifier.weight(1f))
                                }
                            }
                        }
                    }

                    Slider(
                        value = sliderValue,
                        onValueChange = { sliderValue = it },
                        onValueChangeFinished = {
                            onBoostLevelSelected(sliderValue.roundToInt())
                        },
                        valueRange = 100f..250f,
                        steps = 149,
                    )

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                    ) {
                        Text(
                            "100%",
                            style = MaterialTheme.typography.labelSmall,
                        )
                        Text(
                            "${sliderValue.roundToInt()}%",
                            style = MaterialTheme.typography.labelMedium,
                        )
                        Text(
                            "250%",
                            style = MaterialTheme.typography.labelSmall,
                        )
                    }
                }
            }

            Card(modifier = Modifier.fillMaxWidth()) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    Text(
                        "Output",
                        style = MaterialTheme.typography.titleMedium,
                    )
                    Text(audioState.outputRoute)
                    HorizontalDivider()
                    Text(
                        audioState.message,
                        style = MaterialTheme.typography.bodySmall,
                    )
                    TextButton(onClick = { showDiagnostics = !showDiagnostics }) {
                        Text(if (showDiagnostics) "Hide diagnostics" else "Engine diagnostics")
                    }

                    if (showDiagnostics) {
                        Text(
                            audioState.implementation,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                        Text(
                            "Compatibility mode uses Android audio session 0. It was audibly validated on the target Samsung device, but Android deprecates global insert effects on session 0 and compatibility can still vary by phone.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                OutlinedButton(
                    modifier = Modifier.weight(1f),
                    enabled = false,
                    onClick = {},
                ) {
                    Text("Mixer / EQ · next")
                }

                OutlinedButton(
                    modifier = Modifier.weight(1f),
                    enabled = false,
                    onClick = {},
                ) {
                    Text("Smart Boost · Beta")
                }
            }

            Text(
                "Target signal gain: 150% ≈ ${String.format(Locale.US, "%.2f", BoostMath.percentToDb(150))} dB, 200% ≈ ${String.format(Locale.US, "%.2f", BoostMath.percentToDb(200))} dB, and 250% ≈ ${String.format(Locale.US, "%.2f", BoostMath.percentToDb(250))} dB. These are digital signal-gain values, not acoustic dB SPL. Actual loudspeaker output depends on the device, DSP, source, distance, and output hardware.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun AboutScreen(
    updateStatus: UpdateStatus,
    onCheckForUpdates: () -> Unit,
    onInstallUpdate: () -> Unit,
    onBack: () -> Unit,
) {
    val uriHandler = LocalUriHandler.current
    val navy = Color(0xFF071835)
    val gold = Color(0xFFEFBD52)
    val light = Color(0xFFEEF6FF)
    val muted = Color(0xFFC9D9EB)

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("About") },
                navigationIcon = {
                    TextButton(onClick = onBack) {
                        Text("Back")
                    }
                },
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .padding(18.dp)
                .fillMaxSize()
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(16.dp),
        ) {
            Card(
                colors = CardDefaults.cardColors(containerColor = navy),
                shape = RoundedCornerShape(24.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(10.dp),
                ) {
                    Text(
                        "LOUDERME",
                        color = gold,
                        style = MaterialTheme.typography.labelLarge,
                    )
                    Text(
                        "Make everything louder.",
                        color = light,
                        style = MaterialTheme.typography.headlineSmall,
                    )
                    Text(
                        "Fast audio boost controls, device-aware diagnostics, mixer/EQ groundwork, and experimental smart processing.",
                        color = muted,
                        style = MaterialTheme.typography.bodyMedium,
                    )
                }
            }

            Card(
                colors = CardDefaults.cardColors(containerColor = navy),
                shape = RoundedCornerShape(22.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                Column(
                    modifier = Modifier.padding(18.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(9.dp),
                ) {
                    Image(
                        painter = painterResource(R.drawable.michel_duarte_avatar),
                        contentDescription = "Michel Duarte",
                        modifier = Modifier
                            .size(width = 126.dp, height = 174.dp)
                            .clip(RoundedCornerShape(22.dp)),
                        contentScale = ContentScale.Fit,
                    )
                    Text(
                        "Developer · Michel's Lab",
                        color = gold,
                        style = MaterialTheme.typography.labelLarge,
                    )
                    Text(
                        "Michel Duarte",
                        color = light,
                        style = MaterialTheme.typography.headlineSmall,
                    )
                    Text(
                        "Independent software by Michel's Lab.",
                        color = muted,
                    )

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.Center,
                    ) {
                        TextButton(
                            onClick = {
                                uriHandler.openUri("https://www.instagram.com/realmichelduarte/")
                            }
                        ) { Text("Instagram", color = gold) }

                        TextButton(
                            onClick = {
                                uriHandler.openUri("https://github.com/realmichelduarte")
                            }
                        ) { Text("GitHub", color = gold) }
                    }

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.Center,
                    ) {
                        TextButton(
                            onClick = {
                                uriHandler.openUri("https://www.linkedin.com/in/realmichelduart/")
                            }
                        ) { Text("LinkedIn", color = gold) }

                        TextButton(
                            onClick = {
                                uriHandler.openUri("mailto:realmichelduarte@gmail.com")
                            }
                        ) { Text("Email", color = gold) }
                    }
                }
            }

            Card(modifier = Modifier.fillMaxWidth()) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp),
                ) {
                    Text("App", style = MaterialTheme.typography.titleMedium)
                    Text("Version ${BuildConfig.VERSION_NAME} · code ${BuildConfig.VERSION_CODE}")
                    Text("Package ${BuildConfig.APPLICATION_ID}")
                    HorizontalDivider()
                    Text("Software update: ${updateStatusText(updateStatus)}")

                    if (
                        updateStatus == UpdateStatus.UpdateAvailable ||
                        updateStatus == UpdateStatus.UpdateCancelled
                    ) {
                        Button(onClick = onInstallUpdate) {
                            Text("Install update")
                        }
                    } else {
                        OutlinedButton(
                            enabled = updateStatus != UpdateStatus.Checking,
                            onClick = onCheckForUpdates,
                        ) {
                            Text("Check for updates")
                        }
                    }

                    Text(
                        "Google Play builds check for newer versions through Play. Direct/debug APKs are not owned by Play and therefore cannot use that production update channel.",
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
            }

            Card(modifier = Modifier.fillMaxWidth()) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(6.dp),
                ) {
                    Text("Privacy & legal", style = MaterialTheme.typography.titleMedium)
                    Text("LouderMe does not record, capture, or upload your audio.")
                    Text("Audio-effect state and app settings remain local on the device.")
                    Text("© 2026 Michel Armando Duarte Flores / Michel's Lab")
                    Text("Proprietary · All Rights Reserved")
                }
            }
        }
    }
}

@Composable
private fun statusColor(status: AudioEngineStatus): Color =
    when (status) {
        AudioEngineStatus.ATTACHED -> MaterialTheme.colorScheme.primary
        AudioEngineStatus.STARTING -> MaterialTheme.colorScheme.tertiary
        AudioEngineStatus.DEGRADED -> MaterialTheme.colorScheme.tertiary
        AudioEngineStatus.UNSUPPORTED,
        AudioEngineStatus.ERROR -> MaterialTheme.colorScheme.error
        AudioEngineStatus.OFF -> MaterialTheme.colorScheme.onSurfaceVariant
    }

private fun engineStatusText(state: AudioEngineUiState): String =
    when (state.status) {
        AudioEngineStatus.OFF -> "Boost off"
        AudioEngineStatus.STARTING -> "Starting engine…"
        AudioEngineStatus.ATTACHED -> "Engine attached"
        AudioEngineStatus.DEGRADED -> "Engine attached with limited control"
        AudioEngineStatus.UNSUPPORTED -> "Session-0 boost unsupported on this device"
        AudioEngineStatus.ERROR -> "Audio engine error"
    }

private fun updateStatusText(status: UpdateStatus): String =
    when (status) {
        UpdateStatus.Checking -> "Checking…"
        UpdateStatus.UpToDate -> "Up to date"
        UpdateStatus.UpdateAvailable -> "Update available"
        UpdateStatus.PlayStoreUnavailable -> "Play updates available only in the Play-installed build"
        UpdateStatus.UpdateCancelled -> "Update available · installation postponed"
        is UpdateStatus.Error -> "Unable to check"
    }
