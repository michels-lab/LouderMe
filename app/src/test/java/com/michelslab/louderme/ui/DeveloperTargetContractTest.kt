package com.michelslab.louderme.ui

import com.michelslab.louderme.BuildConfig
import com.michelslab.louderme.audio.EqualizerPresets
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class DeveloperTargetContractTest {
    @Test fun exactLiveSectionsAndEqualizerTargetsAreRegistered() {
        val anchors = DeveloperTargets.known
        listOf("home.header", "home.volume", "home.boost", "home.equalizer",
            "home.diagnostics", "about.header", "about.profile", "about.updates")
            .forEach { assertTrue("Missing live anchor " + it, anchors.containsKey(it)) }
        assertEquals(7, anchors.keys.count { it.startsWith("home.eq-band-") })
        EqualizerPresets.labels.forEach { assertTrue(anchors.containsKey("home.eq-band-" + it)) }
        assertEquals(7, anchors.keys.count { it.matches(Regex("home\\.quick-\\d+")) })
    }

    @Test fun noteStatusFilterNeverMutatesOrDropsAuditExportAnnotations() {
        val notes = listOf(
            OwnerNote("1", "Home", "home.volume", "Media volume",
                "Slider doesn't respond on owner device", "P1", "Open", 101L, 102L),
            OwnerNote("2", "About", "about.header", "About",
                "Already repaired after QA", "P2", "Resolved", 201L, 202L),
            OwnerNote("3", "Home", "home.equalizer", "Equalizer",
                "Needs inspection on Bluetooth", "P0", "Investigating", 301L, 302L),
        )
        assertEquals(notes, visibleDeveloperNotes(notes, "All"))
        assertEquals(listOf(notes[0]), visibleDeveloperNotes(notes, "Open"))
        assertEquals(listOf(notes[1]), visibleDeveloperNotes(notes, "Resolved"))
        assertEquals(listOf(notes[2]), visibleDeveloperNotes(notes, "Investigating"))
        assertTrue(visibleDeveloperNotes(notes, "Unknown").isEmpty())
        assertEquals(3, notes.size)
        assertEquals("Slider doesn't respond on owner device", notes[0].comment)
        assertEquals(202L, notes[1].updatedUtcMs)
    }

    @Test fun developerDistributionNeverInheritsPublicPackage() {
        if (BuildConfig.DEVELOPER_EDITION)
            assertEquals("com.michelslab.louderme.developer", BuildConfig.APPLICATION_ID)
        else {
            assertFalse(BuildConfig.APPLICATION_ID.endsWith(".developer"))
            assertEquals("com.michelslab.louderme", BuildConfig.APPLICATION_ID)
        }
    }
}
