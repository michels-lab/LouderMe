package com.michelslab.louderme.update

object DirectUpdateConfig {
    const val MANIFEST_URL =
        "https://raw.githubusercontent.com/michels-lab/LouderMe-releases/main/louderme/latest.json"

    // Updater compatibility: previously installed clients still query this
    // legacy feed. Retain as read-only network fallback during migration.
    const val LEGACY_MANIFEST_URL =
        "https://raw.githubusercontent.com/michels-lab/michel-s-life-releases/main/louderme/latest.json"

    const val HTTP_CONNECT_TIMEOUT_MS = 12_000
    const val HTTP_READ_TIMEOUT_MS = 30_000
}
