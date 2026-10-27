package com.michelslab.louderme.ui

import androidx.compose.ui.geometry.Rect

/**
 * In-session evidence, never a claim of a successful navigation merely
 * because a note's stored screen/locator was requested.
 */
internal data class DeveloperLocateEvidence(
    val screen: String,
    val locator: String,
    val requestedUtcMs: Long,
    val verifiedUtcMs: Long? = null,
)

internal object DeveloperNavigationEvidence {
    fun validBounds(bounds: Rect): Boolean =
        bounds.left.isFinite() && bounds.top.isFinite() &&
            bounds.right.isFinite() && bounds.bottom.isFinite() &&
            bounds.width > 0f && bounds.height > 0f

    fun intersectsViewport(bounds: Rect, viewport: Rect?): Boolean =
        viewport != null && validBounds(bounds) && validBounds(viewport) &&
            bounds.left < viewport.right && bounds.right > viewport.left &&
            bounds.top < viewport.bottom && bounds.bottom > viewport.top

    fun request(
        screen: String, locator: String, known: Map<String, String>, timeUtcMs: Long,
    ): DeveloperLocateEvidence? {
        val validScreen = (screen == "Home" && locator.startsWith("home.")) ||
            (screen == "About" && locator.startsWith("about."))
        return if (validScreen && known.containsKey(locator) && timeUtcMs > 0L)
            DeveloperLocateEvidence(screen, locator, timeUtcMs) else null
    }

    fun confirm(
        request: DeveloperLocateEvidence?, screen: String, locator: String,
        bounds: Rect, viewport: Rect?, timeUtcMs: Long,
    ): DeveloperLocateEvidence? {
        if (request == null || request.verifiedUtcMs != null ||
            request.screen != screen || request.locator != locator ||
            timeUtcMs < request.requestedUtcMs || !intersectsViewport(bounds, viewport))
            return request
        return request.copy(verifiedUtcMs = timeUtcMs)
    }

    fun status(
        request: DeveloperLocateEvidence?, currentScreen: String,
        renderedLocators: Set<String>,
    ): String {
        if (request == null) return "No navigation requested"
        if (request.verifiedUtcMs != null) return "Verified on-screen at recorded UTC time"
        if (request.screen != currentScreen) return "Waiting for requested screen"
        val root = if (currentScreen == "About") "about.header" else "home.header"
        if (!renderedLocators.contains(root)) return "Waiting for actual screen layout"
        return "Target not yet observed in mounted screen — not a verified failure"
    }
}
