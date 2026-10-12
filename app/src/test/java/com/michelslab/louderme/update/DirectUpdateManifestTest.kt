package com.michelslab.louderme.update

import org.junit.Assert.assertEquals
import org.junit.Test

class DirectUpdateManifestTest {
    private val officialUrl =
        "https://github.com/michels-lab/LouderME-Releases/releases/download/" +
            "louderme-android-v0.1.11-desktop-v0.1.11/LouderMe-v0.1.11-sideload.apk"

    private fun trusted() = DirectUpdateManifest(
        versionCode = 12L,
        versionName = "0.1.11",
        apkUrl = officialUrl,
        sha256 = "a".repeat(64),
        releaseNotes = "Official channel",
    )

    @Test
    fun validOfficialPublisher() {
        trusted().requireTrustedPublisher()
        assertEquals("0.1.11", trusted().versionName)
    }

    @Test
    fun rejectsForeignOrMalformedReleaseSources() {
        val good = trusted()
        val invalid = listOf(
            good.copy(apkUrl = officialUrl.replace("LouderME-Releases", "michel-s-life-releases")),
            good.copy(apkUrl = officialUrl.replace("michels-lab", "realmichelduarte")),
            good.copy(apkUrl = officialUrl.replace("github.com", "evil.example")),
            good.copy(apkUrl = officialUrl.replace("https://", "http://")),
            good.copy(apkUrl = officialUrl + "?redirect=other-app"),
            good.copy(apkUrl = "https:opaque"),
            good.copy(sha256 = "not-a-sha"),
            good.copy(versionCode = -1),
            good.copy(versionName = "../other-app"),
        )
        invalid.forEachIndexed { index, entry ->
            var rejected = false
            try {
                entry.requireTrustedPublisher()
            } catch (_: IllegalArgumentException) {
                rejected = true
            }
            if (!rejected) {
                throw AssertionError("Untrusted update manifest accepted at case $index")
            }
        }
    }
}
