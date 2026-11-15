package br.com.fiap.sobe.ui.estacoes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.DoNotDisturbOn
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.StarBorder
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import br.com.fiap.sobe.data.Equipamento
import br.com.fiap.sobe.data.EstacaoDetalhe
import br.com.fiap.sobe.data.StatusEquipamento
import br.com.fiap.sobe.ui.componentes.AtualizarPeriodicamente
import br.com.fiap.sobe.ui.componentes.Carregando
import br.com.fiap.sobe.ui.componentes.Espaco
import br.com.fiap.sobe.ui.componentes.SemConexao
import br.com.fiap.sobe.ui.componentes.SemSinal
import br.com.fiap.sobe.ui.componentes.SimboloDaLinha
import br.com.fiap.sobe.ui.componentes.Situacao
import br.com.fiap.sobe.ui.componentes.icone
import br.com.fiap.sobe.ui.componentes.nome
import br.com.fiap.sobe.ui.componentes.tempoDesde
import br.com.fiap.sobe.ui.tema.LocalSituacao
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EstacaoScreen(
    vm: EstacaoViewModel,
    admin: Boolean,
    aoVoltar: () -> Unit,
    aoAbrirEstacao: (Int) -> Unit,
    aoRelatar: (Int) -> Unit,
    avisar: (String) -> Unit,
) {
    val escopo = rememberCoroutineScope()
    AtualizarPeriodicamente { vm.atualizar() }
    LaunchedEffect(vm.aviso) {
        vm.aviso?.let(avisar)
        vm.aviso = null
    }
    val estacao = vm.estacao
    val erro = vm.erro

    Column(Modifier.fillMaxSize()) {
        TopAppBar(
            title = {},
            navigationIcon = {
                IconButton(onClick = aoVoltar) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Voltar") }
            },
            actions = {
                if (estacao != null) {
                    IconButton(onClick = vm::alternarFavorita) {
                        Icon(
                            if (estacao.favorita) Icons.Filled.Star else Icons.Filled.StarBorder,
                            contentDescription = if (estacao.favorita) "Deixar de acompanhar esta estação" else "Acompanhar esta estação",
                            tint = if (estacao.favorita) LocalSituacao.current.favorita else MaterialTheme.colorScheme.onSurface,
                        )
                    }
                }
            },
            colors = TopAppBarDefaults.topAppBarColors(containerColor = MaterialTheme.colorScheme.background),
            windowInsets = WindowInsets(0),
        )
        when {
            estacao == null && erro != null -> SemConexao(erro, { escopo.launch { vm.atualizar() } })
            estacao == null -> Carregando()
            else -> LazyColumn(contentPadding = PaddingValues(start = 20.dp, end = 20.dp, bottom = 28.dp)) {
                vm.guardadoEm?.let { guardadoEm -> item { SemSinal(guardadoEm, Modifier.padding(bottom = 14.dp)) } }
                item { Cabecalho(estacao) }
                item { Veredito(estacao, aoAbrirEstacao) }
                item {
                    Text(
                        "Equipamentos",
                        style = MaterialTheme.typography.titleLarge,
                        modifier = Modifier.padding(top = 28.dp, bottom = 10.dp).semantics { heading() },
                    )
                }
                item {
                    if (estacao.equipamentos.isEmpty()) {
                        Text(
                            "Os equipamentos desta estação ainda não foram carregados neste aparelho. Eles aparecem quando a conexão voltar.",
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                    Surface(shape = MaterialTheme.shapes.large, color = MaterialTheme.colorScheme.surface) {
                        Column {
                            estacao.equipamentos.forEachIndexed { indice, equipamento ->
                                if (indice > 0) HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                                LinhaDoEquipamento(equipamento, admin, aoRelatar, vm::mudarSituacao)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun Cabecalho(estacao: EstacaoDetalhe) {
    Column(Modifier.semantics(mergeDescendants = true) { heading() }) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            SimboloDaLinha(estacao.linha.numero, estacao.linha.cor, tamanho = 26.dp)
            Text(
                "Linha ${estacao.linha.numero} ${estacao.linha.nome}",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(start = 10.dp),
            )
        }
        Espaco(8.dp)
        Text(estacao.nome, style = MaterialTheme.typography.displaySmall)
    }
}

@Composable
private fun Veredito(estacao: EstacaoDetalhe, aoAbrirEstacao: (Int) -> Unit) {
    val situacao = LocalSituacao.current
    val cor = if (estacao.acessivel) situacao.operando else situacao.parado
    val fundo = if (estacao.acessivel) situacao.fundoOperando else situacao.fundoParado
    Column(Modifier.padding(top = 18.dp).fillMaxWidth().clip(MaterialTheme.shapes.large).background(fundo).padding(18.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                if (estacao.acessivel) Icons.Filled.CheckCircle else Icons.Filled.DoNotDisturbOn,
                contentDescription = null,
                tint = cor,
                modifier = Modifier.size(30.dp),
            )
            Text(
                if (estacao.acessivel) "Dá para usar os elevadores" else "Sem acesso por elevador agora",
                style = MaterialTheme.typography.titleLarge,
                color = cor,
                modifier = Modifier.padding(start = 12.dp),
            )
        }
        if (estacao.acessivel) return@Column

        Espaco(12.dp)
        val alternativa = estacao.alternativa
        if (alternativa == null) {
            Text("Nenhuma outra estação desta linha está com todos os elevadores funcionando.", color = MaterialTheme.colorScheme.onSurface)
            return@Column
        }
        val distancia = if (alternativa.estacoesDeDistancia == 1) "a 1 estação daqui" else "a ${alternativa.estacoesDeDistancia} estações daqui"
        Text(
            "A estação acessível mais próxima nesta linha é ${alternativa.nome}, $distancia.",
            color = MaterialTheme.colorScheme.onSurface,
        )
        Espaco(14.dp)
        OutlinedButton(
            onClick = { aoAbrirEstacao(alternativa.id) },
            shape = MaterialTheme.shapes.medium,
            modifier = Modifier.heightIn(min = 48.dp),
        ) {
            Text("Ver ${alternativa.nome}", color = MaterialTheme.colorScheme.onSurface)
        }
    }
}

@Composable
private fun LinhaDoEquipamento(
    equipamento: Equipamento,
    admin: Boolean,
    aoRelatar: (Int) -> Unit,
    aoMudarSituacao: (Equipamento, StatusEquipamento) -> Unit,
) {
    Column(Modifier.fillMaxWidth().padding(16.dp)) {
        Row(verticalAlignment = Alignment.Top) {
            Icon(
                equipamento.tipo.icone,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 2.dp).size(26.dp),
            )
            Column(Modifier.weight(1f).padding(start = 14.dp)) {
                Text(equipamento.tipo.nome, style = MaterialTheme.typography.titleMedium)
                Text(equipamento.localizacao, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
        Column(Modifier.padding(start = 40.dp)) {
            Espaco(10.dp)
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                Situacao(equipamento.status)
                if (equipamento.status != StatusEquipamento.OPERANDO) {
                    Text(tempoDesde(equipamento.statusDesde), style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
            equipamento.alerta?.let {
                Espaco(8.dp)
                Text(it.motivo, style = MaterialTheme.typography.bodyMedium)
            }
            Espaco(12.dp)
            if (admin) {
                MenuDeSituacao(equipamento, aoMudarSituacao)
            } else {
                OutlinedButton(
                    onClick = { aoRelatar(equipamento.id) },
                    shape = MaterialTheme.shapes.medium,
                    modifier = Modifier.heightIn(min = 48.dp),
                ) {
                    Text("Avisar um problema")
                }
            }
        }
    }
}

@Composable
private fun MenuDeSituacao(equipamento: Equipamento, aoMudar: (Equipamento, StatusEquipamento) -> Unit) {
    var aberto by remember { mutableStateOf(false) }
    Box {
        OutlinedButton(onClick = { aberto = true }, shape = MaterialTheme.shapes.medium, modifier = Modifier.heightIn(min = 48.dp)) {
            Text("Mudar a situação")
        }
        DropdownMenu(expanded = aberto, onDismissRequest = { aberto = false }) {
            StatusEquipamento.entries.filter { it != equipamento.status }.forEach { status ->
                DropdownMenuItem(
                    text = { Text(status.nome) },
                    onClick = {
                        aberto = false
                        aoMudar(equipamento, status)
                    },
                )
            }
        }
    }
}
