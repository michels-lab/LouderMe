package com.michelslab.louderme.audio

/**
 * AudioEffect.setEnabled(SUCCESS) alone does not prove that the app owns or
 * controls a session-0 effect. Keep this check shared between Boost and EQ.
 */
internal object SessionZeroAttachmentPolicy {
    fun isAttached(
        enableSucceeded: Boolean,
        reportedEnabled: Boolean,
        hasControl: Boolean,
    ): Boolean = enableSucceeded && reportedEnabled && hasControl
}
