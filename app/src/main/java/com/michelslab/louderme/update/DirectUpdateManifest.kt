package com.michelslab.louderme.update

import org.json.JSONObject
import java.net.URI

data class DirectUpdateManifest(
    val versionCode: Long,
    val versionName: String,
    val apkUrl: String,
    val sha256: String,
    val releaseNotes: String,
) {
    // A matching hash/certificate is necessary, but not sufficient: the
    // manifest must identify LouderMe's own dedicated release repository.
    // Check cached manifests too, not just newly downloaded JSON.
    fun requireTrustedPublisher() {
        val uri = try {
            URI(apkUrl)
        } catch (_: Exception) {
            null
        }
        require(
            uri != null &&
                uri.scheme.equals("https", ignoreCase = true) &&
                uri.host?.equals("github.com", ignoreCase = true) == true &&
                uri.rawPath.startsWith(
                    "/michels-lab/LouderME-Releases/releases/download/",
                ) &&
                uri.port == -1 &&
                uri.userInfo == null &&
                uri.rawQuery == null &&
                uri.rawFragment == null
        ) {
            "LouderMe APK URL is not from the official release repository."
        }
        require(versionCode > 0 && versionName.matches(
            Regex("[0-9]+\\.[0-9]+\\.[0-9]+"),
        )) {
            "Invalid LouderMe release version."
        }
        require(sha256.matches(Regex("[0-9a-fA-F]{64}"))) {
            "Invalid LouderMe APK SHA-256."
        }
    }

    companion object {
        fun fromJson(json: String): DirectUpdateManifest {
            val root = JSONObject(json)
            require(
                root.optInt("schema", -1) == 1 &&
                    root.optString("appId") == "com.michelslab.louderme" &&
                    root.optString("channel") == "sideload"
            ) {
                "Update feed does not identify the LouderMe sideload app."
            }
            return DirectUpdateManifest(
                versionCode = root.getLong("versionCode"),
                versionName = root.getString("versionName"),
                apkUrl = root.optString("apkUrl"),
                sha256 = root.optString("sha256").lowercase(),
                releaseNotes = root.optString("releaseNotes"),
            ).also { it.requireTrustedPublisher() }
        }
    }
}
