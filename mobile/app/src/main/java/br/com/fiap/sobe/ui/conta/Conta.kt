package br.com.fiap.sobe.ui.conta

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import br.com.fiap.sobe.data.Painel
import br.com.fiap.sobe.data.Repositorio
import br.com.fiap.sobe.data.Resultado
import br.com.fiap.sobe.data.Usuario
import br.com.fiap.sobe.ui.componentes.AtualizarPeriodicamente
import br.com.fiap.sobe.ui.componentes.Carregando
import br.com.fiap.sobe.ui.componentes.Espaco
import br.com.fiap.sobe.ui.componentes.SemConexao
import br.com.fiap.sobe.ui.componentes.SimboloDaLinha
import br.com.fiap.sobe.ui.componentes.duracao
import br.com.fiap.sobe.ui.componentes.nome
import br.com.fiap.sobe.ui.tema.LocalSituacao
import java.util.Locale
import kotlinx.coroutines.launch

class PainelViewModel(private val repositorio: Repositorio) : ViewModel() {
    var painel by mutableStateOf<Painel?>(null)
        private set
    var erro by mutableStateOf<String?>(null)
        private set

    suspend fun atualizar() {
        when (val resultado = repositorio.painel()) {
            is Resultado.Ok -> {
                painel = resultado.valor
                erro = null
            }
            is Resultado.Falha -> erro = resultado.mensagem
        }
    }
}

@Composable
fun ContaScreen(usuario: Usuario, servidor: String, aoSair: () -> Unit) {
    Column(Modifier.fillMaxSize().padding(horizontal = 20.dp)) {
        Text("Conta", style = MaterialTheme.typography.headlineMedium, modifier = Modifier.padding(top = 20.dp, bottom = 14.dp).semantics { heading() })
        Surface(shape = MaterialTheme.shapes.large, color = MaterialTheme.colorScheme.surface, modifier = Modifier.fillMaxWidth()) {
            Column {
                Dado("Nome", usuario.nome)
                HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                Dado("E-mail", usuario.email)
                HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                Dado("Perfil", if (usuario.admin) "Administrador da operadora" else "Cidadão")
                HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                Dado("Servidor", servidor)
            }
        }
        Espaco(20.dp)
        OutlinedButton(onClick = aoSair, shape = MaterialTheme.shapes.medium, modifier = Modifier.fillMaxWidth().heightIn(min = 52.dp)) {
            Text("Sair da conta")
        }
    }
}

@Composable
private fun Dado(rotulo: String, valor: String) {
    Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp).semantics(mergeDescendants = true) {}) {
        Text(rotulo, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(valor, style = MaterialTheme.typography.bodyLarge)
    }
}

@Composable
fun PainelScreen(vm: PainelViewModel) {
    val escopo = rememberCoroutineScope()
    AtualizarPeriodicamente { vm.atualizar() }
    val painel = vm.painel
    val erro = vm.erro
    val situacao = LocalSituacao.current

    Column(Modifier.fillMaxSize()) {
        Text(
            "Painel",
            style = MaterialTheme.typography.headlineMedium,
            modifier = Modifier.padding(start = 20.dp, top = 20.dp, end = 20.dp, bottom = 8.dp).semantics { heading() },
        )
        when {
            painel == null && erro != null -> SemConexao(erro, { escopo.launch { vm.atualizar() } })
            painel == null -> Carregando()
            else -> LazyColumn(
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 6.dp, bottom = 24.dp),
                verticalArrangement = Arrangement.spacedBy(14.dp),
            ) {
                item {
                    Bloco("Agora") {
                        Contagem("Funcionando", painel.operando, situacao.operando)
                        Contagem("Parados", painel.parados, situacao.parado)
                        Contagem("Em manutenção", painel.emManutencao, situacao.manutencao)
                        Contagem("Avisos de cidadãos aguardando", painel.relatosAbertos, MaterialTheme.colorScheme.onSurface)
                    }
                }
                item {
                    Bloco("Mais tempo parado em 30 dias") {
                        if (painel.maisParados.isEmpty()) Text("Nenhuma parada no período.", Modifier.padding(16.dp))
                        painel.maisParados.forEach { item ->
                            Row(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 10.dp), verticalAlignment = Alignment.CenterVertically) {
                                SimboloDaLinha(item.local.linhaNumero, item.local.linhaCor, tamanho = 26.dp)
                                Column(Modifier.weight(1f).padding(horizontal = 12.dp)) {
                                    Text(item.local.estacao, style = MaterialTheme.typography.titleSmall)
                                    Text(
                                        "${item.local.tipo.nome}, ${if (item.paradas == 1) "1 parada" else "${item.paradas} paradas"}",
                                        style = MaterialTheme.typography.bodySmall,
                                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    )
                                }
                                Text(duracao(item.minutosParado), style = MaterialTheme.typography.titleSmall)
                            }
                        }
                    }
                }
                item {
                    Bloco("Disponibilidade dos elevadores em 30 dias") {
                        painel.disponibilidade.forEach { linha ->
                            Row(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 10.dp), verticalAlignment = Alignment.CenterVertically) {
                                Text("Linha ${linha.linhaNumero} ${linha.linhaNome}", Modifier.weight(1f))
                                Text(String.format(Locale("pt", "BR"), "%.2f%%", linha.percentual), style = MaterialTheme.typography.titleSmall)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun Bloco(titulo: String, conteudo: @Composable () -> Unit) {
    Surface(shape = MaterialTheme.shapes.large, color = MaterialTheme.colorScheme.surface, modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(vertical = 8.dp)) {
            Text(titulo, style = MaterialTheme.typography.titleMedium, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp).semantics { heading() })
            conteudo()
        }
    }
}

@Composable
private fun Contagem(rotulo: String, valor: Int, cor: Color) {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp).semantics(mergeDescendants = true) {},
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(rotulo, Modifier.weight(1f))
        Text(valor.toString(), style = MaterialTheme.typography.titleLarge, color = cor)
    }
}
