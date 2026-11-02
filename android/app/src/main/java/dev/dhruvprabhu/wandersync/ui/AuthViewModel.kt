package dev.dhruvprabhu.wandersync.ui

import android.app.Activity
import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import dev.dhruvprabhu.wandersync.auth.AuthCancelledException
import dev.dhruvprabhu.wandersync.auth.AuthRepository
import dev.dhruvprabhu.wandersync.data.WanderSyncApi
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

/** Everything the screen needs to draw itself. The UI never holds state of its own. */
data class AuthUiState(
    val isLoading: Boolean = true,
    val username: String? = null,
    val isBusy: Boolean = false,
    val apiResult: String? = null,
    val error: String? = null,
) {
    val isSignedIn: Boolean get() = username != null
}

/**
 * Holds UI state across configuration changes (rotation) and runs the work.
 *
 * Methods that may open the sign-in tab take the Activity as a parameter, used
 * only for that call, so the ViewModel never keeps a reference to it (which
 * would leak it after rotation).
 */
class AuthViewModel(application: Application) : AndroidViewModel(application) {

    private val auth = AuthRepository(application)
    private val api = WanderSyncApi()

    private val _state = MutableStateFlow(AuthUiState())
    val state: StateFlow<AuthUiState> = _state.asStateFlow()

    init {
        viewModelScope.launch {
            val account = runCatching { auth.currentAccount() }.getOrNull()
            _state.update { it.copy(isLoading = false, username = account?.username) }
        }
    }

    fun signIn(activity: Activity) = runAction {
        val account = auth.signIn(activity)
        _state.update { it.copy(username = account.username, apiResult = null) }
    }

    fun callApi(activity: Activity) = runAction {
        val me = api.me(auth.accessToken(activity))
        _state.update {
            it.copy(apiResult = "API says you are:\nsubject = ${me.subject}\nname = ${me.name ?: "-"}\nemail = ${me.email ?: "-"}")
        }
    }

    fun signOut() = runAction {
        auth.signOut()
        _state.update { AuthUiState(isLoading = false) }
    }

    /** Shows a progress bar while [block] runs and turns any failure into a message. */
    private fun runAction(block: suspend () -> Unit) {
        viewModelScope.launch {
            _state.update { it.copy(isBusy = true, error = null) }
            try {
                block()
            } catch (e: AuthCancelledException) {
                // Usually the user closed the sign-in tab. Shown for now because a
                // broken redirect also surfaces as "cancelled" and would otherwise be invisible.
                _state.update { it.copy(error = "Sign-in was cancelled or did not return to the app.") }
            } catch (e: CancellationException) {
                throw e // never swallow coroutine cancellation
            } catch (e: Exception) {
                _state.update { it.copy(error = e.message ?: e::class.java.simpleName) }
            } finally {
                _state.update { it.copy(isBusy = false) }
            }
        }
    }
}
