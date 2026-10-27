package com.michelslab.louderme.ui

import com.michelslab.louderme.audio.AudioEngineStatus
import com.michelslab.louderme.audio.AudioEngineUiState
import com.michelslab.louderme.audio.EqualizerStatus
import com.michelslab.louderme.audio.EqualizerUiState
import org.json.JSONArray
import org.json.JSONObject

/**
 * Developer-only, local, metadata-only observations of live audio state.
 * Unlike error messages, stack traces, output-route names or device IDs,
 * these closed vocabulary codes cannot contain sensitive user input.
 */
internal data class DeveloperRuntimeIncident(
    val code: String,
    val firstUtcMs: Long,
    val lastUtcMs: Long,
    val occurrences: Int,
)

internal object DeveloperRuntimeLedger {
    const val schema = "michelslab-developer-runtime-ledger-v1"
    const val maxEntries = 16
    const val maxOccurrences = 100_000
    val allowedCodes = setOf(
        "AUDIO_ENGINE_ERROR_OBSERVED",
        "EQUALIZER_ENGINE_ERROR_OBSERVED",
        "BOOST_STATE_OUT_OF_RANGE",
        "EQUALIZER_BAND_COUNT_INVALID",
        "EQUALIZER_NONFINITE_GAIN",
    )

    fun detect(audio: AudioEngineUiState, eq: EqualizerUiState): Set<String> = buildSet {
        if (audio.status == AudioEngineStatus.ERROR) add("AUDIO_ENGINE_ERROR_OBSERVED")
        if (eq.status == EqualizerStatus.ERROR) add("EQUALIZER_ENGINE_ERROR_OBSERVED")
        if (audio.percent !in 100..250) add("BOOST_STATE_OUT_OF_RANGE")
        if (eq.gainsDb.size != 7) add("EQUALIZER_BAND_COUNT_INVALID")
        if (eq.gainsDb.any { !it.isFinite() }) add("EQUALIZER_NONFINITE_GAIN")
    }

    fun append(incidents: List<DeveloperRuntimeIncident>, code: String,
               nowUtcMs: Long): List<DeveloperRuntimeIncident> {
        require(code in allowedCodes && nowUtcMs > 0L) { "Invalid runtime observation." }
        validate(incidents)
        val index = incidents.indexOfFirst { it.code == code }
        val next = incidents.toMutableList()
        if (index >= 0) {
            val old = next[index]
            next[index] = old.copy(
                lastUtcMs = maxOf(nowUtcMs, old.lastUtcMs),
                occurrences = (old.occurrences + 1).coerceAtMost(maxOccurrences),
            )
        } else {
            next += DeveloperRuntimeIncident(code, nowUtcMs, nowUtcMs, 1)
        }
        return next.sortedByDescending { it.lastUtcMs }.take(maxEntries)
    }

    fun encode(incidents: List<DeveloperRuntimeIncident>): String {
        validate(incidents)
        val entries = JSONArray()
        incidents.forEach { event ->
            entries.put(JSONObject()
                .put("code", event.code)
                .put("firstUtcMs", event.firstUtcMs)
                .put("lastUtcMs", event.lastUtcMs)
                .put("occurrences", event.occurrences))
        }
        return JSONObject().put("schema", schema).put("incidents", entries).toString()
    }

    fun decode(raw: String): List<DeveloperRuntimeIncident> {
        require(raw.toByteArray(Charsets.UTF_8).size <= 16_384) {
            "Developer diagnostic history exceeds its local bound."
        }
        val doc = JSONObject(raw)
        require(doc.getString("schema") == schema) { "Unsupported runtime diagnostics." }
        val array = doc.getJSONArray("incidents")
        require(array.length() <= maxEntries) { "Too many runtime incidents." }
        val incidents = (0 until array.length()).map { i ->
            val item = array.getJSONObject(i)
            DeveloperRuntimeIncident(item.getString("code"), item.getLong("firstUtcMs"),
                item.getLong("lastUtcMs"), item.getInt("occurrences"))
        }
        validate(incidents)
        return incidents
    }

    private fun validate(incidents: List<DeveloperRuntimeIncident>) {
        require(incidents.size <= maxEntries && incidents.map { it.code }.distinct().size == incidents.size) {
            "Duplicate or excess diagnostic entries."
        }
        require(incidents.all {
            it.code in allowedCodes && it.firstUtcMs > 0L &&
                it.lastUtcMs >= it.firstUtcMs && it.occurrences in 1..maxOccurrences
        }) { "Runtime history contains invalid or unapproved diagnostic metadata." }
    }
}
