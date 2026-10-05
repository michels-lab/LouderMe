package com.michelslab.louderme.audio

object AudioBoostContract {
    const val ACTION_ENABLE = "com.michelslab.louderme.action.ENABLE"
    const val ACTION_SET_LEVEL = "com.michelslab.louderme.action.SET_LEVEL"
    const val ACTION_DISABLE = "com.michelslab.louderme.action.DISABLE"
    const val ACTION_STATE = "com.michelslab.louderme.action.STATE"

    const val ACTION_SET_EQ = "com.michelslab.louderme.action.SET_EQ"
    const val ACTION_EQ_STATE = "com.michelslab.louderme.action.EQ_STATE"

    const val EXTRA_PERCENT = "percent"
    const val EXTRA_STATUS = "status"
    const val EXTRA_GAIN_DB = "gain_db"
    const val EXTRA_ROUTE = "route"
    const val EXTRA_IMPLEMENTATION = "implementation"
    const val EXTRA_MESSAGE = "message"

    const val EXTRA_EQ_ENABLED = "eq_enabled"
    const val EXTRA_EQ_PRESET = "eq_preset"
    const val EXTRA_EQ_GAINS = "eq_gains"
    const val EXTRA_EQ_STATUS = "eq_status"
    const val EXTRA_EQ_IMPLEMENTATION = "eq_implementation"
    const val EXTRA_EQ_MESSAGE = "eq_message"
}
