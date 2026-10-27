package com.michelslab.louderme.ui

import androidx.compose.ui.geometry.Rect
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class DeveloperNavigationEvidenceTest {
    private val targets = mapOf("home.header" to "Home", "home.volume" to "Volume",
        "about.header" to "About", "about.profile" to "Author")
    private val bounds = Rect(10f, 20f, 110f, 90f)

    @Test fun pendingIsNotConfusedWithVerifiedScreen() {
        val pending = DeveloperNavigationEvidence.request("About", "about.profile", targets, 100L)!!
        assertEquals("Waiting for requested screen",
            DeveloperNavigationEvidence.status(pending, "Home", setOf("home.header")))
        assertEquals("Waiting for actual screen layout",
            DeveloperNavigationEvidence.status(pending, "About", emptySet()))
        assertTrue(DeveloperNavigationEvidence.status(pending, "About",
            setOf("about.header")).contains("not a verified failure"))
        val unrelated = DeveloperNavigationEvidence.confirm(pending, "About", "about.header",
            bounds, 200L)
        assertEquals(pending, unrelated)
        val confirmed = DeveloperNavigationEvidence.confirm(pending, "About", "about.profile",
            bounds, 210L)!!
        assertEquals(210L, confirmed.verifiedUtcMs)
        assertEquals("Verified on the live rendered control",
            DeveloperNavigationEvidence.status(confirmed, "About", emptySet()))
    }

    @Test fun cannotVerifyGhostOffscreenOrWrongSurfaceCoordinates() {
        val pending = DeveloperNavigationEvidence.request("Home", "home.volume", targets, 100L)!!
        listOf(
            Rect(0f, 0f, 0f, 10f), Rect(10f, 10f, 5f, 50f),
            Rect(Float.NaN, 0f, 20f, 40f), Rect(1f, 1f, Float.POSITIVE_INFINITY, 40f),
            Rect(0f, 0f, 10f, Float.NaN),
        ).forEach { invalid ->
            assertFalse(DeveloperNavigationEvidence.validBounds(invalid))
            assertEquals(pending,
                DeveloperNavigationEvidence.confirm(pending, "Home", "home.volume", invalid, 105L))
        }
        assertEquals(pending,
            DeveloperNavigationEvidence.confirm(pending, "About", "home.volume", bounds, 110L))
        assertEquals(pending,
            DeveloperNavigationEvidence.confirm(pending, "Home", "about.profile", bounds, 110L))
        assertEquals(pending,
            DeveloperNavigationEvidence.confirm(pending, "Home", "home.volume", bounds, 99L))
    }

    @Test fun staleLocatorOrMismatchedScreenCannotStartNavigation() {
        assertNull(DeveloperNavigationEvidence.request("About", "home.volume", targets, 100L))
        assertNull(DeveloperNavigationEvidence.request("Home", "missing.target", targets, 100L))
        assertNull(DeveloperNavigationEvidence.request("Home", "home.volume", targets, 0L))
        assertNull(DeveloperNavigationEvidence.request("Settings", "home.volume", targets, 100L))
        assertTrue(DeveloperNavigationEvidence.validBounds(bounds))
        assertEquals("No navigation requested",
            DeveloperNavigationEvidence.status(null, "Home", emptySet()))
    }
}
