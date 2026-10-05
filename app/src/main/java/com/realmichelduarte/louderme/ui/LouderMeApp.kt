package com.realmichelduarte.louderme.ui

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.realmichelduarte.louderme.BuildConfig

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LouderMeApp() {
    var showAbout by remember { mutableStateOf(false) }

    MaterialTheme {
        if (showAbout) {
            AboutScreen(onBack = { showAbout = false })
        } else {
            HomeScreen(onAbout = { showAbout = true })
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun HomeScreen(onAbout: () -> Unit) {
    val levels = listOf(100, 125, 150, 175, 200)
    var selected by remember { mutableIntStateOf(100) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("LouderMe") },
                actions = { TextButton(onClick = onAbout) { Text("About") } }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier.padding(padding).padding(20.dp).fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(20.dp)
        ) {
            Text("Global Boost", style = MaterialTheme.typography.headlineMedium)
            Text("Engine not implemented yet", color = MaterialTheme.colorScheme.error)

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                levels.forEach { level ->
                    FilterChip(
                        selected = selected == level,
                        onClick = { selected = level },
                        label = { Text("$level%") }
                    )
                }
            }

            Text("Selected: $selected%")
            Button(onClick = { }) { Text("Mixer / EQ") }
            OutlinedButton(onClick = { }) { Text("Smart Boost · Beta") }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun AboutScreen(onBack: () -> Unit) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("About") },
                navigationIcon = { TextButton(onClick = onBack) { Text("Back") } }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier.padding(padding).padding(24.dp).fillMaxSize(),
            horizontalAlignment = Alignment.Start,
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text("LouderMe", style = MaterialTheme.typography.headlineLarge)
            Text("Make everything louder.", style = MaterialTheme.typography.titleMedium)
            Text("Boost low-volume audio with fast controls, mixer presets, and optional experimental smart processing.")
            HorizontalDivider()
            Text("Version ${BuildConfig.VERSION_NAME}")
            Text("Developer: Michel Armando Duarte Flores")
            Text("© 2026 Michel Armando Duarte Flores")
            Text("License: Proprietary · All Rights Reserved")
        }
    }
}
