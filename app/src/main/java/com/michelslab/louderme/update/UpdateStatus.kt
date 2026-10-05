package com.michelslab.louderme.update

sealed interface UpdateStatus {
    data object Checking : UpdateStatus
    data object UpToDate : UpdateStatus

    data class UpdateAvailable(
        val versionName: String? = null,
        val channel: String? = null,
    ) : UpdateStatus

    data class Downloading(
        val versionName: String,
        val progressPercent: Int,
    ) : UpdateStatus

    data class ReadyToInstall(
        val versionName: String,
    ) : UpdateStatus

    data class InstallPermissionRequired(
        val versionName: String,
    ) : UpdateStatus

    data class SignatureMismatch(
        val versionName: String,
    ) : UpdateStatus

    data class Installing(
        val versionName: String,
    ) : UpdateStatus

    data object PlayStoreUnavailable : UpdateStatus
    data object UpdateCancelled : UpdateStatus
    data class Error(val message: String? = null) : UpdateStatus
}
