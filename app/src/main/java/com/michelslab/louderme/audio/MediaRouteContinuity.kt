package com.michelslab.louderme.audio

/**
 * Ephemeral identities of the devices Android predicts will carry media.
 *
 * The IDs are compared in RAM only: they are not saved in SharedPreferences,
 * written to diagnostics, sent through broadcasts or displayed in the UI.
 */
internal data class MediaRouteContinuity(
    val label: String,
    val deviceKeys: List<Pair<Int, Int>>,
) {
    companion object {
        fun from(label: String, devices: Collection<Pair<Int, Int>>): MediaRouteContinuity =
            MediaRouteContinuity(
                label = label,
                deviceKeys = devices.distinct().sortedWith(
                    compareBy<Pair<Int, Int>>({ it.first }, { it.second }),
                ),
            )
    }
}

internal object MediaRouteRecoveryPolicy {
    fun hasChanged(previous: MediaRouteContinuity?, current: MediaRouteContinuity): Boolean =
        previous != null && previous != current

    fun deferTransientEmpty(
        previous: MediaRouteContinuity?,
        current: MediaRouteContinuity,
        attempts: Int,
    ): Boolean =
        previous != null && previous.deviceKeys.isNotEmpty() &&
            current.deviceKeys.isEmpty() && attempts < 1

    fun mustReattachBoost(running: Boolean, engineExists: Boolean, effectOwned: Boolean): Boolean =
        running && engineExists && !effectOwned
}
