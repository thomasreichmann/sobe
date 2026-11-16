package br.com.fiap.sobe

import android.Manifest
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material.icons.filled.Insights
import androidx.compose.material.icons.filled.NotificationsActive
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Subway
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import br.com.fiap.sobe.data.Usuario
import br.com.fiap.sobe.ui.Fabrica
import br.com.fiap.sobe.ui.alertas.AlertasScreen
import br.com.fiap.sobe.ui.alertas.AlertasViewModel
import br.com.fiap.sobe.ui.componentes.AtualizarPeriodicamente
import br.com.fiap.sobe.ui.conta.ContaScreen
import br.com.fiap.sobe.ui.conta.PainelScreen
import br.com.fiap.sobe.ui.entrada.CriarContaScreen
import br.com.fiap.sobe.ui.entrada.EntradaViewModel
import br.com.fiap.sobe.ui.entrada.EntrarScreen
import br.com.fiap.sobe.ui.estacoes.EstacaoScreen
import br.com.fiap.sobe.ui.estacoes.EstacoesScreen
import br.com.fiap.sobe.ui.relatos.NovoRelatoScreen
import br.com.fiap.sobe.ui.relatos.RelatosScreen
import br.com.fiap.sobe.ui.tema.TemaSobe
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val app = application as SobeApplication
        setContent {
            TemaSobe {
                val usuario by app.sessao.usuario.collectAsStateWithLifecycle()
                val atual = usuario
                if (atual == null) Entrada() else Principal(atual, app.repositorio.servidor, app.repositorio::sair)
            }
        }
    }
}

@Composable
private fun Entrada() {
    val navegacao = rememberNavController()
    val vm: EntradaViewModel = viewModel(factory = Fabrica)
    NavHost(navegacao, startDestination = "entrar") {
        composable("entrar") { EntrarScreen(vm) { navegacao.navigate("criar-conta") } }
        composable("criar-conta") { CriarContaScreen(vm) { navegacao.popBackStack() } }
    }
}

private data class Aba(val rota: String, val rotulo: String, val icone: ImageVector)

private val ABAS_DO_CIDADAO = listOf(
    Aba("estacoes", "Estações", Icons.Filled.Subway),
    Aba("paradas", "Paradas", Icons.Filled.NotificationsActive),
    Aba("avisos", "Meus avisos", Icons.Filled.Campaign),
    Aba("conta", "Conta", Icons.Filled.Person),
)

private val ABAS_DO_ADMIN = listOf(
    Aba("estacoes", "Estações", Icons.Filled.Subway),
    Aba("painel", "Painel", Icons.Filled.Insights),
    Aba("avisos", "Avisos", Icons.Filled.Campaign),
    Aba("conta", "Conta", Icons.Filled.Person),
)

@Composable
private fun Principal(usuario: Usuario, servidor: String, aoSair: () -> Unit) {
    val contexto = LocalContext.current
    val navegacao = rememberNavController()
    val escopo = rememberCoroutineScope()
    val avisos = remember { SnackbarHostState() }
    val alertas: AlertasViewModel = viewModel(key = "alertas-${usuario.id}", factory = Fabrica)
    val abas = if (usuario.admin) ABAS_DO_ADMIN else ABAS_DO_CIDADAO
    val rotaAtual = navegacao.currentBackStackEntryAsState().value?.destination?.route
    val avisar: (String) -> Unit = { mensagem -> escopo.launch { avisos.showSnackbar(mensagem) } }

    val pedirPermissao = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) {}
    LaunchedEffect(Unit) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU && !podeNotificar(contexto)) {
            pedirPermissao.launch(Manifest.permission.POST_NOTIFICATIONS)
        }
    }
    AtualizarPeriodicamente { alertas.atualizar() }
    LaunchedEffect(alertas) {
        alertas.novos.collect { alerta ->
            notificarParada(contexto, alerta)
            avisar("Parada em ${alerta.local.estacao}: ${alerta.local.localizacao.replaceFirstChar(Char::lowercase)}")
        }
    }

    Scaffold(
        containerColor = MaterialTheme.colorScheme.background,
        snackbarHost = { SnackbarHost(avisos) },
        bottomBar = {
            if (abas.any { it.rota == rotaAtual }) {
                BarraDeAbas(abas, rotaAtual, alertas.dasFavoritas?.size ?: 0, navegacao)
            }
        },
    ) { margens ->
        NavHost(navegacao, startDestination = "estacoes", modifier = Modifier.fillMaxSize().padding(margens)) {
            composable("estacoes") {
                EstacoesScreen(viewModel(factory = Fabrica), { navegacao.navigate("estacao/$it") }, avisar)
            }
            composable("estacao/{id}", arguments = listOf(navArgument("id") { type = NavType.IntType })) {
                EstacaoScreen(
                    vm = viewModel(factory = Fabrica),
                    admin = usuario.admin,
                    aoVoltar = { navegacao.popBackStack() },
                    aoAbrirEstacao = { navegacao.navigate("estacao/$it") },
                    aoRelatar = { navegacao.navigate("avisar/$it") },
                    avisar = avisar,
                )
            }
            composable("avisar/{equipamentoId}", arguments = listOf(navArgument("equipamentoId") { type = NavType.IntType })) {
                NovoRelatoScreen(viewModel(factory = Fabrica), { navegacao.popBackStack() }) {
                    navegacao.popBackStack()
                    avisar("Aviso enviado. Acompanhe em Meus avisos.")
                }
            }
            composable("paradas") {
                AlertasScreen(alertas, { navegacao.navigate("estacao/$it") }) { navegacao.irParaAba("estacoes") }
            }
            composable("avisos") {
                RelatosScreen(viewModel(factory = Fabrica), usuario.admin, { navegacao.irParaAba("estacoes") }, avisar)
            }
            composable("painel") { PainelScreen(viewModel(factory = Fabrica)) }
            composable("conta") { ContaScreen(usuario, servidor, aoSair) }
        }
    }
}

private fun NavHostController.irParaAba(rota: String) {
    navigate(rota) {
        popUpTo(graph.findStartDestination().id) { saveState = true }
        launchSingleTop = true
        restoreState = true
    }
}

@Composable
private fun BarraDeAbas(abas: List<Aba>, rotaAtual: String?, paradas: Int, navegacao: NavHostController) {
    NavigationBar(containerColor = MaterialTheme.colorScheme.surface) {
        abas.forEach { aba ->
            NavigationBarItem(
                selected = aba.rota == rotaAtual,
                onClick = { navegacao.irParaAba(aba.rota) },
                icon = {
                    BadgedBox(badge = {
                        if (aba.rota == "paradas" && paradas > 0) Badge { Text(paradas.toString()) }
                    }) {
                        Icon(aba.icone, contentDescription = null, modifier = Modifier.size(26.dp))
                    }
                },
                label = { Text(aba.rotulo, style = MaterialTheme.typography.labelSmall) },
                colors = NavigationBarItemDefaults.colors(indicatorColor = MaterialTheme.colorScheme.secondaryContainer),
            )
        }
    }
}
