package com.michelslab.louderme.update

import org.json.JSONObject

data class ReleaseManifest(
    val versionName: String,
    val versionCode: Int,
    val apkUrl: String,
    val sha256: String,
    val releaseNotesUrl: String? = null,
) {
    companion object {
        fun parse(json: String): ReleaseManifest {
            val root = JSONObject(json)
            return ReleaseManifest(
                versionName = root.getString("versionName"),
                versionCode = root.getInt("versionCode"),
                apkUrl = root.getString("apkUrl"),
                sha256 = root.getString("sha256").lowercase(),
                releaseNotesUrl = root.optString("releaseNotesUrl")
                    .takeIf { it.isNotBlank() },
            )
        }
    }
}
