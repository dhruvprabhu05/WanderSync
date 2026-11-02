package dev.dhruvprabhu.wandersync.auth

import android.app.Activity
import android.content.Context
import com.microsoft.identity.client.AcquireTokenParameters
import com.microsoft.identity.client.AcquireTokenSilentParameters
import com.microsoft.identity.client.AuthenticationCallback
import com.microsoft.identity.client.IAccount
import com.microsoft.identity.client.IAuthenticationResult
import com.microsoft.identity.client.ISingleAccountPublicClientApplication
import com.microsoft.identity.client.Logger
import com.microsoft.identity.client.PublicClientApplication
import com.microsoft.identity.client.SignInParameters
import com.microsoft.identity.client.exception.MsalException
import com.microsoft.identity.client.exception.MsalUiRequiredException
import dev.dhruvprabhu.wandersync.BuildConfig
import dev.dhruvprabhu.wandersync.R
import kotlinx.coroutines.CancellableContinuation
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/** Thrown when the user closes the sign-in tab. Not an error worth showing. */
class AuthCancelledException : Exception("Sign-in was cancelled")

/**
 * The only class that talks to MSAL. Everything else in the app asks this class
 * for "the current user" or "a token", so swapping identity providers later
 * means changing one file.
 *
 * MSAL's blocking calls must not run on the main thread, so they run on
 * Dispatchers.IO. Its interactive calls (which open the browser tab) report back
 * through callbacks, which are bridged to coroutines with suspendCancellableCoroutine.
 */
class AuthRepository(context: Context) {

    private val appContext = context.applicationContext
    private val scopes = listOf(BuildConfig.API_SCOPE)
    private val mutex = Mutex()
    private var client: ISingleAccountPublicClientApplication? = null

    /** Created once, lazily: reading the config file and the token cache does disk I/O. */
    private suspend fun client(): ISingleAccountPublicClientApplication = mutex.withLock {
        client ?: withContext(Dispatchers.IO) {
            if (BuildConfig.DEBUG) {
                // Debug builds only: MSAL's own log in Logcat (tag "MSAL"), without
                // personal data, so sign-in problems can be diagnosed.
                Logger.getInstance().setEnableLogcatLog(true)
                Logger.getInstance().setLogLevel(Logger.LogLevel.VERBOSE)
                Logger.getInstance().setEnablePII(false)
            }
            PublicClientApplication.createSingleAccountPublicClientApplication(appContext, R.raw.auth_config)
        }.also { client = it }
    }

    /** The signed-in account from MSAL's token cache, or null. No network call. */
    suspend fun currentAccount(): IAccount? {
        val app = client()
        return withContext(Dispatchers.IO) { app.currentAccount.currentAccount }
    }

    /** Opens the sign-in / sign-up page in a browser tab. */
    suspend fun signIn(activity: Activity): IAccount {
        val app = client()
        val result = suspendCancellableCoroutine<IAuthenticationResult> { cont ->
            app.signIn(
                SignInParameters.builder()
                    .withActivity(activity)
                    .withScopes(scopes)
                    .withCallback(callbackFor(cont))
                    .build()
            )
        }
        return result.account
    }

    /**
     * An access token for the WanderSync API. Tries the cache first (silent,
     * refreshing it if expired). Only if that needs the user, for example a
     * revoked session, does it open the browser again.
     */
    suspend fun accessToken(activity: Activity): String {
        val app = client()
        val account = currentAccount() ?: error("Not signed in")
        return try {
            withContext(Dispatchers.IO) {
                val params = AcquireTokenSilentParameters.Builder()
                    .forAccount(account)
                    .fromAuthority(app.configuration.defaultAuthority.authorityURL.toString())
                    .withScopes(scopes)
                    .build()
                app.acquireTokenSilent(params).accessToken
            }
        } catch (e: MsalUiRequiredException) {
            suspendCancellableCoroutine<IAuthenticationResult> { cont ->
                app.acquireToken(
                    AcquireTokenParameters.Builder()
                        .startAuthorizationFromActivity(activity)
                        .forAccount(account)
                        .withScopes(scopes)
                        .withCallback(callbackFor(cont))
                        .build()
                )
            }.accessToken
        }
    }

    /** Clears this app's tokens from the device. */
    suspend fun signOut() {
        val app = client()
        withContext(Dispatchers.IO) { app.signOut() }
    }

    private fun callbackFor(cont: CancellableContinuation<IAuthenticationResult>) =
        object : AuthenticationCallback {
            override fun onSuccess(result: IAuthenticationResult) {
                if (cont.isActive) cont.resume(result)
            }

            override fun onError(exception: MsalException) {
                if (cont.isActive) cont.resumeWithException(exception)
            }

            override fun onCancel() {
                if (cont.isActive) cont.resumeWithException(AuthCancelledException())
            }
        }
}
