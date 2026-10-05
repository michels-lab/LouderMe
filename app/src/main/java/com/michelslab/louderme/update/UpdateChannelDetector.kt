package com.michelslab.louderme.update

import android.content.Context
import android.os.Build

object UpdateChannelDetector {
    private const val PLAY_PACKAGE = "com.android.vending"

    fun isPlayInstall(context: Context): Boolean {
        return runCatching {
            val installer = if (Build.VERSION.SDK_INT >= 30) {
                context.packageManager
                    .getInstallSourceInfo(context.packageName)
                    .installingPackageName
            } else {
                @Suppress("DEPRECATION")
                context.packageManager
                    .getInstallerPackageName(context.packageName)
            }

            installer == PLAY_PACKAGE
        }.getOrDefault(false)
    }
}
