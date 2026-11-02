package dev.dhruvprabhu.wandersync.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import dev.dhruvprabhu.wandersync.ui.theme.WanderSyncTheme

/**
 * Stateless: it draws whatever [state] says and reports clicks upward. That makes
 * it previewable and testable without MSAL or a network.
 */
@Composable
fun AuthScreen(
    state: AuthUiState,
    onSignIn: () -> Unit,
    onCallApi: () -> Unit,
    onSignOut: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text("WanderSync", style = MaterialTheme.typography.headlineMedium)

        when {
            state.isLoading -> CircularProgressIndicator()
            !state.isSignedIn -> {
                Text("You're signed out.")
                Button(onClick = onSignIn, enabled = !state.isBusy) { Text("Sign in or create account") }
            }
            else -> {
                Text("Signed in as ${state.username}")
                Button(onClick = onCallApi, enabled = !state.isBusy) { Text("Call /v1/me") }
                OutlinedButton(onClick = onSignOut, enabled = !state.isBusy) { Text("Sign out") }
            }
        }

        if (state.isBusy) LinearProgressIndicator(modifier = Modifier.fillMaxWidth())
        state.apiResult?.let { Text(it, style = MaterialTheme.typography.bodyMedium) }
        state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
    }
}

@Preview(showBackground = true)
@Composable
private fun SignedOutPreview() {
    WanderSyncTheme {
        AuthScreen(AuthUiState(isLoading = false), onSignIn = {}, onCallApi = {}, onSignOut = {})
    }
}

@Preview(showBackground = true)
@Composable
private fun SignedInPreview() {
    WanderSyncTheme {
        AuthScreen(
            AuthUiState(isLoading = false, username = "traveler@example.com", apiResult = "API says you are:\nsubject = 1234"),
            onSignIn = {}, onCallApi = {}, onSignOut = {},
        )
    }
}
