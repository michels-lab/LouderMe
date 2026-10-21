package com.michelslab.louderme.ui

import android.content.Context
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import com.michelslab.louderme.BuildConfig
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.EqualizerUiState
import com.michelslab.louderme.audio.EqualizerPresets
import org.json.JSONArray
import org.json.JSONObject
import java.util.UUID

internal data class OwnerNote(
    val id: String, val screen: String, val locator: String, val label: String,
    val comment: String, val severity: String, val status: String,
    val createdUtcMs: Long, val updatedUtcMs: Long,
) {
    fun json(): JSONObject = JSONObject()
        .put("id", id).put("screen", screen).put("locator", locator)
        .put("label", label).put("comment", comment)
        .put("severity", severity).put("status", status)
        .put("createdUtcMs", createdUtcMs).put("updatedUtcMs", updatedUtcMs)
}

// Filtering changes only the inspector presentation; audit and exports always
// retain every saved annotation, including resolved owner observations.
internal fun visibleDeveloperNotes(notes: List<OwnerNote>, statusFilter: String): List<OwnerNote> =
    if (statusFilter == "All") notes else notes.filter { it.status == statusFilter }

// Merge never deletes a note absent from a backup. Owner must deliberately
// choose overwrite when the same stable note ID has different content.
internal fun mergeDeveloperNotes(
    existing: List<OwnerNote>, incoming: List<OwnerNote>, replaceConflicts: Boolean,
): List<OwnerNote> {
    val incomingById = incoming.associateBy { it.id }
    val currentById = existing.associateBy { it.id }
    val kept = existing.map { old ->
        val replacement = incomingById[old.id]
        if (replaceConflicts && replacement != null) replacement else old
    }
    return kept + incoming.filter { it.id !in currentById }
}

internal data class DeveloperImportPreview(
    val incoming: List<OwnerNote>,
    val newCount: Int,
    val exactDuplicates: Int,
    val conflicts: Int,
    val staleLocators: Int,
)

internal data class TargetSelection(
    val screen: String, val locator: String, val label: String,
    val existing: OwnerNote? = null,
)

internal object DeveloperTargets {
    val known = mapOf(
        "home.header" to "Main header",
        "home.updates" to "Update status",
        "home.volume" to "Media volume",
        "home.boost" to "Global Boost",
        "home.quick-levels" to "Quick boost levels",
        "home.fine-tune" to "Manual gain",
        "home.equalizer" to "Seven-band equalizer",
        "home.diagnostics" to "Engine diagnostics",
        "about.header" to "About navigation",
        "about.profile" to "Developer and studio",
        "about.updates" to "About updates",
    ) + listOf(100, 125, 150, 175, 200, 225, 250)
        .associate { ("home.quick-" + it) to ("Boost " + it + "%") } +
        EqualizerPresets.labels.associate { ("home.eq-band-" + it) to (it + " Hz gain") }
}

// All SharedPreferences are isolated by the distinct .developer Android package.
// Never read public LouderMe files, credentials or user account state.
internal data class LiveTarget(val screen: String, val locator: String, val label: String, val bounds: Rect)

internal class DeveloperNotesStore(context: Context) {
    private val prefs = context.applicationContext.getSharedPreferences(
        "louderme_developer_annotations_v1", Context.MODE_PRIVATE)
    var notes by mutableStateOf<List<OwnerNote>>(emptyList())
        private set
    var error by mutableStateOf<String?>(null)
        private set
    var inspect by mutableStateOf(false)
    var panel by mutableStateOf(false)
    var edit by mutableStateOf<TargetSelection?>(null)
    var focused by mutableStateOf<String?>(null)
    var export by mutableStateOf<String?>(null)
    var exportJson by mutableStateOf(false)
    var currentScreen: String = "Home"
        private set
    var selectionHint by mutableStateOf<String?>(null)
        private set
    private val visibleBounds = mutableMapOf<String, LiveTarget>()

    init {
        check(BuildConfig.DEVELOPER_EDITION) { "Developer notes must not load in the public variant." }
        val raw = prefs.getString("notes", null)
        if (raw != null) {
            try {
                val array = JSONArray(raw)
                val recovered = mutableListOf<OwnerNote>()
                for (i in 0 until array.length()) {
                    val n = array.getJSONObject(i)
                    recovered += OwnerNote(n.getString("id"), n.getString("screen"),
                        n.getString("locator"), n.getString("label"), n.getString("comment"),
                        n.getString("severity"), n.getString("status"),
                        n.getLong("createdUtcMs"), n.getLong("updatedUtcMs"))
                }
                notes = recovered
            } catch (_: Exception) {
                error = "Saved annotations are unreadable. Original data is preserved; editing disabled."
            }
        }
    }

    // Reversible owner-controlled export for migration from developerDebug to
    // a permanently signed Developer APK. No Android filesystem permission,
    // online storage, diagnostics, or production app data is read here.
    fun backupJson(): String {
        val data = JSONArray()
        notes.forEach { data.put(it.json()) }
        return JSONObject()
            .put("schema", "michelslab-developer-note-backup-v1")
            .put("product", "LouderMe")
            .put("edition", "Developer Edition")
            .put("platform", "Android")
            .put("package", BuildConfig.APPLICATION_ID)
            .put("exportedUtcMs", System.currentTimeMillis())
            .put("ownerAnnotations", data).toString(2)
    }

    fun previewImport(raw: String): DeveloperImportPreview {
        check(error == null) { "Existing developer notes cannot be safely read. Import disabled." }
        require(raw.toByteArray(Charsets.UTF_8).size <= 2_097_152) {
            "Backup is larger than the 2 MB import limit."
        }
        val source = JSONObject(raw)
        require(source.getString("product") == "LouderMe" &&
            source.getString("edition") == "Developer Edition" &&
            source.getString("platform") == "Android" &&
            source.getString("package") == BuildConfig.APPLICATION_ID) {
            "Backup must be a LouderMe Android Developer Edition file."
        }
        if (source.has("schema")) {
            require(source.getString("schema") == "michelslab-developer-note-backup-v1") {
                "Unsupported Developer backup format."
            }
        }
        val array = source.getJSONArray("ownerAnnotations")
        require(array.length() in 1..250) { "Backup must contain 1–250 owner notes." }
        val incoming = mutableListOf<OwnerNote>()
        val ids = mutableSetOf<String>()
        for (i in 0 until array.length()) {
            val item = array.getJSONObject(i)
            val id = item.getString("id")
            val screen = item.getString("screen")
            val locator = item.getString("locator")
            val label = item.getString("label")
            val comment = item.getString("comment")
            val severity = item.getString("severity")
            val status = item.getString("status")
            val created = item.getLong("createdUtcMs")
            val updated = item.getLong("updatedUtcMs")
            require(id.length in 1..120 && ids.add(id)) { "Duplicate or invalid note identifier." }
            require(screen == "Home" || screen == "About") { "Unknown note screen." }
            require(locator.length in 1..160 && locator.all {
                it.isLetterOrDigit() || it == '.' || it == '_' || it == '-'
            }) { "Invalid saved section locator." }
            require(label.length in 1..200 && label.isNotBlank()) { "Invalid section label." }
            require(comment.isNotBlank() && comment.length <= 12000) {
                "Saved owner comment exceeds the permitted length or is blank."
            }
            require(severity in setOf("P0", "P1", "P2") &&
                status in setOf("Open", "Investigating", "Resolved")) {
                "Unsupported annotation severity or status."
            }
            require(created >= 0L && updated >= created) { "Invalid annotation timestamps." }
            incoming += OwnerNote(id, screen, locator, label, comment,
                severity, status, created, updated)
        }
        val currentById = notes.associateBy { it.id }
        return DeveloperImportPreview(
            incoming = incoming,
            newCount = incoming.count { it.id !in currentById },
            exactDuplicates = incoming.count { currentById[it.id] == it },
            conflicts = incoming.count {
                val old = currentById[it.id]
                old != null && old != it
            },
            staleLocators = incoming.count { it.locator !in DeveloperTargets.known },
        )
    }

    fun applyImport(preview: DeveloperImportPreview, replaceConflicts: Boolean): Boolean {
        if (error != null) return false
        persist(mergeDeveloperNotes(notes, preview.incoming, replaceConflicts))
        return error == null
    }

    fun activateScreen(screen: String) {
        if (currentScreen != screen) {
            currentScreen = screen
            visibleBounds.clear()
            // Preserve Locate's requested anchor across Home/About navigation.
            // The view highlights only the exact matching live locator, never a substitute.
        }
    }

    fun register(screen: String, locator: String, label: String, bounds: Rect) {
        if (screen == currentScreen && DeveloperTargets.known.containsKey(locator))
            visibleBounds[locator] = LiveTarget(screen, locator, label, bounds)
    }

    fun pick(point: Offset) {
        if (!inspect) return
        // Deepest/smallest live target wins, not the parent card: the overlay
        // swallows this tap BEFORE the real audio control can activate.
        val hit = visibleBounds.values.filter {
            it.screen == currentScreen && it.bounds.contains(point)
        }.minByOrNull { it.bounds.width * it.bounds.height }
        if (hit == null) {
            selectionHint = "Tap directly on a visible, marked LouderMe section or control."
            return
        }
        selectionHint = null
        select(hit.screen, hit.locator, hit.label)
    }

    fun startInspect() {
        if (error != null) return
        panel = false
        focused = null
        inspect = true
        selectionHint = null
    }

    fun select(screen: String, locator: String, label: String) {
        if (!inspect || !DeveloperTargets.known.containsKey(locator)) return
        inspect = false
        edit = TargetSelection(screen, locator, label)
    }

    fun editNote(note: OwnerNote) {
        panel = false
        edit = TargetSelection(note.screen, note.locator, note.label, note)
    }

    fun save(target: TargetSelection, comment: String, severity: String, status: String) {
        if (error != null || comment.isBlank()) return
        val now = System.currentTimeMillis()
        val old = target.existing
        val item = OwnerNote(old?.id ?: UUID.randomUUID().toString(), target.screen,
            target.locator, target.label, comment, severity, status, old?.createdUtcMs ?: now, now)
        persist(notes.filterNot { it.id == item.id } + item)
        edit = null
    }

    fun delete(note: OwnerNote) {
        if (error == null) persist(notes.filterNot { it.id == note.id })
    }

    private fun persist(next: List<OwnerNote>) {
        val json = JSONArray()
        next.forEach { json.put(it.json()) }
        if (!prefs.edit().putString("notes", json.toString()).commit()) {
            error = "Local developer storage failed. Original notes remain unchanged."
            return
        }
        notes = next
    }

    fun findings(audio: AudioEngineUiState, eq: EqualizerUiState): JSONArray {
        // Read-only checks. Neither this code nor its export plays/records sound.
        val result = JSONArray()
        fun finding(code: String, priority: String, detail: String) {
            result.put(JSONObject().put("code", code).put("priority", priority)
                .put("detail", detail).put("evidence", "local state or declared Compose anchor"))
        }
        if (eq.gainsDb.size != 7) finding("EQ_BAND_COUNT", "P1", "Expected seven bands.")
        if (audio.percent !in 100..250) finding("BOOST_OUT_OF_RANGE", "P1", "Expected target 100-250%.")
        if (error != null) finding("ANNOTATION_STORAGE_ERROR", "P1", "Notes cannot be loaded/written.")
        notes.filter { !DeveloperTargets.known.containsKey(it.locator) }.forEach {
            finding("UNRESOLVED_SECTION", "P2", "Note " + it.id + " has an outdated locator.")
        }
        finding("AUDIO_HARDWARE_UNTESTED", "P2",
            "A passing app audit is not external-player audible gain, clipping or SPL proof.")
        return result
    }

    fun json(audio: AudioEngineUiState, eq: EqualizerUiState): String {
        val saved = JSONArray()
        notes.forEach { saved.put(it.json()) }
        return JSONObject().put("product", "LouderMe")
            .put("edition", "Developer Edition")
            .put("platform", "Android")
            .put("package", BuildConfig.APPLICATION_ID)
            .put("version", BuildConfig.VERSION_NAME)
            .put("capturedUtcMs", System.currentTimeMillis())
            .put("evidence", "read-only local state and instrumented UI sections; sound unverified")
            .put("findings", findings(audio, eq))
            .put("ownerAnnotations", saved).toString(2)
    }

    fun prompt(audio: AudioEngineUiState, eq: EqualizerUiState): String = buildString {
        appendLine("LouderMe Android Developer Edition -- AI repair prompt")
        appendLine("Version: " + BuildConfig.VERSION_NAME + "; package: " + BuildConfig.APPLICATION_ID)
        appendLine("Do not assume audio changes were heard or that owner notes are verified defects.")
        val all = findings(audio, eq)
        for (i in 0 until all.length()) {
            val item = all.getJSONObject(i)
            appendLine("[" + item.getString("priority") + "] " +
                item.getString("code") + " -- " + item.getString("detail"))
        }
        appendLine("OWNER COMMENTS: verbatim and section-anchored; not automated findings")
        notes.forEach { note ->
            appendLine("Note " + note.id + ": " + note.severity + " / " + note.status)
            appendLine("Screen " + note.screen + " / locator " + note.locator + " / " + note.label)
            appendLine("Created UTC millis " + note.createdUtcMs + ", updated " + note.updatedUtcMs)
            appendLine(note.comment)
        }
        appendLine("Reproduce each observation, preserve settings, isolate test editions, add regression coverage.")
    }
}
