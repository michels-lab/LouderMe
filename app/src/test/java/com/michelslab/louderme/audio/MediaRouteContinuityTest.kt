package com.michelslab.louderme.audio

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class MediaRouteContinuityTest {
    private fun route(vararg devices: Pair<Int, Int>): MediaRouteContinuity =
        MediaRouteContinuity.from("Bluetooth", devices.toList())

    @Test
    fun sameClassDifferentDeviceIdentityTriggersRecheck() {
        val old = route(8 to 31)
        val new = route(8 to 32)
        assertTrue(MediaRouteRecoveryPolicy.hasChanged(old, new))
        assertFalse(MediaRouteRecoveryPolicy.hasChanged(new, new))
    }

    @Test
    fun availableDeviceOrderAndDuplicatesDoNotCreateFakeRouteChanges() {
        val old = route(1 to 9, 8 to 15)
        val equivalent = route(8 to 15, 1 to 9, 1 to 9)
        assertEquals(old, equivalent)
        assertFalse(MediaRouteRecoveryPolicy.hasChanged(old, equivalent))
        assertFalse(MediaRouteRecoveryPolicy.hasChanged(null, old))
    }

    @Test
    fun unstableEmptyMediaListGetsOneBoundedRetryOnly() {
        val old = route(8 to 15)
        val empty = route()
        assertTrue(MediaRouteRecoveryPolicy.deferTransientEmpty(old, empty, 0))
        assertFalse(MediaRouteRecoveryPolicy.deferTransientEmpty(old, empty, 1))
        assertFalse(MediaRouteRecoveryPolicy.deferTransientEmpty(null, empty, 0))
        assertFalse(MediaRouteRecoveryPolicy.deferTransientEmpty(old, route(8 to 15), 0))
    }

    @Test
    fun reattachmentIsOnlyAllowedForActiveEngineWithLostControl() {
        assertTrue(MediaRouteRecoveryPolicy.mustReattachBoost(true, true, false))
        assertFalse(MediaRouteRecoveryPolicy.mustReattachBoost(true, true, true))
        assertFalse(MediaRouteRecoveryPolicy.mustReattachBoost(true, false, false))
        assertFalse(MediaRouteRecoveryPolicy.mustReattachBoost(false, true, false))
    }
}
