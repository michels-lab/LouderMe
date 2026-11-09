package com.michelslab.louderme.audio

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class SessionZeroAttachmentPolicyTest {
    @Test
    fun attachmentRequiresAllThreeIndependentSignals() {
        assertTrue(SessionZeroAttachmentPolicy.isAttached(true, true, true))
        assertFalse(SessionZeroAttachmentPolicy.isAttached(false, true, true))
        assertFalse(SessionZeroAttachmentPolicy.isAttached(true, false, true))
        assertFalse(SessionZeroAttachmentPolicy.isAttached(true, true, false))
        assertFalse(SessionZeroAttachmentPolicy.isAttached(false, false, false))
    }
}
