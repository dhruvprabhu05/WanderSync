package dev.dhruvprabhu.wandersync

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import dev.dhruvprabhu.wandersync.ui.AuthScreen
import dev.dhruvprabhu.wandersync.ui.AuthViewModel
import dev.dhruvprabhu.wandersync.ui.theme.WanderSyncTheme

class MainActivity : ComponentActivity() {

    private val viewModel: AuthViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            WanderSyncTheme {
                // Collects only while the app is visible, so nothing runs in the background.
                val state by viewModel.state.collectAsStateWithLifecycle()
                Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
                    AuthScreen(
                        state = state,
                        onSignIn = { viewModel.signIn(this@MainActivity) },
                        onCallApi = { viewModel.callApi(this@MainActivity) },
                        onSignOut = viewModel::signOut,
                        modifier = Modifier.padding(innerPadding),
                    )
                }
            }
        }
    }
}
