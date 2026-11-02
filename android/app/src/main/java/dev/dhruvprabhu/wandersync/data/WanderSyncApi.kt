package dev.dhruvprabhu.wandersync.data

import dev.dhruvprabhu.wandersync.BuildConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL

/** What GET /v1/me returns: the identity the API sees in our token. */
data class Me(val subject: String, val name: String?, val email: String?)

class ApiException(val statusCode: Int) : Exception("API returned HTTP $statusCode")

/**
 * Minimal HTTP client for the API. Deliberately dependency-free for this first
 * end-to-end check; Phase 3 replaces it with Retrofit once there are real
 * endpoints to model.
 */
class WanderSyncApi(private val baseUrl: String = BuildConfig.API_BASE_URL) {

    suspend fun me(accessToken: String): Me = withContext(Dispatchers.IO) {
        val connection = URL("$baseUrl/v1/me").openConnection() as HttpURLConnection
        try {
            // Generous timeouts: the API scales to zero, so the first call
            // after idle waits for a cold start.
            connection.connectTimeout = 30_000
            connection.readTimeout = 30_000
            connection.setRequestProperty("Authorization", "Bearer $accessToken")
            connection.setRequestProperty("Accept", "application/json")

            val status = connection.responseCode
            if (status != HttpURLConnection.HTTP_OK) throw ApiException(status)

            val json = JSONObject(connection.inputStream.bufferedReader().use { it.readText() })
            Me(
                subject = json.getString("subject"),
                name = json.stringOrNull("name"),
                email = json.stringOrNull("email"),
            )
        } finally {
            connection.disconnect()
        }
    }

    private fun JSONObject.stringOrNull(key: String): String? =
        if (has(key) && !isNull(key)) getString(key) else null
}
