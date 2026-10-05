package com.michelslab.louderme.audio

object AudioBoostContract {
    const val ACTION_ENABLE = "com.michelslab.louderme.action.ENABLE"
    const val ACTION_SET_LEVEL = "com.michelslab.louderme.action.SET_LEVEL"
    const val ACTION_DISABLE = "com.michelslab.louderme.action.DISABLE"
    const val ACTION_STATE = "com.michelslab.louderme.action.STATE"

    const val EXTRA_PERCENT = "percent"
    const val EXTRA_STATUS = "status"
    const val EXTRA_GAIN_DB = "gain_db"
    const val EXTRA_ROUTE = "route"
    const val EXTRA_IMPLEMENTATION = "implementation"
    const val EXTRA_MESSAGE = "message"
}
