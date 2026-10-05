package com.michelslab.louderme.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.michelslab.louderme.BuildConfig
import com.michelslab.louderme.R
import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.BoostMath
import com.michelslab.louderme.audio.EqualizerPreset
import com.michelslab.louderme.audio.EqualizerPresets
import com.michelslab.louderme.audio.EqualizerStatus
import com.michelslab.louderme.audio.EqualizerUiState
import com.michelslab.louderme.update.UpdateStatus
import java.util.Locale
import kotlin.math.roundToInt

@Composable
fun LouderMeApp(
    updateStatus: UpdateStatus,
    audioState: AudioEngineUiState,
    equalizerState: EqualizerUiState,
    onCheckForUpdates: () -> Unit,
    onInstallUpdate: () -> Unit,
    onBoostToggle: (Boolean) -> Unit,
    onBoostLevelSelected: (Int) -> Unit,
    onEqEnabledChanged: (Boolean) -> Unit,
    onEqPresetSelected: (EqualizerPreset) -> Unit,
    onEqBandChanged: (Int, Float) -> Unit,
) {
    var showAbout by remember { mutableStateOf(false) }

    LouderMeTheme {
        if (showAbout) {
            AboutScreen(
                updateStatus = updateStatus,
                onCheckForUpdates = onCheckForUpdates,
                onInstallUpdate = onInstallUpdate,
                onBack = { showAbout = false },
            )
        } else {
            HomeScreen(
                updateStatus = updateStatus,
                audioState = audioState,
                equalizerState = equalizerState,
                onAbout = { showAbout = true },
                onCheckForUpdates = onCheckForUpdates,
                onInstallUpdate = onInstallUpdate,
                onBoostToggle = onBoostToggle,
                onBoostLevelSelected = onBoostLevelSelected,
                onEqEnabledChanged = onEqEnabledChanged,
                onEqPresetSelected = onEqPresetSelected,
                onEqBandChanged = onEqBandChanged,
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun HomeScreen(
    updateStatus: UpdateStatus,
    audioState: AudioEngineUiState,
    equalizerState: EqualizerUiState,
    onAbout: () -> Unit,
    onCheckForUpdates: () -> Unit,
    onInstallUpdate: () -> Unit,
    onBoostToggle: (Boolean) -> Unit,
    onBoostLevelSelected: (Int) -> Unit,
    onEqEnabledChanged: (Boolean) -> Unit,
    onEqPresetSelected: (EqualizerPreset) -> Unit,
    onEqBandChanged: (Int, Float) -> Unit,
) {
    val levels = listOf(100, 125, 150, 175, 200, 225, 250)
    var sliderValue by remember(audioState.percent) {
        mutableFloatStateOf(audioState.percent.toFloat())
    }
    var showDiagnostics by remember { mutableStateOf(false) }

    Scaffold(
        containerColor = LouderMeColors.Bg,
        topBar = {
            CommandBar(
                eyebrow = "AUDIO WORKSPACE",
                title = "LouderMe",
                onAbout = onAbout,
            )
        },
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .background(LouderMeColors.Bg)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp, vertical = 18.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            WorkspaceHeader(
                eyebrow = "LOUDERME",
                title = "Boost Center",
                subtitle = "General audio gain, equalizer and diagnostics in one workspace.",
            )

            UpdateStrip(
                status = updateStatus,
                onCheckForUpdates = onCheckForUpdates,
                onInstallUpdate = onInstallUpdate,
            )

            BoostHero(
                audioState = audioState,
                onBoostToggle = onBoostToggle,
            )

            Panel {
                PanelHeading(
                    eyebrow = "QUICK LEVELS",
                    title = "Choose a boost",
                    subtitle = "One tap changes the active target signal gain.",
                )

                Spacer(Modifier.height(12.dp))

                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    levels.chunked(4).forEach { rowLevels ->
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            rowLevels.forEach { level ->
                                GainButton(
                                    modifier = Modifier.weight(1f),
                                    level = level,
                                    selected = audioState.percent == level,
                                    onClick = { onBoostLevelSelected(level) },
                                )
                            }
                            repeat(4 - rowLevels.size) {
                                Spacer(Modifier.weight(1f))
                            }
                        }
                    }
                }
            }

            Panel {
                PanelHeading(
                    eyebrow = "FINE TUNE",
                    title = "Manual gain",
                    subtitle = "Drag anywhere from 100% to 250%.",
                )

                Spacer(Modifier.height(8.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.Bottom,
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    Text(
                        sliderValue.roundToInt().toString() + "%",
                        color = LouderMeColors.Text,
                        fontSize = 30.sp,
                        fontWeight = FontWeight.Bold,
                    )
                    Text(
                        String.format(
                            Locale.US,
                            "%+.2f dB signal gain",
                            BoostMath.percentToDb(sliderValue.roundToInt()),
                        ),
                        color = LouderMeColors.Cyan,
                        fontFamily = FontFamily.Monospace,
                        fontSize = 11.sp,
                    )
                }

                Slider(
                    value = sliderValue,
                    onValueChange = { sliderValue = it },
                    onValueChangeFinished = {
                        onBoostLevelSelected(sliderValue.roundToInt())
                    },
                    valueRange = 100f..250f,
                    steps = 149,
                    colors = SliderDefaults.colors(
                        thumbColor = LouderMeColors.Cyan,
                        activeTrackColor = LouderMeColors.Blue,
                        inactiveTrackColor = LouderMeColors.Surface3,
                    ),
                )

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    MonoLabel("100%")
                    MonoLabel("250%")
                }
            }

            MetricGrid(audioState, equalizerState)

            EqualizerPanel(
                audioState = audioState,
                state = equalizerState,
                onEnabledChanged = onEqEnabledChanged,
                onPresetSelected = onEqPresetSelected,
                onBandChanged = onEqBandChanged,
            )

            Panel {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    PanelHeading(
                        eyebrow = "ENGINE",
                        title = "Diagnostics",
                        subtitle = audioState.message,
                        modifier = Modifier.weight(1f),
                    )
                    StatusBadge(audioState.status)
                }

                Spacer(Modifier.height(8.dp))

                TextButton(
                    onClick = { showDiagnostics = !showDiagnostics },
                    contentPadding = PaddingValues(horizontal = 0.dp, vertical = 4.dp),
                ) {
                    Text(
                        if (showDiagnostics) "Hide technical details" else "Show technical details",
                        color = LouderMeColors.Cyan,
                    )
                }

                if (showDiagnostics) {
                    DividerLine()
                    Spacer(Modifier.height(8.dp))
                    Eyebrow("BOOST ENGINE", LouderMeColors.Cyan)
                    Text(
                        audioState.implementation,
                        color = LouderMeColors.Text,
                        fontFamily = FontFamily.Monospace,
                        fontSize = 10.sp,
                        lineHeight = 15.sp,
                    )
                    Spacer(Modifier.height(10.dp))
                    Eyebrow("EQUALIZER", equalizerStatusColor(equalizerState.status))
                    Text(
                        equalizerState.implementation,
                        color = LouderMeColors.Text,
                        fontFamily = FontFamily.Monospace,
                        fontSize = 10.sp,
                        lineHeight = 15.sp,
                    )
                    Text(
                        equalizerState.message,
                        color = LouderMeColors.Muted,
                        style = MaterialTheme.typography.bodySmall,
                    )
                    Spacer(Modifier.height(10.dp))
                    Text(
                        "Both boost and EQ use the Android session-0 compatibility path. It is audibly validated on the target Samsung device, but Android deprecates global insert effects on session 0 and compatibility can vary by phone.",
                        color = LouderMeColors.Muted,
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
            }

            LaunchDeck()

            Text(
                "The dB figure shown for boost is target digital signal gain, not measured acoustic dB SPL. EQ values are requested band gain and may be clamped or mapped to the bands exposed by the device.",
                color = LouderMeColors.Dim,
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.padding(horizontal = 2.dp, vertical = 2.dp),
            )

            Spacer(Modifier.height(18.dp))
        }
    }
}

@Composable
private fun UpdateStrip(
    status: UpdateStatus,
    onCheckForUpdates: () -> Unit,
    onInstallUpdate: () -> Unit,
) {
    val accent = updateStatusColor(status)

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(16.dp))
            .background(LouderMeColors.Surface)
            .border(1.dp, accent.copy(alpha = 0.30f), RoundedCornerShape(16.dp))
            .padding(horizontal = 13.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Box(
            modifier = Modifier
                .size(8.dp)
                .clip(CircleShape)
                .background(accent)
        )

        Column(modifier = Modifier.weight(1f)) {
            Eyebrow("SOFTWARE", LouderMeColors.Muted)
            Text(
                updateStatusText(status),
                color = LouderMeColors.Text,
                style = MaterialTheme.typography.bodySmall,
            )
        }

        when (status) {
            is UpdateStatus.UpdateAvailable,
            is UpdateStatus.ReadyToInstall,
            is UpdateStatus.InstallPermissionRequired,
            UpdateStatus.UpdateCancelled -> {
                TextButton(onClick = onInstallUpdate) {
                    Text("Update", color = LouderMeColors.Gold)
                }
            }

            is UpdateStatus.Downloading -> {
                Text(
                    status.progressPercent.toString() + "%",
                    color = LouderMeColors.Cyan,
                    fontFamily = FontFamily.Monospace,
                    fontSize = 9.sp,
                    fontWeight = FontWeight.Bold,
                )
            }

            is UpdateStatus.Installing -> {
                Text(
                    "INSTALLING",
                    color = LouderMeColors.Gold,
                    fontFamily = FontFamily.Monospace,
                    fontSize = 8.sp,
                    fontWeight = FontWeight.Bold,
                )
            }

            UpdateStatus.Checking -> {
                Text(
                    "CHECKING",
                    color = LouderMeColors.Muted,
                    fontFamily = FontFamily.Monospace,
                    fontSize = 8.sp,
                    fontWeight = FontWeight.Bold,
                )
            }

            else -> {
                TextButton(onClick = onCheckForUpdates) {
                    Text("Check", color = LouderMeColors.Cyan)
                }
            }
        }
    }
}

@Composable
private fun EqualizerPanel(
    audioState: AudioEngineUiState,
    state: EqualizerUiState,
    onEnabledChanged: (Boolean) -> Unit,
    onPresetSelected: (EqualizerPreset) -> Unit,
    onBandChanged: (Int, Float) -> Unit,
) {
    Panel {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween,
        ) {
            PanelHeading(
                eyebrow = "MIXER / EQ",
                title = "7-band equalizer",
                subtitle = if (audioState.isRunning) {
                    state.message
                } else {
                    "Preset is saved now and applies when Global Boost is active."
                },
                modifier = Modifier.weight(1f),
            )

            Switch(
                checked = state.enabled,
                onCheckedChange = onEnabledChanged,
                colors = SwitchDefaults.colors(
                    checkedThumbColor = LouderMeColors.Text,
                    checkedTrackColor = LouderMeColors.Blue,
                    uncheckedThumbColor = LouderMeColors.Muted,
                    uncheckedTrackColor = LouderMeColors.Surface3,
                ),
            )
        }

        Spacer(Modifier.height(13.dp))

        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            EqStatusBadge(state.status)
            Text(
                state.preset.displayName,
                color = LouderMeColors.Text,
                fontFamily = FontFamily.Monospace,
                fontSize = 10.sp,
                fontWeight = FontWeight.Bold,
            )
        }

        Spacer(Modifier.height(13.dp))

        Eyebrow("PRESETS", LouderMeColors.Muted)
        Spacer(Modifier.height(8.dp))

        val presets = listOf(
            EqualizerPreset.FLAT,
            EqualizerPreset.BASS,
            EqualizerPreset.DEEP_BASS,
            EqualizerPreset.DIALOGUE,
            EqualizerPreset.TREBLE,
            EqualizerPreset.SPEAKER,
            EqualizerPreset.HEADPHONES,
        )

        Column(verticalArrangement = Arrangement.spacedBy(7.dp)) {
            presets.chunked(3).forEach { rowPresets ->
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(7.dp),
                ) {
                    rowPresets.forEach { preset ->
                        EqPresetButton(
                            modifier = Modifier.weight(1f),
                            preset = preset,
                            selected = state.preset == preset,
                            onClick = { onPresetSelected(preset) },
                        )
                    }
                    repeat(3 - rowPresets.size) {
                        Spacer(Modifier.weight(1f))
                    }
                }
            }
        }

        Spacer(Modifier.height(16.dp))
        DividerLine()
        Spacer(Modifier.height(12.dp))

        EqualizerPresets.labels.forEachIndexed { index, label ->
            EqBandControl(
                label = label,
                gainDb = state.gainsDb.getOrElse(index) { 0f },
                enabled = state.enabled,
                onChange = { gain -> onBandChanged(index, gain) },
            )
            if (index != EqualizerPresets.labels.lastIndex) {
                Spacer(Modifier.height(7.dp))
            }
        }

        Spacer(Modifier.height(11.dp))
        Text(
            "The 7 controls are LouderMe target bands. Android devices can expose a different number of physical EQ bands, so LouderMe maps each target frequency to the nearest band reported by the device.",
            color = LouderMeColors.Dim,
            style = MaterialTheme.typography.bodySmall,
        )
    }
}

@Composable
private fun EqBandControl(
    label: String,
    gainDb: Float,
    enabled: Boolean,
    onChange: (Float) -> Unit,
) {
    var localValue by remember(gainDb) { mutableFloatStateOf(gainDb) }

    Column {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
        ) {
            Text(
                label + " Hz",
                color = if (enabled) LouderMeColors.Text else LouderMeColors.Dim,
                fontFamily = FontFamily.Monospace,
                fontSize = 10.sp,
                fontWeight = FontWeight.Bold,
            )
            Text(
                String.format(Locale.US, "%+.1f dB", localValue),
                color = if (enabled) LouderMeColors.Cyan else LouderMeColors.Dim,
                fontFamily = FontFamily.Monospace,
                fontSize = 10.sp,
            )
        }

        Slider(
            value = localValue,
            onValueChange = {
                localValue = (it * 2f).roundToInt() / 2f
            },
            onValueChangeFinished = {
                onChange(localValue)
            },
            enabled = enabled,
            valueRange = -10f..10f,
            steps = 39,
            colors = SliderDefaults.colors(
                thumbColor = LouderMeColors.Cyan,
                activeTrackColor = LouderMeColors.Blue,
                inactiveTrackColor = LouderMeColors.Surface3,
                disabledThumbColor = LouderMeColors.Dim,
                disabledActiveTrackColor = LouderMeColors.Dim,
                disabledInactiveTrackColor = LouderMeColors.Surface3,
            ),
        )
    }
}

@Composable
private fun EqPresetButton(
    modifier: Modifier,
    preset: EqualizerPreset,
    selected: Boolean,
    onClick: () -> Unit,
) {
    val shape = RoundedCornerShape(12.dp)

    Box(
        modifier = modifier
            .heightIn(min = 44.dp)
            .clip(shape)
            .background(
                if (selected) {
                    LouderMeColors.Blue.copy(alpha = 0.18f)
                } else {
                    LouderMeColors.Surface2
                }
            )
            .border(
                1.dp,
                if (selected) LouderMeColors.Blue else LouderMeColors.Line,
                shape,
            )
            .clickable(onClick = onClick)
            .padding(horizontal = 7.dp, vertical = 9.dp),
        contentAlignment = Alignment.Center,
    ) {
        Text(
            preset.displayName,
            color = if (selected) Color.White else LouderMeColors.Muted,
            fontWeight = FontWeight.Bold,
            fontSize = 9.sp,
            textAlign = TextAlign.Center,
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CommandBar(
    eyebrow: String,
    title: String,
    onAbout: () -> Unit,
) {
    Surface(
        color = LouderMeColors.Canvas,
        shadowElevation = 0.dp,
        tonalElevation = 0.dp,
        border = BorderStroke(1.dp, LouderMeColors.Line),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 11.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(11.dp),
        ) {
            Box(
                modifier = Modifier
                    .size(39.dp)
                    .clip(RoundedCornerShape(12.dp))
                    .background(
                        Brush.linearGradient(
                            listOf(
                                Color(0xFF234879),
                                LouderMeColors.Surface2,
                            )
                        )
                    )
                    .border(
                        1.dp,
                        LouderMeColors.LineStrong,
                        RoundedCornerShape(12.dp),
                    ),
                contentAlignment = Alignment.Center,
            ) {
                Text(
                    "LM",
                    color = LouderMeColors.Cyan,
                    fontWeight = FontWeight.Black,
                    fontFamily = FontFamily.Monospace,
                    fontSize = 12.sp,
                )
            }

            Column(modifier = Modifier.weight(1f)) {
                Eyebrow(eyebrow, LouderMeColors.Cyan)
                Text(
                    title,
                    color = LouderMeColors.Text,
                    style = MaterialTheme.typography.titleLarge,
                )
            }

            OutlinedButton(
                onClick = onAbout,
                shape = RoundedCornerShape(12.dp),
                border = BorderStroke(1.dp, LouderMeColors.Line),
                colors = ButtonDefaults.outlinedButtonColors(
                    contentColor = LouderMeColors.Text,
                ),
                contentPadding = PaddingValues(horizontal = 13.dp, vertical = 8.dp),
            ) {
                Text("About", fontSize = 10.sp, fontWeight = FontWeight.Bold)
            }
        }
    }
}

@Composable
private fun WorkspaceHeader(
    eyebrow: String,
    title: String,
    subtitle: String,
) {
    Column(modifier = Modifier.padding(horizontal = 2.dp, vertical = 2.dp)) {
        Eyebrow(eyebrow, LouderMeColors.Dim)
        Spacer(Modifier.height(5.dp))
        Text(
            title,
            color = LouderMeColors.Text,
            style = MaterialTheme.typography.headlineLarge,
        )
        Spacer(Modifier.height(6.dp))
        Text(
            subtitle,
            color = LouderMeColors.Muted,
            style = MaterialTheme.typography.bodySmall,
        )
    }
}

@Composable
private fun BoostHero(
    audioState: AudioEngineUiState,
    onBoostToggle: (Boolean) -> Unit,
) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(26.dp))
            .background(
                Brush.linearGradient(
                    listOf(
                        Color(0xCC192D4A),
                        Color(0xF00B131F),
                    )
                )
            )
            .border(
                1.dp,
                LouderMeColors.LineStrong,
                RoundedCornerShape(26.dp),
            )
            .padding(18.dp),
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(13.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                Column {
                    Eyebrow("LIVE ENGINE", LouderMeColors.Cyan)
                    Spacer(Modifier.height(5.dp))
                    Text(
                        "Global Boost",
                        color = LouderMeColors.Text,
                        style = MaterialTheme.typography.headlineMedium,
                    )
                }
                StatusBadge(audioState.status)
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.Bottom,
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                Column {
                    Text(
                        audioState.percent.toString() + "%",
                        color = Color.White,
                        fontSize = 56.sp,
                        lineHeight = 58.sp,
                        fontWeight = FontWeight.Black,
                        letterSpacing = (-2.0).sp,
                    )
                    Text(
                        String.format(
                            Locale.US,
                            "%+.2f dB · TARGET SIGNAL GAIN",
                            audioState.gainDb,
                        ),
                        color = LouderMeColors.Cyan,
                        fontFamily = FontFamily.Monospace,
                        fontWeight = FontWeight.Bold,
                        fontSize = 10.sp,
                        letterSpacing = 0.7.sp,
                    )
                }

                Switch(
                    checked = audioState.isRunning,
                    onCheckedChange = onBoostToggle,
                    colors = SwitchDefaults.colors(
                        checkedThumbColor = LouderMeColors.Text,
                        checkedTrackColor = LouderMeColors.Blue,
                        uncheckedThumbColor = LouderMeColors.Muted,
                        uncheckedTrackColor = LouderMeColors.Surface3,
                        uncheckedBorderColor = LouderMeColors.LineStrong,
                    ),
                )
            }

            DividerLine()

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column {
                    MonoLabel("OUTPUT ROUTE")
                    Text(
                        audioState.outputRoute,
                        color = LouderMeColors.Text,
                        style = MaterialTheme.typography.titleMedium,
                    )
                }
                Text(
                    engineStatusText(audioState),
                    color = statusColor(audioState.status),
                    style = MaterialTheme.typography.labelMedium,
                    textAlign = TextAlign.End,
                )
            }
        }
    }
}

@Composable
private fun GainButton(
    modifier: Modifier,
    level: Int,
    selected: Boolean,
    onClick: () -> Unit,
) {
    val shape = RoundedCornerShape(14.dp)
    val background = if (selected) {
        Brush.linearGradient(
            listOf(
                Color(0xFF337ACB),
                Color(0xFF5BA8FF),
            )
        )
    } else {
        Brush.linearGradient(
            listOf(
                LouderMeColors.Surface2,
                LouderMeColors.Surface2,
            )
        )
    }

    Box(
        modifier = modifier
            .height(54.dp)
            .clip(shape)
            .background(background)
            .border(
                1.dp,
                if (selected) LouderMeColors.Cyan else LouderMeColors.Line,
                shape,
            )
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center,
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text(
                level.toString() + "%",
                color = if (selected) Color.White else LouderMeColors.Text,
                fontWeight = FontWeight.ExtraBold,
                fontSize = 13.sp,
            )
            if (selected) {
                Text(
                    "ACTIVE",
                    color = Color(0xFFD8F4FF),
                    fontFamily = FontFamily.Monospace,
                    fontWeight = FontWeight.Bold,
                    fontSize = 7.sp,
                    letterSpacing = 0.8.sp,
                )
            }
        }
    }
}

@Composable
private fun MetricGrid(
    audioState: AudioEngineUiState,
    equalizerState: EqualizerUiState,
) {
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            MetricCard(
                modifier = Modifier.weight(1f),
                eyebrow = "OUTPUT",
                value = audioState.outputRoute,
                accent = LouderMeColors.Cyan,
            )
            MetricCard(
                modifier = Modifier.weight(1f),
                eyebrow = "ENGINE",
                value = engineMetricText(audioState.status),
                accent = statusColor(audioState.status),
            )
        }
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            MetricCard(
                modifier = Modifier.weight(1f),
                eyebrow = "SIGNAL",
                value = String.format(Locale.US, "%+.2f dB", audioState.gainDb),
                accent = LouderMeColors.Blue,
            )
            MetricCard(
                modifier = Modifier.weight(1f),
                eyebrow = "EQ",
                value = if (equalizerState.enabled) {
                    equalizerState.preset.displayName
                } else {
                    "Off"
                },
                accent = equalizerStatusColor(equalizerState.status),
            )
        }
    }
}

@Composable
private fun MetricCard(
    modifier: Modifier,
    eyebrow: String,
    value: String,
    accent: Color,
) {
    Box(
        modifier = modifier
            .clip(RoundedCornerShape(16.dp))
            .background(LouderMeColors.Surface2)
            .border(1.dp, LouderMeColors.Line, RoundedCornerShape(16.dp))
            .padding(13.dp),
    ) {
        Column {
            Eyebrow(eyebrow, LouderMeColors.Muted)
            Spacer(Modifier.height(8.dp))
            Text(
                value,
                color = accent,
                fontWeight = FontWeight.ExtraBold,
                fontSize = 15.sp,
                maxLines = 2,
            )
        }
    }
}

@Composable
private fun LaunchDeck() {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(24.dp))
            .background(
                Brush.linearGradient(
                    listOf(
                        Color(0xCC192D4A),
                        Color(0xEE0B131F),
                    )
                )
            )
            .border(
                1.dp,
                Color(0x3067B3FF),
                RoundedCornerShape(24.dp),
            )
            .padding(16.dp),
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(11.dp)) {
            PanelHeading(
                eyebrow = "NEXT MODULES",
                title = "Audio Lab",
                subtitle = "The core boost and equalizer are live; these are the next processing layers.",
            )

            FeatureCard(
                tag = "PROFILES",
                title = "Output profiles",
                subtitle = "Remember separate tuning for speaker, Bluetooth and headphones.",
                accent = LouderMeColors.Cyan,
            )
            FeatureCard(
                tag = "PROTECTION",
                title = "Limiter / compressor",
                subtitle = "Control peaks and keep 225–250% more usable on hot material.",
                accent = LouderMeColors.Gold,
            )
            FeatureCard(
                tag = "BETA",
                title = "Smart Boost",
                subtitle = "Automatic dynamics for quiet content without blindly raising peaks.",
                accent = LouderMeColors.Violet,
            )
        }
    }
}

@Composable
private fun FeatureCard(
    tag: String,
    title: String,
    subtitle: String,
    accent: Color,
) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(17.dp))
            .background(Color(0x85060B13))
            .border(1.dp, LouderMeColors.Line, RoundedCornerShape(17.dp))
            .padding(14.dp),
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Eyebrow(tag, accent)
                Spacer(Modifier.height(7.dp))
                Text(
                    title,
                    color = LouderMeColors.Text,
                    fontWeight = FontWeight.Bold,
                    fontSize = 14.sp,
                )
                Spacer(Modifier.height(4.dp))
                Text(
                    subtitle,
                    color = LouderMeColors.Muted,
                    style = MaterialTheme.typography.bodySmall,
                )
            }
            Text(
                "NEXT",
                color = LouderMeColors.Dim,
                fontFamily = FontFamily.Monospace,
                fontSize = 8.sp,
                fontWeight = FontWeight.Bold,
            )
        }
    }
}

@Composable
private fun Panel(content: @Composable ColumnScope.() -> Unit) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(20.dp))
            .background(
                Brush.verticalGradient(
                    listOf(
                        Color(0xE80E1724),
                        Color(0xE80A111C),
                    )
                )
            )
            .border(1.dp, LouderMeColors.Line, RoundedCornerShape(20.dp))
            .padding(16.dp),
        content = content,
    )
}

@Composable
private fun PanelHeading(
    eyebrow: String,
    title: String,
    subtitle: String,
    modifier: Modifier = Modifier,
) {
    Column(modifier = modifier) {
        Eyebrow(eyebrow, LouderMeColors.Muted)
        Spacer(Modifier.height(5.dp))
        Text(
            title,
            color = LouderMeColors.Text,
            style = MaterialTheme.typography.titleLarge,
        )
        if (subtitle.isNotBlank()) {
            Spacer(Modifier.height(4.dp))
            Text(
                subtitle,
                color = LouderMeColors.Muted,
                style = MaterialTheme.typography.bodySmall,
            )
        }
    }
}

@Composable
private fun Eyebrow(text: String, color: Color) {
    Text(
        text,
        color = color,
        fontFamily = FontFamily.Monospace,
        fontSize = 8.sp,
        fontWeight = FontWeight.ExtraBold,
        letterSpacing = 1.25.sp,
    )
}

@Composable
private fun MonoLabel(text: String) {
    Text(
        text,
        color = LouderMeColors.Muted,
        fontFamily = FontFamily.Monospace,
        fontSize = 9.sp,
        fontWeight = FontWeight.Bold,
    )
}

@Composable
private fun DividerLine() {
    HorizontalDivider(
        color = LouderMeColors.Line,
        thickness = 1.dp,
    )
}

@Composable
private fun StatusBadge(status: AudioEngineStatus) {
    val color = statusColor(status)
    val text = when (status) {
        AudioEngineStatus.OFF -> "OFF"
        AudioEngineStatus.STARTING -> "STARTING"
        AudioEngineStatus.ATTACHED -> "ATTACHED"
        AudioEngineStatus.DEGRADED -> "DEGRADED"
        AudioEngineStatus.UNSUPPORTED -> "UNSUPPORTED"
        AudioEngineStatus.ERROR -> "ERROR"
    }

    BadgePill(text, color)
}

@Composable
private fun EqStatusBadge(status: EqualizerStatus) {
    val text = when (status) {
        EqualizerStatus.READY -> "READY"
        EqualizerStatus.APPLYING -> "APPLYING"
        EqualizerStatus.ATTACHED -> "ATTACHED"
        EqualizerStatus.DEGRADED -> "DEGRADED"
        EqualizerStatus.DISABLED -> "OFF"
        EqualizerStatus.UNSUPPORTED -> "UNSUPPORTED"
        EqualizerStatus.ERROR -> "ERROR"
    }

    BadgePill(text, equalizerStatusColor(status))
}

@Composable
private fun BadgePill(text: String, color: Color) {
    Box(
        modifier = Modifier
            .clip(CircleShape)
            .background(color.copy(alpha = 0.09f))
            .border(1.dp, color.copy(alpha = 0.38f), CircleShape)
            .padding(horizontal = 9.dp, vertical = 5.dp),
    ) {
        Text(
            text,
            color = color,
            fontFamily = FontFamily.Monospace,
            fontWeight = FontWeight.ExtraBold,
            fontSize = 8.sp,
            letterSpacing = 0.5.sp,
        )
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

    Scaffold(
        containerColor = LouderMeColors.Bg,
        topBar = {
            Surface(
                color = LouderMeColors.Canvas,
                border = BorderStroke(1.dp, LouderMeColors.Line),
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 12.dp, vertical = 10.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    TextButton(onClick = onBack) {
                        Text("Back", color = LouderMeColors.Cyan)
                    }
                    Column(modifier = Modifier.weight(1f)) {
                        Eyebrow("MICHEL'S LAB", LouderMeColors.Gold)
                        Text(
                            "About LouderMe",
                            color = LouderMeColors.Text,
                            style = MaterialTheme.typography.titleLarge,
                        )
                    }
                }
            }
        },
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .background(LouderMeColors.Bg)
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(26.dp))
                    .background(
                        Brush.linearGradient(
                            listOf(
                                Color(0xFF173159),
                                Color(0xFF08111E),
                            )
                        )
                    )
                    .border(
                        1.dp,
                        LouderMeColors.LineStrong,
                        RoundedCornerShape(26.dp),
                    )
                    .padding(18.dp),
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Eyebrow("LOUDERME", LouderMeColors.Cyan)
                    Spacer(Modifier.height(7.dp))
                    Text(
                        "Make everything louder.",
                        color = Color.White,
                        style = MaterialTheme.typography.headlineMedium,
                        textAlign = TextAlign.Center,
                    )
                    Spacer(Modifier.height(7.dp))
                    Text(
                        "A Michel's Lab audio utility built around fast gain control, equalization and transparent diagnostics.",
                        color = LouderMeColors.Muted,
                        style = MaterialTheme.typography.bodyMedium,
                        textAlign = TextAlign.Center,
                    )
                }
            }

            Panel {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(15.dp),
                ) {
                    Image(
                        painter = painterResource(R.drawable.michel_duarte_avatar),
                        contentDescription = "Michel Duarte",
                        modifier = Modifier
                            .size(width = 96.dp, height = 132.dp)
                            .clip(RoundedCornerShape(18.dp))
                            .border(
                                1.dp,
                                LouderMeColors.Gold,
                                RoundedCornerShape(18.dp),
                            ),
                        contentScale = ContentScale.Fit,
                    )

                    Column(modifier = Modifier.weight(1f)) {
                        Eyebrow("DEVELOPER · MICHEL'S LAB", LouderMeColors.Gold)
                        Spacer(Modifier.height(7.dp))
                        Text(
                            "Michel Duarte",
                            color = LouderMeColors.Text,
                            style = MaterialTheme.typography.headlineMedium,
                        )
                        Spacer(Modifier.height(5.dp))
                        Text(
                            "Independent software by Michel's Lab.",
                            color = LouderMeColors.Muted,
                            style = MaterialTheme.typography.bodySmall,
                        )
                    }
                }

                Spacer(Modifier.height(14.dp))

                SocialButton("Instagram") {
                    uriHandler.openUri("https://www.instagram.com/realmichelduarte/")
                }
                SocialButton("LinkedIn") {
                    uriHandler.openUri("https://www.linkedin.com/in/realmichelduart/")
                }
                SocialButton("GitHub") {
                    uriHandler.openUri("https://github.com/realmichelduarte")
                }
                SocialButton("Email") {
                    uriHandler.openUri("mailto:realmichelduarte@gmail.com")
                }
            }

            Panel {
                PanelHeading(
                    eyebrow = "APP",
                    title = "Build information",
                    subtitle = "",
                )
                Spacer(Modifier.height(12.dp))
                InfoRow(
                    "Version",
                    BuildConfig.VERSION_NAME + " · code " + BuildConfig.VERSION_CODE,
                )
                InfoRow("Package", BuildConfig.APPLICATION_ID)
                InfoRow(
                    "Channel",
                    if (BuildConfig.UPDATE_CHANNEL == "play") {
                        "Google Play"
                    } else {
                        "Michel's Lab direct"
                    },
                )
                InfoRow("Update", updateStatusText(updateStatus))
                Spacer(Modifier.height(12.dp))

                if (
                    updateStatus is UpdateStatus.UpdateAvailable ||
                    updateStatus is UpdateStatus.ReadyToInstall ||
                    updateStatus is UpdateStatus.InstallPermissionRequired ||
                    updateStatus == UpdateStatus.UpdateCancelled
                ) {
                    Button(
                        onClick = onInstallUpdate,
                        colors = ButtonDefaults.buttonColors(
                            containerColor = LouderMeColors.Blue,
                            contentColor = Color.White,
                        ),
                        shape = RoundedCornerShape(12.dp),
                    ) {
                        Text("Install update")
                    }
                } else {
                    OutlinedButton(
                        enabled =
                            updateStatus != UpdateStatus.Checking &&
                            updateStatus !is UpdateStatus.Downloading &&
                            updateStatus !is UpdateStatus.Installing,
                        onClick = onCheckForUpdates,
                        border = BorderStroke(1.dp, LouderMeColors.LineStrong),
                        shape = RoundedCornerShape(12.dp),
                    ) {
                        Text("Check for updates")
                    }
                }

                Spacer(Modifier.height(8.dp))
                Text(
                    "Google Play builds update through Play. Michel's Lab direct builds check the public release feed automatically, download and verify the APK, then hand installation to Android for the required system confirmation.",
                    color = LouderMeColors.Muted,
                    style = MaterialTheme.typography.bodySmall,
                )
            }

            Panel {
                PanelHeading(
                    eyebrow = "PRIVACY & LEGAL",
                    title = "Local audio processing",
                    subtitle = "LouderMe does not record, capture or upload your audio.",
                )
                Spacer(Modifier.height(11.dp))
                Text(
                    "Boost, equalizer state and app settings stay local on the device.",
                    color = LouderMeColors.Muted,
                    style = MaterialTheme.typography.bodySmall,
                )
                Spacer(Modifier.height(8.dp))
                Text(
                    "© 2026 Michel Armando Duarte Flores / Michel's Lab",
                    color = LouderMeColors.Text,
                    style = MaterialTheme.typography.bodySmall,
                )
                Text(
                    "Proprietary · All Rights Reserved",
                    color = LouderMeColors.Gold,
                    style = MaterialTheme.typography.labelMedium,
                )
            }

            Spacer(Modifier.height(18.dp))
        }
    }
}

@Composable
private fun SocialButton(
    label: String,
    onClick: () -> Unit,
) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 7.dp)
            .clip(RoundedCornerShape(13.dp))
            .background(LouderMeColors.Surface2)
            .border(1.dp, LouderMeColors.Line, RoundedCornerShape(13.dp))
            .clickable(onClick = onClick)
            .padding(horizontal = 13.dp, vertical = 11.dp),
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
        ) {
            Text(
                label,
                color = LouderMeColors.Text,
                fontWeight = FontWeight.Bold,
                fontSize = 11.sp,
            )
            Text(
                "↗",
                color = LouderMeColors.Cyan,
                fontSize = 13.sp,
            )
        }
    }
}

@Composable
private fun InfoRow(label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 5.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.Top,
    ) {
        Text(
            label,
            color = LouderMeColors.Muted,
            style = MaterialTheme.typography.bodySmall,
        )
        Text(
            value,
            color = LouderMeColors.Text,
            style = MaterialTheme.typography.bodySmall,
            textAlign = TextAlign.End,
            modifier = Modifier.padding(start = 18.dp),
        )
    }
}

@Composable
private fun statusColor(status: AudioEngineStatus): Color =
    when (status) {
        AudioEngineStatus.ATTACHED -> LouderMeColors.Green
        AudioEngineStatus.STARTING -> LouderMeColors.Cyan
        AudioEngineStatus.DEGRADED -> LouderMeColors.Gold
        AudioEngineStatus.UNSUPPORTED,
        AudioEngineStatus.ERROR -> LouderMeColors.Red
        AudioEngineStatus.OFF -> LouderMeColors.Muted
    }

@Composable
private fun equalizerStatusColor(status: EqualizerStatus): Color =
    when (status) {
        EqualizerStatus.READY -> LouderMeColors.Cyan
        EqualizerStatus.APPLYING -> LouderMeColors.Cyan
        EqualizerStatus.ATTACHED -> LouderMeColors.Green
        EqualizerStatus.DEGRADED -> LouderMeColors.Gold
        EqualizerStatus.DISABLED -> LouderMeColors.Muted
        EqualizerStatus.UNSUPPORTED,
        EqualizerStatus.ERROR -> LouderMeColors.Red
    }

@Composable
private fun updateStatusColor(status: UpdateStatus): Color =
    when (status) {
        UpdateStatus.Checking -> LouderMeColors.Cyan
        UpdateStatus.UpToDate -> LouderMeColors.Green
        is UpdateStatus.UpdateAvailable -> LouderMeColors.Gold
        is UpdateStatus.Downloading -> LouderMeColors.Cyan
        is UpdateStatus.ReadyToInstall -> LouderMeColors.Gold
        is UpdateStatus.InstallPermissionRequired -> LouderMeColors.Gold
        is UpdateStatus.Installing -> LouderMeColors.Gold
        UpdateStatus.PlayStoreUnavailable -> LouderMeColors.Muted
        UpdateStatus.UpdateCancelled -> LouderMeColors.Gold
        is UpdateStatus.Error -> LouderMeColors.Red
    }

private fun engineStatusText(state: AudioEngineUiState): String =
    when (state.status) {
        AudioEngineStatus.OFF -> "Boost off"
        AudioEngineStatus.STARTING -> "Starting engine…"
        AudioEngineStatus.ATTACHED -> "Engine attached"
        AudioEngineStatus.DEGRADED -> "Attached · limited control"
        AudioEngineStatus.UNSUPPORTED -> "Unsupported on this device"
        AudioEngineStatus.ERROR -> "Audio engine error"
    }

private fun engineMetricText(status: AudioEngineStatus): String =
    when (status) {
        AudioEngineStatus.OFF -> "Off"
        AudioEngineStatus.STARTING -> "Starting"
        AudioEngineStatus.ATTACHED -> "Attached"
        AudioEngineStatus.DEGRADED -> "Degraded"
        AudioEngineStatus.UNSUPPORTED -> "Unsupported"
        AudioEngineStatus.ERROR -> "Error"
    }

private fun updateStatusText(status: UpdateStatus): String =
    when (status) {
        UpdateStatus.Checking ->
            "Checking for updates…"

        UpdateStatus.UpToDate ->
            "Up to date"

        is UpdateStatus.UpdateAvailable ->
            status.versionName?.let {
                "Version " + it + " available"
            } ?: "Update available"

        is UpdateStatus.Downloading ->
            "Downloading " + status.versionName +
                " · " + status.progressPercent + "%"

        is UpdateStatus.ReadyToInstall ->
            "Version " + status.versionName +
                " verified · ready to install"

        is UpdateStatus.InstallPermissionRequired ->
            "Allow LouderMe to install this verified update"

        is UpdateStatus.Installing ->
            "Installing version " + status.versionName + "…"

        UpdateStatus.PlayStoreUnavailable ->
            "Google Play update channel unavailable"

        UpdateStatus.UpdateCancelled ->
            "Update available · installation postponed"

        is UpdateStatus.Error ->
            status.message ?: "Unable to check for updates"
    }
