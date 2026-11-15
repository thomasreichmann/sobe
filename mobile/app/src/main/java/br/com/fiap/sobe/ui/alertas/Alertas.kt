package br.com.fiap.sobe.ui.alertas

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import br.com.fiap.sobe.data.Alerta
import br.com.fiap.sobe.data.Repositorio
import br.com.fiap.sobe.data.Resultado
import br.com.fiap.sobe.data.StatusEquipamento
import br.com.fiap.sobe.ui.componentes.Aviso
import br.com.fiap.sobe.ui.componentes.Carregando
import br.com.fiap.sobe.ui.componentes.Espaco
import br.com.fiap.sobe.ui.componentes.SemConexao
import br.com.fiap.sobe.ui.componentes.SimboloDaLinha
import br.com.fiap.sobe.ui.componentes.Situacao
import br.com.fiap.sobe.ui.componentes.nome
import br.com.fiap.sobe.ui.componentes.tempoDesde
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.launch

class AlertasViewModel(private val repositorio: Repositorio) : ViewModel() {
    private val novosAlertas = MutableSharedFlow<Alerta>(extraBufferCapacity = 16)
    private var conhecidos: Set<Int>? = null

    var somenteFavoritas by mutableStateOf(true)
    var dasFavoritas by mutableStateOf<List<Alerta>?>(null)
        private set
    var todos by mutableStateOf<List<Alerta>?>(null)
        private set
    var erro by mutableStateOf<String?>(null)
        private set

    val novos: SharedFlow<Alerta> = novosAlertas
    val visiveis get() = if (somenteFavoritas) dasFavoritas else todos

    suspend fun atualizar() {
        when (val resultado = repositorio.alertas(somenteFavoritas = true)) {
            is Resultado.Ok -> {
                val antes = conhecidos
                dasFavoritas = resultado.valor
                conhecidos = resultado.valor.map { it.id }.toSet()
                erro = null
                if (antes != null) resultado.valor.filter { it.id !in antes }.forEach(novosAlertas::tryEmit)
            }
            is Resultado.Falha -> erro = resultado.mensagem
        }
        if (!somenteFavoritas) {
            val resultado = repositorio.alertas(somenteFavoritas = false)
            if (resultado is Resultado.Ok) todos = resultado.valor
        }
    }
}

@Composable
fun AlertasScreen(vm: AlertasViewModel, aoAbrirEstacao: (Int) -> Unit, aoVerEstacoes: () -> Unit) {
    val escopo = rememberCoroutineScope()
    val alertas = vm.visiveis
    val erro = vm.erro

    Column(Modifier.fillMaxSize()) {
        Text(
            "Paradas",
            style = MaterialTheme.typography.headlineMedium,
            modifier = Modifier.padding(start = 20.dp, top = 20.dp, end = 20.dp).semantics { heading() },
        )
        FilterChip(
            selected = vm.somenteFavoritas,
            onClick = {
                vm.somenteFavoritas = !vm.somenteFavoritas
                escopo.launch { vm.atualizar() }
            },
            label = { Text("Só das estações que eu acompanho") },
            leadingIcon = { Icon(Icons.Filled.Star, contentDescription = null, modifier = Modifier.size(18.dp)) },
            modifier = Modifier.padding(horizontal = 20.dp, vertical = 8.dp),
        )
        when {
            alertas == null && erro != null -> SemConexao(erro, { escopo.launch { vm.atualizar() } })
            alertas == null -> Carregando()
            alertas.isEmpty() && vm.somenteFavoritas -> Aviso(
                Icons.Filled.CheckCircle,
                "Nenhuma parada nas suas estações",
                "Quando um equipamento de uma estação que você acompanha parar, o aviso aparece aqui.",
                acao = "Escolher estações para acompanhar",
                aoAgir = aoVerEstacoes,
            )
            alertas.isEmpty() -> Aviso(Icons.Filled.CheckCircle, "Nenhuma parada agora", "Todos os equipamentos monitorados estão funcionando.")
            else -> LazyColumn(
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 6.dp, bottom = 24.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                items(alertas, key = { it.id }) { alerta -> CartaoDeAlerta(alerta) { aoAbrirEstacao(alerta.local.estacaoId) } }
            }
        }
    }
}

@Composable
private fun CartaoDeAlerta(alerta: Alerta, aoAbrir: () -> Unit) {
    Surface(shape = MaterialTheme.shapes.large, color = MaterialTheme.colorScheme.surface) {
        Column(
            Modifier.fillMaxWidth().clickable(onClickLabel = "abrir a estação", role = Role.Button, onClick = aoAbrir).padding(16.dp)
                .semantics(mergeDescendants = true) {},
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                SimboloDaLinha(alerta.local.linhaNumero, alerta.local.linhaCor, tamanho = 26.dp)
                Text(alerta.local.estacao, style = MaterialTheme.typography.titleLarge, modifier = Modifier.padding(start = 10.dp))
            }
            Espaco(10.dp)
            Text("${alerta.local.tipo.nome}: ${alerta.local.localizacao.replaceFirstChar(Char::lowercase)}", style = MaterialTheme.typography.bodyLarge)
            Text(alerta.motivo, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Espaco(12.dp)
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                Situacao(StatusEquipamento.PARADO)
                Text(tempoDesde(alerta.abertoEm), style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}
