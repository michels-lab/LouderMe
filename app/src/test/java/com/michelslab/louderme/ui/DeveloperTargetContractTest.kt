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

    @Test fun developerDistributionNeverInheritsPublicPackage() {
        if (BuildConfig.DEVELOPER_EDITION)
            assertEquals("com.michelslab.louderme.developer", BuildConfig.APPLICATION_ID)
        else {
            assertFalse(BuildConfig.APPLICATION_ID.endsWith(".developer"))
            assertEquals("com.michelslab.louderme", BuildConfig.APPLICATION_ID)
        }
    }
}
