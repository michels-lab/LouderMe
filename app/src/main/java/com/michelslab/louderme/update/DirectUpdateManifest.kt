package com.michelslab.louderme.update

import org.json.JSONObject

data class DirectUpdateManifest(
    val versionCode: Long,
    val versionName: String,
    val apkUrl: String,
    val sha256: String,
    val releaseNotes: String,
) {
    companion object {
        fun fromJson(json: String): DirectUpdateManifest {
            val root = JSONObject(json)

            return DirectUpdateManifest(
                versionCode = root.getLong("versionCode"),
                versionName = root.getString("versionName"),
                apkUrl = root.optString("apkUrl"),
                sha256 = root.optString("sha256").lowercase(),
                releaseNotes = root.optString("releaseNotes"),
            )
        }
    }
}
