package com.michelslab.louderme.update

import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import androidx.core.content.FileProvider
import com.michelslab.louderme.BuildConfig
import java.io.File
import java.net.HttpURLConnection
import java.net.URL
import java.security.MessageDigest
import java.util.concurrent.Executors

class SideloadUpdateController(
    private val activity: Activity,
    private val onStatusChanged: (UpdateStatus) -> Unit,
) {
    private val executor = Executors.newSingleThreadExecutor()
    private val mainHandler = Handler(Looper.getMainLooper())

    private var pendingManifest: ReleaseManifest? = null
    private var pendingApk: File? = null
    private var automaticCheckDone = false

    fun checkForUpdates(automatic: Boolean) {
        if (automatic && automaticCheckDone) return
        if (automatic) automaticCheckDone = true

        emit(UpdateStatus.Checking)

        executor.execute {
            runCatching {
                val manifest = ReleaseManifest.parse(
                    downloadText(MANIFEST_URL)
                )

                if (manifest.versionCode > BuildConfig.VERSION_CODE) {
                    pendingManifest = manifest
                    emit(
                        UpdateStatus.UpdateAvailable(
                            versionName = manifest.versionName,
                            channel = "Michel's Lab Direct",
                        )
                    )
                } else {
                    pendingManifest = null
                    pendingApk = null
                    emit(UpdateStatus.UpToDate)
                }
            }.onFailure { error ->
                emit(
                    UpdateStatus.Error(
                        error.message ?: "Direct update check failed"
                    )
                )
            }
        }
    }

    fun installAvailableUpdate() {
        val manifest = pendingManifest
        if (manifest == null) {
            checkForUpdates(automatic = false)
            return
        }

        val existing = pendingApk
        if (existing != null && existing.isFile) {
            prepareInstall(manifest, existing)
            return
        }

        downloadUpdate(manifest)
    }

    fun resumePendingInstall() {
        val manifest = pendingManifest ?: return
        val apk = pendingApk ?: return

        if (
            Build.VERSION.SDK_INT >= 26 &&
            !activity.packageManager.canRequestPackageInstalls()
        ) {
            emit(
                UpdateStatus.InstallPermissionRequired(
                    manifest.versionName
                )
            )
            return
        }

        launchInstaller(manifest, apk)
    }

    fun shutdown() {
        executor.shutdownNow()
    }

    private fun downloadUpdate(manifest: ReleaseManifest) {
        emit(UpdateStatus.Downloading(manifest.versionName, 0))

        executor.execute {
            runCatching {
                val updateDir = File(
                    activity.externalCacheDir ?: activity.cacheDir,
                    "updates",
                ).apply { mkdirs() }

                val apk = File(
                    updateDir,
                    "LouderMe-v" + manifest.versionName + ".apk",
                )

                downloadFile(
                    url = manifest.apkUrl,
                    target = apk,
                    onProgress = { progress ->
                        emit(
                            UpdateStatus.Downloading(
                                manifest.versionName,
                                progress,
                            )
                        )
                    },
                )

                val actualSha = sha256(apk)
                if (!actualSha.equals(manifest.sha256, ignoreCase = true)) {
                    apk.delete()
                    error(
                        "Downloaded APK checksum mismatch. Expected " +
                            manifest.sha256 +
                            ", got " +
                            actualSha
                    )
                }

                verifyPackageIdentity(apk)

                pendingApk = apk
                emit(UpdateStatus.ReadyToInstall(manifest.versionName))

                mainHandler.post {
                    prepareInstall(manifest, apk)
                }
            }.onFailure { error ->
                pendingApk?.delete()
                pendingApk = null

                if (error is SignatureMismatchException) {
                    emit(
                        UpdateStatus.SignatureMismatch(
                            manifest.versionName
                        )
                    )
                } else {
                    emit(
                        UpdateStatus.Error(
                            error.message ?: "Update download failed"
                        )
                    )
                }
            }
        }
    }

    private fun prepareInstall(
        manifest: ReleaseManifest,
        apk: File,
    ) {
        if (
            Build.VERSION.SDK_INT >= 26 &&
            !activity.packageManager.canRequestPackageInstalls()
        ) {
            emit(
                UpdateStatus.InstallPermissionRequired(
                    manifest.versionName
                )
            )

            val settingsIntent = Intent(
                Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                Uri.parse("package:" + activity.packageName),
            )
            activity.startActivity(settingsIntent)
            return
        }

        launchInstaller(manifest, apk)
    }

    private fun launchInstaller(
        manifest: ReleaseManifest,
        apk: File,
    ) {
        val uri = FileProvider.getUriForFile(
            activity,
            activity.packageName + ".fileprovider",
            apk,
        )

        val intent = Intent(Intent.ACTION_VIEW)
            .setDataAndType(
                uri,
                "application/vnd.android.package-archive",
            )
            .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)

        emit(UpdateStatus.Installing(manifest.versionName))
        activity.startActivity(intent)
    }

    private fun verifyPackageIdentity(apk: File) {
        val flags = PackageManager.GET_SIGNING_CERTIFICATES

        val archive = activity.packageManager
            .getPackageArchiveInfo(apk.absolutePath, flags)
            ?: error("Downloaded file is not a valid Android package")

        if (archive.packageName != activity.packageName) {
            error(
                "Update package mismatch: " + archive.packageName
            )
        }

        val installed = activity.packageManager.getPackageInfo(
            activity.packageName,
            flags,
        )

        val archiveDigests = signingDigests(
            archive.signingInfo?.apkContentsSigners.orEmpty()
        )
        val installedDigests = signingDigests(
            installed.signingInfo?.apkContentsSigners.orEmpty()
        )

        if (
            archiveDigests.isEmpty() ||
            installedDigests.isEmpty() ||
            archiveDigests.intersect(installedDigests).isEmpty()
        ) {
            throw SignatureMismatchException()
        }
    }

    private fun signingDigests(
        signatures: Array<android.content.pm.Signature>,
    ): Set<String> {
        return signatures.map { signature ->
            val digest = MessageDigest.getInstance("SHA-256")
                .digest(signature.toByteArray())

            digest.joinToString("") { byte ->
                "%02x".format(byte)
            }
        }.toSet()
    }

    private fun downloadText(url: String): String {
        val connection = openConnection(url)
        return try {
            connection.inputStream.bufferedReader().use {
                it.readText()
            }
        } finally {
            connection.disconnect()
        }
    }

    private fun downloadFile(
        url: String,
        target: File,
        onProgress: (Int) -> Unit,
    ) {
        val connection = openConnection(url)

        try {
            val total = connection.contentLengthLong
            connection.inputStream.use { input ->
                target.outputStream().use { output ->
                    val buffer = ByteArray(DEFAULT_BUFFER_SIZE)
                    var copied = 0L
                    var lastProgress = -1

                    while (true) {
                        val read = input.read(buffer)
                        if (read < 0) break

                        output.write(buffer, 0, read)
                        copied += read

                        if (total > 0) {
                            val progress =
                                ((copied * 100L) / total)
                                    .toInt()
                                    .coerceIn(0, 100)

                            if (progress != lastProgress) {
                                lastProgress = progress
                                onProgress(progress)
                            }
                        }
                    }
                }
            }

            onProgress(100)
        } finally {
            connection.disconnect()
        }
    }

    private fun openConnection(url: String): HttpURLConnection {
        return (URL(url).openConnection() as HttpURLConnection)
            .apply {
                connectTimeout = 15_000
                readTimeout = 45_000
                instanceFollowRedirects = true
                requestMethod = "GET"
                setRequestProperty(
                    "User-Agent",
                    "LouderMe/" + BuildConfig.VERSION_NAME,
                )

                connect()

                if (responseCode !in 200..299) {
                    error(
                        "Update server returned HTTP " + responseCode
                    )
                }
            }
    }

    private fun sha256(file: File): String {
        val digest = MessageDigest.getInstance("SHA-256")

        file.inputStream().use { input ->
            val buffer = ByteArray(DEFAULT_BUFFER_SIZE)

            while (true) {
                val read = input.read(buffer)
                if (read < 0) break
                digest.update(buffer, 0, read)
            }
        }

        return digest.digest().joinToString("") { byte ->
            "%02x".format(byte)
        }
    }

    private fun emit(status: UpdateStatus) {
        mainHandler.post {
            onStatusChanged(status)
        }
    }

    private class SignatureMismatchException : Exception()

    companion object {
        const val MANIFEST_URL =
            "https://raw.githubusercontent.com/realmichelduarte/" +
                "michel-s-life-releases/main/louderme/latest.json"
    }
}
