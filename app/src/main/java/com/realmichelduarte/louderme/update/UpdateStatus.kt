package com.realmichelduarte.louderme.update

sealed interface UpdateStatus {
    data object Checking : UpdateStatus
    data object UpToDate : UpdateStatus
    data object UpdateAvailable : UpdateStatus
    data object PlayStoreUnavailable : UpdateStatus
    data object UpdateCancelled : UpdateStatus
    data class Error(val message: String? = null) : UpdateStatus
}
