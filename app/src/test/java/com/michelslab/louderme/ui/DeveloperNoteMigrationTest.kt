package com.michelslab.louderme.ui

import org.json.JSONArray
import org.json.JSONObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Test

class DeveloperNoteMigrationTest {
    private val pack = "com.michelslab.louderme.developer"
    private val baseline = OwnerNote(
        "id-1", "Home", "home.volume", "Media volume", "Verbatim owner report",
        "P1", "Open", 100L, 200L,
    )
    private fun file(vararg notes: OwnerNote): String {
        val contents = JSONArray()
        notes.forEach { contents.put(it.json()) }
        return JSONObject().put("schema", "michelslab-developer-note-backup-v1")
            .put("product", "LouderMe").put("edition", "Developer Edition")
            .put("platform", "Android").put("package", pack)
            .put("ownerAnnotations", contents).toString()
    }

    @Test fun acceptsRealDeveloperBackupAndPreservesExactVerbatimFields() {
        val imported = baseline.copy(
            id = "id-2", locator = "home.old-unresolved", status = "Resolved",
            comment = "Exact owner's 'quoted' input", createdUtcMs = 50L, updatedUtcMs = 300L,
        )
        val preview = parseDeveloperBackup(file(baseline, imported), pack, listOf(baseline))
        assertEquals(2, preview.incoming.size)
        assertEquals(1, preview.newCount)
        assertEquals(1, preview.exactDuplicates)
        assertEquals(0, preview.conflicts)
        assertEquals(1, preview.staleLocators)
        assertEquals(imported, preview.incoming.last())
    }

    @Test fun nonDestructiveImportNeverOverwritesUnlessExplicitlyApproved() {
        val newer = baseline.copy(comment = "Changed in backup", updatedUtcMs = 350)
        val other = baseline.copy(id = "id-3", comment = "Imported after signer change")
        val existingUnrelated = baseline.copy(id = "existing-only", comment = "Keep always")
        val existing = listOf(baseline, existingUnrelated)
        val incoming = listOf(newer, other)
        val result = mergeDeveloperNotes(existing, incoming, false)
        assertEquals(listOf(baseline, existingUnrelated, other), result)
        assertEquals("Changed in backup",
            mergeDeveloperNotes(existing, incoming, true).first().comment)
        assertEquals(existingUnrelated,
            mergeDeveloperNotes(existing, incoming, true)[1])
        // The previous list cannot be mutated while owner reviews a restore.
        assertEquals("Verbatim owner report", existing.first().comment)
    }

    @Test fun wrongApplicationIdentityIsRejected() {
        val raw = file(baseline).replace(pack, "com.michelslab.louderme")
        assertThrows(IllegalArgumentException::class.java) {
            parseDeveloperBackup(raw, pack, emptyList())
        }
    }

    @Test fun invalidSeverityStatusTimestampOrBlankNoteIsRejected() {
        for (bad in listOf(
            baseline.copy(status = "Arbitrary"),
            baseline.copy(severity = "P9"),
            baseline.copy(createdUtcMs = 300, updatedUtcMs = 200),
            baseline.copy(comment = ""),
            baseline.copy(screen = "Settings"),
        )) {
            assertThrows(IllegalArgumentException::class.java) {
                parseDeveloperBackup(file(bad), pack, emptyList())
            }
        }
    }

    @Test fun duplicatedIdsAreRejectedRatherThanSilentlyMerged() {
        assertThrows(IllegalArgumentException::class.java) {
            parseDeveloperBackup(file(baseline, baseline.copy(comment = "different")),
                pack, emptyList())
        }
    }

    @Test fun rejectsMissingOrInvalidOrOversizeFiles() {
        assertThrows(Exception::class.java) {
            parseDeveloperBackup("{", pack, emptyList())
        }
        assertThrows(IllegalArgumentException::class.java) {
            parseDeveloperBackup(file(baseline).replace(
                "michelslab-developer-note-backup-v1", "michelslab-unknown"),
                pack, emptyList())
        }
        assertThrows(IllegalArgumentException::class.java) {
            parseDeveloperBackup(" ".repeat(2_097_153), pack, emptyList())
        }
    }

    @Test fun explicitlyAllowsLegacyFullAuditExportOfSameDeveloperPackage() {
        // Existing full audit exports have the same product/package identity,
        // but no dedicated backup schema. Preserve those old note exports.
        val report = JSONObject(file(baseline))
        report.remove("schema")
        report.put("findings", JSONArray())
        assertEquals(1, parseDeveloperBackup(report.toString(), pack, emptyList()).newCount)
    }
}
