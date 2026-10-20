package com.michelslab.louderme.ui

import android.content.Intent
import android.widget.Toast
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.border
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.boundsInWindow
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInWindow
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.EqualizerUiState
import org.json.JSONArray

internal val LocalDeveloperNotes = compositionLocalOf<DeveloperNotesStore?> { null }

// Section bounds are measured from the actual rendered Compose controls.
// Selection happens in a full-screen overlay, so underlying controls NEVER
// receive the inspection tap; the smallest real anchored control wins.
@Composable
internal fun Modifier.developerAnchor(screen: String, locator: String, label: String): Modifier {
    val owner = LocalDeveloperNotes.current ?: return this
    val highlight = if (owner.focused == locator)
        Modifier.border(2.dp, Color(0xFFFFC45A), RoundedCornerShape(12.dp))
    else Modifier
    return this.then(highlight).onGloballyPositioned {
        owner.register(screen, locator, label, it.boundsInWindow())
    }
}

@Composable
internal fun DeveloperInspectorUi(
    owner: DeveloperNotesStore,
    audio: AudioEngineUiState,
    eq: EqualizerUiState,
    onNavigate: (String) -> Unit,
) {
    val context = LocalContext.current
    var audit by remember { mutableStateOf<JSONArray?>(null) }
    var pendingDelete by remember { mutableStateOf<OwnerNote?>(null) }
    var statusFilter by remember { mutableStateOf("All") }

    BackHandler(enabled = owner.inspect) { owner.inspect = false }

    var origin by remember { mutableStateOf(Offset.Zero) }
    Box(Modifier.fillMaxSize()) {
        if (owner.inspect) {
            Box(Modifier.fillMaxSize()
                .onGloballyPositioned { origin = it.positionInWindow() }
                .pointerInput(owner) { detectTapGestures { owner.pick(origin + it) } })
            Surface(
                modifier = Modifier.align(Alignment.TopCenter).padding(top = 6.dp),
                color = Color(0xFF23415A), shape = RoundedCornerShape(12.dp),
            ) {
                Text("INSPECT MODE · tap a real app section (Back cancels)",
                    modifier = Modifier.padding(9.dp), color = Color.White)
            }
        }
        Button(
            onClick = { owner.panel = true },
            modifier = Modifier.align(Alignment.BottomEnd)
                .windowInsetsPadding(WindowInsets.navigationBars)
                .padding(12.dp),
        ) { Text("DEV · Audit / Notes") }
    }

    if (owner.panel) {
        AlertDialog(
            onDismissRequest = { owner.panel = false },
            title = { Text("LouderMe · Developer Edition") },
            text = {
                Column(Modifier.heightIn(max = 400.dp).verticalScroll(rememberScrollState())) {
                    Text("Isolated Android installation; no external logs or automatic upload.")
                    if (owner.selectionHint != null) Text(owner.selectionHint ?: "")
                    Text("LouderMe has no public sign-in/account workflow; none is mocked.",
                        style = MaterialTheme.typography.bodySmall)
                    if (owner.error != null)
                        Text(owner.error ?: "", color = MaterialTheme.colorScheme.error)
                    Text("Saved section notes: " + owner.notes.size, fontWeight = FontWeight.Bold)
                    Row(Modifier.fillMaxWidth().horizontalScroll(rememberScrollState())) {
                        listOf("All", "Open", "Investigating", "Resolved").forEach { status ->
                            TextButton(onClick = { statusFilter = status }) {
                                Text(if (statusFilter == status) "✓ " + status else status)
                            }
                        }
                    }
                    val shown = visibleDeveloperNotes(owner.notes, statusFilter)
                    Text("Showing " + shown.size + " of " + owner.notes.size +
                        " notes · " + statusFilter,
                        style = MaterialTheme.typography.bodySmall)
                    shown.forEach { note ->
                        Column(Modifier.fillMaxWidth().padding(top = 10.dp)) {
                            Text("[" + note.severity + "] " + note.screen + " / " +
                                note.label + ": " + note.comment.take(80))
                            Row {
                                TextButton(onClick = { owner.editNote(note) }) { Text("Edit") }
                                TextButton(onClick = {
                                    owner.focused = note.locator
                                    owner.panel = false
                                    onNavigate(note.screen)
                                }) { Text("Locate") }
                                TextButton(onClick = { pendingDelete = note }) { Text("Delete") }
                            }
                        }
                    }
                    if (audit != null) {
                        Text("Read-only findings: " + audit!!.length(), fontWeight = FontWeight.Bold)
                        for (i in 0 until audit!!.length()) {
                            val finding = audit!!.getJSONObject(i)
                            Text(finding.getString("priority") + " · " +
                                finding.getString("code") + ": " + finding.getString("detail"))
                        }
                    }
                }
            },
            confirmButton = {
                Column {
                    TextButton(onClick = { audit = owner.findings(audio, eq) }) { Text("Run Audit") }
                    TextButton(onClick = { owner.startInspect() }, enabled = owner.error == null) {
                        Text("Select section / Add note")
                    }
                    Row {
                        TextButton(onClick = {
                            owner.exportJson = true
                            owner.export = owner.json(audio, eq)
                            owner.panel = false
                        }) { Text("Export JSON") }
                        TextButton(onClick = {
                            owner.exportJson = false
                            owner.export = owner.prompt(audio, eq)
                            owner.panel = false
                        }) { Text("AI repair prompt") }
                    }
                    TextButton(onClick = { owner.panel = false }) { Text("Close") }
                }
            }
        )
    }

    if (pendingDelete != null) {
        val note = pendingDelete!!
        AlertDialog(
            onDismissRequest = { pendingDelete = null },
            title = { Text("Delete selected note?") },
            text = { Text("This removes only this developer annotation, not public app data.") },
            confirmButton = {
                TextButton(onClick = { owner.delete(note); pendingDelete = null }) { Text("Delete") }
            },
            dismissButton = { TextButton(onClick = { pendingDelete = null }) { Text("Cancel") } },
        )
    }

    val target = owner.edit
    if (target != null) {
        var comment by remember(target) { mutableStateOf(target.existing?.comment ?: "") }
        var severity by remember(target) { mutableStateOf(target.existing?.severity ?: "P1") }
        var status by remember(target) { mutableStateOf(target.existing?.status ?: "Open") }
        AlertDialog(
            onDismissRequest = { owner.edit = null },
            title = { Text("Observation · " + target.label) },
            text = {
                Column(Modifier.verticalScroll(rememberScrollState())) {
                    Text(target.screen + " / " + target.locator,
                        style = MaterialTheme.typography.bodySmall)
                    OutlinedTextField(
                        value = comment,
                        onValueChange = { if (it.length <= 12000) comment = it },
                        label = { Text("Your comment, verbatim") },
                        minLines = 3, maxLines = 7,
                    )
                    Row {
                        listOf("P0", "P1", "P2").forEach { value ->
                            TextButton(onClick = { severity = value }) {
                                Text(if (severity == value) "✓ " + value else value)
                            }
                        }
                    }
                    Row {
                        listOf("Open", "Investigating", "Resolved").forEach { value ->
                            TextButton(onClick = { status = value }) {
                                Text(if (status == value) "✓ " + value else value)
                            }
                        }
                    }
                }
            },
            confirmButton = {
                TextButton(onClick = { owner.save(target, comment, severity, status) },
                    enabled = comment.isNotBlank()) { Text("Save note") }
            },
            dismissButton = { TextButton(onClick = { owner.edit = null }) { Text("Cancel") } },
        )
    }

    val rawExport = owner.export
    if (rawExport != null) {
        var reviewed by remember(rawExport) { mutableStateOf(rawExport) }
        AlertDialog(
            onDismissRequest = { owner.export = null },
            title = { Text("Review and redact export") },
            text = {
                Column {
                    Text("Comments may contain personal data. Review this report before choosing an external app.")
                    OutlinedTextField(value = reviewed, onValueChange = { reviewed = it },
                        modifier = Modifier.fillMaxWidth().heightIn(min = 160.dp, max = 320.dp))
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    val intent = Intent(Intent.ACTION_SEND).apply {
                        type = if (owner.exportJson) "application/json" else "text/plain"
                        putExtra(Intent.EXTRA_TEXT, reviewed)
                    }
                    try {
                        context.startActivity(Intent.createChooser(intent, "Export LouderMe report"))
                    } catch (_: Exception) {
                        Toast.makeText(context, "No compatible share target found.", Toast.LENGTH_LONG).show()
                    }
                    owner.export = null
                }) { Text("Share reviewed report") }
            },
            dismissButton = { TextButton(onClick = { owner.export = null }) { Text("Cancel") } },
        )
    }
}
