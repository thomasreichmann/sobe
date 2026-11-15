package br.com.fiap.sobe.ui.relatos

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import br.com.fiap.sobe.data.Relato
import br.com.fiap.sobe.data.StatusRelato
import br.com.fiap.sobe.ui.componentes.AtualizarPeriodicamente
import br.com.fiap.sobe.ui.componentes.Aviso
import br.com.fiap.sobe.ui.componentes.BotaoPrincipal
import br.com.fiap.sobe.ui.componentes.Carregando
import br.com.fiap.sobe.ui.componentes.Espaco
import br.com.fiap.sobe.ui.componentes.Etiqueta
import br.com.fiap.sobe.ui.componentes.SemConexao
import br.com.fiap.sobe.ui.componentes.SimboloDaLinha
import br.com.fiap.sobe.ui.componentes.nome
import br.com.fiap.sobe.ui.componentes.tempoDesde
import br.com.fiap.sobe.ui.tema.LocalSituacao
import kotlinx.coroutines.launch

private val StatusRelato.nome
    get() = when (this) {
        StatusRelato.ABERTO -> "Aguardando a operadora"
        StatusRelato.CONFIRMADO -> "Confirmado pela operadora"
        StatusRelato.RESOLVIDO -> "Resolvido"
        StatusRelato.DESCARTADO -> "Descartado"
    }

@Composable
fun RelatosScreen(vm: RelatosViewModel, admin: Boolean, aoVerEstacoes: () -> Unit, avisar: (String) -> Unit) {
    val escopo = rememberCoroutineScope()
    var editando by remember { mutableStateOf<Relato?>(null) }
    var apagando by remember { mutableStateOf<Relato?>(null) }
    AtualizarPeriodicamente(intervaloEmMs = 10000) { vm.atualizar() }
    LaunchedEffect(vm.aviso) {
        vm.aviso?.let(avisar)
        vm.aviso = null
    }
    val relatos = vm.relatos
    val erro = vm.erro

    Column(Modifier.fillMaxSize()) {
        Text(
            if (admin) "Avisos dos cidadãos" else "Meus avisos",
            style = MaterialTheme.typography.headlineMedium,
            modifier = Modifier.padding(start = 20.dp, top = 20.dp, end = 20.dp, bottom = 8.dp).semantics { heading() },
        )
        when {
            relatos == null && erro != null -> SemConexao(erro, { escopo.launch { vm.atualizar() } })
            relatos == null -> Carregando()
            relatos.isEmpty() && admin -> Aviso(Icons.Filled.Campaign, "Nenhum aviso recebido", "Os problemas que os cidadãos relatarem aparecem aqui.")
            relatos.isEmpty() -> Aviso(
                Icons.Filled.Campaign,
                "Você ainda não avisou nenhum problema",
                "Viu um elevador parado? Abra a estação e toque em Avisar um problema.",
                acao = "Ver estações",
                aoAgir = aoVerEstacoes,
            )
            else -> LazyColumn(
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 6.dp, bottom = 24.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                items(relatos, key = { it.id }) { relato ->
                    CartaoDeRelato(relato, admin, { editando = relato }, { apagando = relato }, { vm.tratar(relato, it) })
                }
            }
        }
    }

    editando?.let { relato ->
        DialogoDeEdicao(relato, { editando = null }) { texto ->
            vm.editar(relato, texto)
            editando = null
        }
    }
    apagando?.let { relato ->
        AlertDialog(
            onDismissRequest = { apagando = null },
            title = { Text("Apagar este aviso?") },
            text = { Text("O aviso sobre ${relato.local.estacao} deixa de existir. Não dá para desfazer.") },
            confirmButton = {
                TextButton(onClick = {
                    vm.apagar(relato)
                    apagando = null
                }) { Text("Apagar aviso", color = MaterialTheme.colorScheme.error) }
            },
            dismissButton = { TextButton(onClick = { apagando = null }) { Text("Manter") } },
        )
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun CartaoDeRelato(relato: Relato, admin: Boolean, aoEditar: () -> Unit, aoApagar: () -> Unit, aoTratar: (StatusRelato) -> Unit) {
    val situacao = LocalSituacao.current
    val (cor, fundo) = when (relato.status) {
        StatusRelato.ABERTO -> situacao.manutencao to situacao.fundoManutencao
        StatusRelato.CONFIRMADO -> situacao.parado to situacao.fundoParado
        StatusRelato.RESOLVIDO -> situacao.operando to situacao.fundoOperando
        StatusRelato.DESCARTADO -> MaterialTheme.colorScheme.onSurfaceVariant to MaterialTheme.colorScheme.surfaceVariant
    }
    Surface(shape = MaterialTheme.shapes.large, color = MaterialTheme.colorScheme.surface) {
        Column(Modifier.fillMaxWidth().padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                SimboloDaLinha(relato.local.linhaNumero, relato.local.linhaCor, tamanho = 26.dp)
                Text(relato.local.estacao, style = MaterialTheme.typography.titleMedium, modifier = Modifier.padding(start = 10.dp))
            }
            Espaco(4.dp)
            Text(
                "${relato.local.tipo.nome}: ${relato.local.localizacao.replaceFirstChar(Char::lowercase)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Espaco(10.dp)
            Text(relato.descricao, style = MaterialTheme.typography.bodyLarge)
            Espaco(12.dp)
            Etiqueta(relato.status.nome, cor, fundo, icone = null)
            Espaco(6.dp)
            Text(
                if (admin) "${relato.autor}, ${tempoDesde(relato.criadoEm)}" else "Enviado ${tempoDesde(relato.criadoEm)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            val acoes = acoesPara(relato, admin, aoEditar, aoApagar, aoTratar)
            if (acoes.isNotEmpty()) {
                Espaco(8.dp)
                FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    acoes.forEach { (rotulo, acao) ->
                        OutlinedButton(onClick = acao, shape = MaterialTheme.shapes.medium, modifier = Modifier.heightIn(min = 48.dp)) { Text(rotulo) }
                    }
                }
            }
        }
    }
}

private fun acoesPara(
    relato: Relato,
    admin: Boolean,
    aoEditar: () -> Unit,
    aoApagar: () -> Unit,
    aoTratar: (StatusRelato) -> Unit,
): List<Pair<String, () -> Unit>> {
    if (!admin) {
        if (relato.status != StatusRelato.ABERTO) return emptyList()
        return listOf("Corrigir" to aoEditar, "Apagar" to aoApagar)
    }
    return when (relato.status) {
        StatusRelato.ABERTO -> listOf("Confirmar" to { aoTratar(StatusRelato.CONFIRMADO) }, "Descartar" to { aoTratar(StatusRelato.DESCARTADO) })
        StatusRelato.CONFIRMADO -> listOf("Marcar como resolvido" to { aoTratar(StatusRelato.RESOLVIDO) })
        else -> listOf("Apagar" to aoApagar)
    }
}

@Composable
private fun DialogoDeEdicao(relato: Relato, aoFechar: () -> Unit, aoSalvar: (String) -> Unit) {
    var texto by remember { mutableStateOf(relato.descricao) }
    val valido = texto.trim().length in MINIMO_DE_CARACTERES..MAXIMO_DE_CARACTERES
    AlertDialog(
        onDismissRequest = aoFechar,
        title = { Text("Corrigir o aviso") },
        text = { CampoDeDescricao(texto) { texto = it } },
        confirmButton = { TextButton(onClick = { aoSalvar(texto) }, enabled = valido) { Text("Salvar") } },
        dismissButton = { TextButton(onClick = aoFechar) { Text("Cancelar") } },
    )
}

@Composable
private fun CampoDeDescricao(texto: String, aoMudar: (String) -> Unit) {
    OutlinedTextField(
        value = texto,
        onValueChange = { if (it.length <= MAXIMO_DE_CARACTERES) aoMudar(it) },
        label = { Text("O que aconteceu?") },
        supportingText = { Text("${texto.length} de $MAXIMO_DE_CARACTERES caracteres") },
        minLines = 4,
        keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Sentences),
        modifier = Modifier.fillMaxWidth(),
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NovoRelatoScreen(vm: NovoRelatoViewModel, aoVoltar: () -> Unit, aoEnviar: () -> Unit) {
    LaunchedEffect(vm.enviado) {
        if (vm.enviado) aoEnviar()
    }
    val equipamento = vm.equipamento
    val erro = vm.erro

    Column(Modifier.fillMaxSize().imePadding()) {
        TopAppBar(
            title = { Text("Avisar um problema") },
            navigationIcon = {
                IconButton(onClick = aoVoltar) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Voltar") }
            },
            colors = TopAppBarDefaults.topAppBarColors(containerColor = MaterialTheme.colorScheme.background),
            windowInsets = WindowInsets(0),
        )
        when {
            equipamento == null && erro != null -> SemConexao(erro, aoVoltar)
            equipamento == null -> Carregando()
            else -> Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 20.dp)) {
                Surface(shape = MaterialTheme.shapes.large, color = MaterialTheme.colorScheme.surface, modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(16.dp)) {
                        vm.estacao?.let { estacao ->
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                SimboloDaLinha(estacao.linha.numero, estacao.linha.cor, tamanho = 26.dp)
                                Text(estacao.nome, style = MaterialTheme.typography.titleLarge, modifier = Modifier.padding(start = 10.dp))
                            }
                            Espaco(6.dp)
                        }
                        Text(equipamento.tipo.nome, style = MaterialTheme.typography.titleMedium)
                        Text(equipamento.localizacao, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
                Espaco(20.dp)
                CampoDeDescricao(vm.descricao) { vm.descricao = it }
                Text(
                    "Conte o que você viu: parado, porta que não fecha, botão sem resposta. A operadora recebe o aviso na hora.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(top = 4.dp),
                )
                if (erro != null) {
                    Text(erro, color = MaterialTheme.colorScheme.error, modifier = Modifier.padding(top = 12.dp))
                }
                Espaco(20.dp)
                BotaoPrincipal("Enviar aviso", vm::enviar, carregando = vm.enviando, ativo = vm.podeEnviar)
                Espaco(24.dp)
            }
        }
    }
}
