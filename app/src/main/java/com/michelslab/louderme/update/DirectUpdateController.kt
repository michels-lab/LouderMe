package com.michelslab.louderme.update

import android.app.Activity
import android.app.PendingIntent
import android.content.Intent
import android.content.pm.PackageInstaller
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import com.michelslab.louderme.BuildConfig
import java.io.File
import java.net.HttpURLConnection
import java.net.URL
import java.security.MessageDigest
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean

class DirectUpdateController(
    private val activity: Activity,
    private val onStatusChanged: (UpdateStatus) -> Unit,
) {
    private val mainHandler = Handler(
        Looper.getMainLooper()
    )
    private val executor =
        Executors.newSingleThreadExecutor()
    private val checkInFlight =
        AtomicBoolean(false)

    @Volatile
    private var pendingManifest:
        DirectUpdateManifest? = null

    fun checkForUpdates(
        allowAutomaticPrompt: Boolean,
    ) {
        if (!checkInFlight.compareAndSet(false, true)) {
            return
        }

        postStatus(UpdateStatus.Checking)

        executor.execute {
            try {
                val manifest = fetchManifest()

                if (
                    manifest.versionCode <=
                    BuildConfig.VERSION_CODE.toLong()
                ) {
                    clearStaleCandidate()
                    postStatus(UpdateStatus.UpToDate)
                    return@execute
                }

                pendingManifest = manifest

                postStatus(
                    UpdateStatus.UpdateAvailable(
                        versionName =
                            manifest.versionName,
                        channel =
                            "Michel's Lab Direct",
                    )
                )

                if (allowAutomaticPrompt) {
                    downloadCandidate(
                        manifest = manifest,
                        autoInstall = true,
                    )
                }
            } catch (error: Throwable) {
                postStatus(
                    UpdateStatus.Error(
                        error.message ?:
                            error::class.java.simpleName
                    )
                )
            } finally {
                checkInFlight.set(false)
            }
        }
    }

    fun installAvailableUpdate() {
        executor.execute {
            try {
                val cached = loadCachedManifest()

                if (
                    cached != null &&
                    cached.versionCode >
                    BuildConfig.VERSION_CODE.toLong() &&
                    cachedApk().exists()
                ) {
                    pendingManifest = cached
                    verifyAndInstall(
                        cached,
                        cachedApk(),
                    )
                    return@execute
                }

                val manifest = pendingManifest

                if (manifest != null) {
                    downloadCandidate(
                        manifest = manifest,
                        autoInstall = true,
                    )
                } else {
                    mainHandler.post {
                        checkForUpdates(
                            allowAutomaticPrompt = true
                        )
                    }
                }
            } catch (error: Throwable) {
                postStatus(
                    UpdateStatus.Error(
                        error.message ?:
                            error::class.java.simpleName
                    )
                )
            }
        }
    }

    fun resumeInterruptedUpdate() {
        val installPrefs =
            activity.getSharedPreferences(
                UpdateInstallReceiver.PREFS,
                Activity.MODE_PRIVATE,
            )

        val lastInstallResult =
            installPrefs.getString(
                UpdateInstallReceiver
                    .KEY_LAST_INSTALL_RESULT,
                null,
            )

        if (!lastInstallResult.isNullOrBlank()) {
            installPrefs
                .edit()
                .remove(
                    UpdateInstallReceiver
                        .KEY_LAST_INSTALL_RESULT
                )
                .apply()

            if (
                lastInstallResult.contains(
                    "success",
                    ignoreCase = true,
                )
            ) {
                postStatus(UpdateStatus.UpToDate)
            } else {
                postStatus(
                    UpdateStatus.Error(
                        lastInstallResult
                    )
                )
            }
        }

        executor.execute {
            val cached =
                loadCachedManifest() ?:
                return@execute

            if (
                cached.versionCode <=
                BuildConfig.VERSION_CODE.toLong() ||
                !cachedApk().exists()
            ) {
                clearStaleCandidate()
                return@execute
            }

            pendingManifest = cached

            if (
                Build.VERSION.SDK_INT < 26 ||
                activity.packageManager
                    .canRequestPackageInstalls()
            ) {
                verifyAndInstall(
                    cached,
                    cachedApk(),
                )
            } else {
                postStatus(
                    UpdateStatus
                        .InstallPermissionRequired(
                            cached.versionName
                        )
                )
            }
        }
    }

    fun shutdown() {
        executor.shutdownNow()
    }

    private fun fetchManifest():
        DirectUpdateManifest {
        val connection =
            (
                URL(
                    DirectUpdateConfig.MANIFEST_URL
                ).openConnection()
                    as HttpURLConnection
                ).apply {
                    requestMethod = "GET"
                    connectTimeout =
                        DirectUpdateConfig
                            .HTTP_CONNECT_TIMEOUT_MS
                    readTimeout =
                        DirectUpdateConfig
                            .HTTP_READ_TIMEOUT_MS
                    setRequestProperty(
                        "User-Agent",
                        "LouderMe/" +
                            BuildConfig.VERSION_NAME,
                    )
                }

        return try {
            val code = connection.responseCode

            if (code !in 200..299) {
                error(
                    "Update feed returned HTTP " +
                        code
                )
            }

            val body =
                connection.inputStream
                    .bufferedReader()
                    .use { it.readText() }

            DirectUpdateManifest.fromJson(body)
        } finally {
            connection.disconnect()
        }
    }

    private fun downloadCandidate(
        manifest: DirectUpdateManifest,
        autoInstall: Boolean,
    ) {
        if (
            manifest.apkUrl.isBlank() ||
            manifest.sha256.isBlank()
        ) {
            error(
                "Update feed is missing APK URL " +
                    "or checksum."
            )
        }

        val file = cachedApk()
        file.parentFile?.mkdirs()

        val connection =
            (
                URL(
                    manifest.apkUrl
                ).openConnection()
                    as HttpURLConnection
                ).apply {
                    requestMethod = "GET"
                    connectTimeout =
                        DirectUpdateConfig
                            .HTTP_CONNECT_TIMEOUT_MS
                    readTimeout =
                        DirectUpdateConfig
                            .HTTP_READ_TIMEOUT_MS
                    instanceFollowRedirects = true
                    setRequestProperty(
                        "User-Agent",
                        "LouderMe/" +
                            BuildConfig.VERSION_NAME,
                    )
                }

        try {
            val code = connection.responseCode

            if (code !in 200..299) {
                error(
                    "APK download returned HTTP " +
                        code
                )
            }

            val contentLength =
                connection.contentLengthLong
            var copied = 0L
            var lastProgress = -1

            connection.inputStream.use { input ->
                file.outputStream()
                    .buffered()
                    .use { output ->
                        val buffer =
                            ByteArray(
                                DEFAULT_BUFFER_SIZE
                            )

                        while (true) {
                            val read =
                                input.read(buffer)

                            if (read <= 0) {
                                break
                            }

                            output.write(
                                buffer,
                                0,
                                read,
                            )

                            copied += read

                            if (contentLength > 0L) {
                                val progress =
                                    (
                                        copied * 100L /
                                            contentLength
                                        )
                                        .toInt()
                                        .coerceIn(
                                            0,
                                            100,
                                        )

                                if (
                                    progress !=
                                    lastProgress
                                ) {
                                    lastProgress =
                                        progress

                                    postStatus(
                                        UpdateStatus
                                            .Downloading(
                                                versionName =
                                                    manifest
                                                        .versionName,
                                                progressPercent =
                                                    progress,
                                            )
                                    )
                                }
                            }
                        }
                    }
            }
        } finally {
            connection.disconnect()
        }

        verifyCandidate(
            manifest,
            file,
        )
        saveCachedManifest(manifest)

        postStatus(
            UpdateStatus.ReadyToInstall(
                manifest.versionName
            )
        )

        if (autoInstall) {
            installVerifiedCandidate(
                manifest,
                file,
            )
        }
    }

    private fun verifyAndInstall(
        manifest: DirectUpdateManifest,
        file: File,
    ) {
        verifyCandidate(
            manifest,
            file,
        )

        installVerifiedCandidate(
            manifest,
            file,
        )
    }

    private fun verifyCandidate(
        manifest: DirectUpdateManifest,
        file: File,
    ) {
        val actualSha = sha256(file)

        if (
            !actualSha.equals(
                manifest.sha256,
                ignoreCase = true,
            )
        ) {
            file.delete()
            error(
                "Downloaded APK checksum does not " +
                    "match the official feed."
            )
        }

        @Suppress("DEPRECATION")
        val archiveInfo =
            activity.packageManager
                .getPackageArchiveInfo(
                    file.absolutePath,
                    PackageManager
                        .GET_SIGNING_CERTIFICATES,
                )
                ?: error(
                    "Android could not inspect " +
                        "the downloaded APK."
                )

        if (
            archiveInfo.packageName !=
            activity.packageName
        ) {
            file.delete()
            error(
                "Downloaded APK package ID does " +
                    "not match LouderMe."
            )
        }

        if (
            archiveInfo.longVersionCode !=
            manifest.versionCode
        ) {
            file.delete()
            error(
                "Downloaded APK version does not " +
                    "match the update manifest."
            )
        }

        if (
            archiveInfo.longVersionCode <=
            BuildConfig.VERSION_CODE.toLong()
        ) {
            file.delete()
            error(
                "Downloaded APK is not newer than " +
                    "this installation."
            )
        }

        @Suppress("DEPRECATION")
        val ownInfo =
            activity.packageManager.getPackageInfo(
                activity.packageName,
                PackageManager
                    .GET_SIGNING_CERTIFICATES,
            )

        val ownSigners =
            signerFingerprints(
                ownInfo.signingInfo
            )

        val candidateSigners =
            signerFingerprints(
                archiveInfo.signingInfo
            )

        if (
            ownSigners
                .intersect(candidateSigners)
                .isEmpty()
        ) {
            file.delete()
            error(
                "Downloaded APK is not signed by " +
                    "this LouderMe release key."
            )
        }
    }

    private fun installVerifiedCandidate(
        manifest: DirectUpdateManifest,
        file: File,
    ) {
        if (
            Build.VERSION.SDK_INT >= 26 &&
            !activity.packageManager
                .canRequestPackageInstalls()
        ) {
            postStatus(
                UpdateStatus
                    .InstallPermissionRequired(
                        manifest.versionName
                    )
            )

            mainHandler.post {
                activity.startActivity(
                    Intent(
                        Settings
                            .ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                        Uri.parse(
                            "package:" +
                                activity.packageName
                        ),
                    )
                )
            }
            return
        }

        postStatus(
            UpdateStatus.Installing(
                manifest.versionName
            )
        )

        val packageInstaller =
            activity.packageManager
                .packageInstaller

        val params =
            PackageInstaller.SessionParams(
                PackageInstaller
                    .SessionParams
                    .MODE_FULL_INSTALL
            ).apply {
                setAppPackageName(
                    activity.packageName
                )
                setOriginatingUri(
                    Uri.parse(manifest.apkUrl)
                )

                if (
                    Build.VERSION.SDK_INT >= 33
                ) {
                    setPackageSource(
                        PackageInstaller
                            .PACKAGE_SOURCE_DOWNLOADED_FILE
                    )
                }
            }

        val sessionId =
            packageInstaller
                .createSession(params)

        packageInstaller
            .openSession(sessionId)
            .use { session ->
                session.openWrite(
                    "base.apk",
                    0,
                    file.length(),
                ).use { output ->
                    file.inputStream()
                        .use { input ->
                            input.copyTo(output)
                        }

                    session.fsync(output)
                }

                val callbackIntent =
                    Intent(
                        activity,
                        UpdateInstallReceiver::class.java,
                    ).putExtra(
                        "version_name",
                        manifest.versionName,
                    )

                val pendingFlags =
                    PendingIntent
                        .FLAG_UPDATE_CURRENT or
                        if (
                            Build.VERSION.SDK_INT >= 31
                        ) {
                            PendingIntent.FLAG_MUTABLE
                        } else {
                            0
                        }

                val statusReceiver =
                    PendingIntent.getBroadcast(
                        activity,
                        sessionId,
                        callbackIntent,
                        pendingFlags,
                    )

                session.commit(
                    statusReceiver.intentSender
                )
            }
    }

    private fun signerFingerprints(
        signingInfo:
            android.content.pm.SigningInfo?,
    ): Set<String> {
        if (signingInfo == null) {
            return emptySet()
        }

        val certificates =
            if (
                signingInfo.hasMultipleSigners()
            ) {
                signingInfo
                    .apkContentsSigners
            } else {
                signingInfo
                    .signingCertificateHistory
            }

        return certificates
            .map { certificate ->
                val digest =
                    MessageDigest
                        .getInstance("SHA-256")
                        .digest(
                            certificate
                                .toByteArray()
                        )

                digest.joinToString("") {
                    "%02x".format(it)
                }
            }
            .toSet()
    }

    private fun sha256(
        file: File,
    ): String {
        val digest =
            MessageDigest.getInstance(
                "SHA-256"
            )

        file.inputStream().use { input ->
            val buffer =
                ByteArray(
                    DEFAULT_BUFFER_SIZE
                )

            while (true) {
                val read =
                    input.read(buffer)

                if (read <= 0) {
                    break
                }

                digest.update(
                    buffer,
                    0,
                    read,
                )
            }
        }

        return digest.digest()
            .joinToString("") {
                "%02x".format(it)
            }
    }

    private fun cachedApk(): File =
        File(
            File(
                activity.filesDir,
                "updates",
            ),
            "louderme-update.apk",
        )

    private fun saveCachedManifest(
        manifest: DirectUpdateManifest,
    ) {
        activity
            .getSharedPreferences(
                PREFS,
                Activity.MODE_PRIVATE,
            )
            .edit()
            .putLong(
                KEY_VERSION_CODE,
                manifest.versionCode,
            )
            .putString(
                KEY_VERSION_NAME,
                manifest.versionName,
            )
            .putString(
                KEY_APK_URL,
                manifest.apkUrl,
            )
            .putString(
                KEY_SHA256,
                manifest.sha256,
            )
            .putString(
                KEY_RELEASE_NOTES,
                manifest.releaseNotes,
            )
            .apply()
    }

    private fun loadCachedManifest():
        DirectUpdateManifest? {
        val prefs =
            activity.getSharedPreferences(
                PREFS,
                Activity.MODE_PRIVATE,
            )

        val versionCode =
            prefs.getLong(
                KEY_VERSION_CODE,
                -1L,
            )

        if (versionCode < 0L) {
            return null
        }

        return DirectUpdateManifest(
            versionCode = versionCode,
            versionName =
                prefs.getString(
                    KEY_VERSION_NAME,
                    "",
                ).orEmpty(),
            apkUrl =
                prefs.getString(
                    KEY_APK_URL,
                    "",
                ).orEmpty(),
            sha256 =
                prefs.getString(
                    KEY_SHA256,
                    "",
                ).orEmpty(),
            releaseNotes =
                prefs.getString(
                    KEY_RELEASE_NOTES,
                    "",
                ).orEmpty(),
        )
    }

    private fun clearStaleCandidate() {
        cachedApk().delete()

        activity
            .getSharedPreferences(
                PREFS,
                Activity.MODE_PRIVATE,
            )
            .edit()
            .clear()
            .apply()

        pendingManifest = null
    }

    private fun postStatus(
        status: UpdateStatus,
    ) {
        mainHandler.post {
            onStatusChanged(status)
        }
    }

    companion object {
        private const val PREFS =
            "louderme_direct_update_cache"

        private const val KEY_VERSION_CODE =
            "version_code"
        private const val KEY_VERSION_NAME =
            "version_name"
        private const val KEY_APK_URL =
            "apk_url"
        private const val KEY_SHA256 =
            "sha256"
        private const val KEY_RELEASE_NOTES =
            "release_notes"
    }
}
