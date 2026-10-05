package com.michelslab.louderme.update

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.pm.PackageInstaller
import android.os.Build

class UpdateInstallReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        val status = intent.getIntExtra(
            PackageInstaller.EXTRA_STATUS,
            PackageInstaller.STATUS_FAILURE,
        )
        val message = intent.getStringExtra(
            PackageInstaller.EXTRA_STATUS_MESSAGE
        )

        when (status) {
            PackageInstaller.STATUS_PENDING_USER_ACTION -> {
                val confirmationIntent = if (Build.VERSION.SDK_INT >= 33) {
                    intent.getParcelableExtra(
                        Intent.EXTRA_INTENT,
                        Intent::class.java,
                    )
                } else {
                    @Suppress("DEPRECATION")
                    intent.getParcelableExtra(Intent.EXTRA_INTENT)
                }

                confirmationIntent?.addFlags(
                    Intent.FLAG_ACTIVITY_NEW_TASK
                )

                if (confirmationIntent != null) {
                    context.startActivity(confirmationIntent)
                }
            }

            PackageInstaller.STATUS_SUCCESS -> {
                saveResult(
                    context,
                    "Update installed successfully.",
                )
            }

            else -> {
                saveResult(
                    context,
                    "Install failed" +
                        (message?.let { ": " + it }
                            ?: " (status " + status + ")"),
                )
            }
        }
    }

    private fun saveResult(
        context: Context,
        message: String,
    ) {
        context
            .getSharedPreferences(
                PREFS,
                Context.MODE_PRIVATE,
            )
            .edit()
            .putString(
                KEY_LAST_INSTALL_RESULT,
                message,
            )
            .apply()
    }

    companion object {
        const val PREFS = "louderme_direct_update"
        const val KEY_LAST_INSTALL_RESULT =
            "last_install_result"
    }
}
