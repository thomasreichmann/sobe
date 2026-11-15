package br.com.fiap.sobe.ui.estacoes

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.SearchOff
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.StarBorder
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.heading
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.dp
import br.com.fiap.sobe.data.EstacaoResumo
import br.com.fiap.sobe.data.Linha
import br.com.fiap.sobe.ui.componentes.AtualizarPeriodicamente
import br.com.fiap.sobe.ui.componentes.Aviso
import br.com.fiap.sobe.ui.componentes.Carregando
import br.com.fiap.sobe.ui.componentes.SemConexao
import br.com.fiap.sobe.ui.componentes.SemSinal
import br.com.fiap.sobe.ui.componentes.SimboloDaLinha
import br.com.fiap.sobe.ui.componentes.corDaLinha
import br.com.fiap.sobe.ui.tema.LocalSituacao
import kotlinx.coroutines.launch
import androidx.compose.runtime.rememberCoroutineScope

@Composable
fun EstacoesScreen(vm: EstacoesViewModel, aoAbrir: (Int) -> Unit, avisar: (String) -> Unit) {
    val escopo = rememberCoroutineScope()
    AtualizarPeriodicamente { vm.atualizar() }
    LaunchedEffect(vm.aviso) {
        vm.aviso?.let(avisar)
        vm.aviso = null
    }

    Column(Modifier.fillMaxSize()) {
        Text(
            "Estações",
            style = MaterialTheme.typography.headlineMedium,
            modifier = Modifier.padding(start = 20.dp, top = 20.dp, end = 20.dp).semantics { heading() },
        )
        vm.guardadoEm?.let { SemSinal(it, Modifier.padding(start = 20.dp, end = 20.dp, top = 12.dp)) }
        OutlinedTextField(
            value = vm.busca,
            onValueChange = { vm.busca = it },
            placeholder = { Text("Buscar estação") },
            leadingIcon = { Icon(Icons.Filled.Search, contentDescription = null) },
            trailingIcon = {
                if (vm.busca.isNotEmpty()) {
                    IconButton(onClick = { vm.busca = "" }) { Icon(Icons.Filled.Close, contentDescription = "Limpar a busca") }
                }
            },
            singleLine = true,
            keyboardOptions = KeyboardOptions(imeAction = ImeAction.Search),
            shape = MaterialTheme.shapes.medium,
            modifier = Modifier.fillMaxWidth().padding(horizontal = 20.dp, vertical = 12.dp),
        )
        Filtros(vm)

        val estacoes = vm.estacoes
        val erro = vm.erro
        val visiveis = vm.visiveis
        when {
            estacoes == null && erro != null -> SemConexao(erro, { escopo.launch { vm.atualizar() } })
            estacoes == null -> Carregando()
            visiveis.isEmpty() && vm.somenteFavoritas && vm.busca.isBlank() -> Aviso(
                Icons.Filled.StarBorder,
                "Você ainda não acompanha nenhuma estação",
                "Toque na estrela de uma estação para ser avisado quando um elevador dela parar.",
                acao = "Ver todas as estações",
                aoAgir = { vm.somenteFavoritas = false },
            )
            visiveis.isEmpty() -> Aviso(
                Icons.Filled.SearchOff,
                "Nenhuma estação encontrada",
                "Confira o nome digitado ou tire os filtros.",
                acao = "Limpar a busca e os filtros",
                aoAgir = {
                    vm.busca = ""
                    vm.linhaEscolhida = null
                    vm.somenteFavoritas = false
                },
            )
            else -> MapaDasLinhas(visiveis, aoAbrir, vm::alternarFavorita)
        }
    }
}

@Composable
private fun Filtros(vm: EstacoesViewModel) {
    Row(
        Modifier.horizontalScroll(rememberScrollState()).padding(horizontal = 20.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        FilterChip(
            selected = vm.somenteFavoritas,
            onClick = { vm.somenteFavoritas = !vm.somenteFavoritas },
            label = { Text("Que eu acompanho") },
            leadingIcon = { Icon(Icons.Filled.Star, contentDescription = null, modifier = Modifier.size(18.dp)) },
        )
        vm.linhas.forEach { linha ->
            FilterChip(
                selected = vm.linhaEscolhida == linha.id,
                onClick = { vm.linhaEscolhida = if (vm.linhaEscolhida == linha.id) null else linha.id },
                label = { Text("Linha ${linha.numero}") },
                leadingIcon = { SimboloDaLinha(linha.numero, linha.cor, tamanho = 20.dp) },
            )
        }
    }
}

@Composable
private fun MapaDasLinhas(estacoes: List<EstacaoResumo>, aoAbrir: (Int) -> Unit, aoAlternarFavorita: (EstacaoResumo) -> Unit) {
    val porLinha = estacoes.groupBy { it.linha }.toList()
    LazyColumn(
        contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 14.dp, bottom = 24.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        items(porLinha, key = { it.first.id }) { (linha, daLinha) ->
            Surface(shape = MaterialTheme.shapes.large, color = MaterialTheme.colorScheme.surface) {
                Column(Modifier.padding(vertical = 8.dp)) {
                    CabecalhoDaLinha(linha)
                    daLinha.forEachIndexed { indice, estacao ->
                        EstacaoNoTrilho(
                            estacao,
                            primeira = indice == 0,
                            ultima = indice == daLinha.lastIndex,
                            aoAbrir = { aoAbrir(estacao.id) },
                            aoAlternarFavorita = { aoAlternarFavorita(estacao) },
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun CabecalhoDaLinha(linha: Linha) {
    Row(
        Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 8.dp, bottom = 6.dp).semantics(mergeDescendants = true) { heading() },
        verticalAlignment = Alignment.CenterVertically,
    ) {
        SimboloDaLinha(linha.numero, linha.cor)
        Column(Modifier.padding(start = 12.dp)) {
            Text("Linha ${linha.numero} ${linha.nome}", style = MaterialTheme.typography.titleMedium)
            Text(linha.operadora, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}

private fun resumoDe(estacao: EstacaoResumo): String = when {
    !estacao.acessivel -> "Sem acesso por elevador"
    estacao.foraDeServico == 0 -> "Tudo funcionando"
    else -> "Elevadores funcionando, escada rolante parada"
}

@Composable
private fun EstacaoNoTrilho(
    estacao: EstacaoResumo,
    primeira: Boolean,
    ultima: Boolean,
    aoAbrir: () -> Unit,
    aoAlternarFavorita: () -> Unit,
) {
    val situacao = LocalSituacao.current
    val resumo = resumoDe(estacao)
    Row(
        Modifier.fillMaxWidth().height(IntrinsicSize.Min).clickable(onClickLabel = "ver os equipamentos", role = Role.Button, onClick = aoAbrir),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Trilho(
            cor = corDaLinha(estacao.linha.cor),
            acessivel = estacao.acessivel,
            primeira = primeira,
            ultima = ultima,
            modifier = Modifier.padding(start = 17.dp).width(28.dp).fillMaxHeight(),
        )
        Column(
            Modifier.weight(1f).padding(start = 13.dp, top = 12.dp, bottom = 12.dp)
                .clearAndSetSemantics { contentDescription = "${estacao.nome}. $resumo." },
        ) {
            Text(estacao.nome, style = MaterialTheme.typography.titleMedium)
            Text(
                resumo,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = if (estacao.acessivel) FontWeight.Normal else FontWeight.Bold,
                color = if (estacao.acessivel) MaterialTheme.colorScheme.onSurfaceVariant else situacao.parado,
            )
        }
        IconButton(onClick = aoAlternarFavorita, modifier = Modifier.padding(end = 6.dp)) {
            Icon(
                if (estacao.favorita) Icons.Filled.Star else Icons.Filled.StarBorder,
                contentDescription = if (estacao.favorita) "Deixar de acompanhar ${estacao.nome}" else "Acompanhar ${estacao.nome}",
                tint = if (estacao.favorita) situacao.favorita else MaterialTheme.colorScheme.outline,
            )
        }
    }
}

@Composable
private fun Trilho(cor: Color, acessivel: Boolean, primeira: Boolean, ultima: Boolean, modifier: Modifier) {
    val fundo = MaterialTheme.colorScheme.surface
    val parado = LocalSituacao.current.parado
    Canvas(modifier) {
        val centro = Offset(size.width / 2, size.height / 2)
        drawLine(
            cor,
            start = Offset(centro.x, if (primeira) centro.y else 0f),
            end = Offset(centro.x, if (ultima) centro.y else size.height),
            strokeWidth = 7.dp.toPx(),
        )
        if (acessivel) {
            drawCircle(fundo, radius = 9.dp.toPx(), center = centro)
            drawCircle(cor, radius = 9.dp.toPx(), center = centro, style = Stroke(width = 4.dp.toPx()))
        } else {
            drawCircle(fundo, radius = 14.dp.toPx(), center = centro)
            drawCircle(parado, radius = 12.dp.toPx(), center = centro)
            drawLine(
                Color.White,
                start = Offset(centro.x - 6.5.dp.toPx(), centro.y),
                end = Offset(centro.x + 6.5.dp.toPx(), centro.y),
                strokeWidth = 4.dp.toPx(),
            )
        }
    }
}
