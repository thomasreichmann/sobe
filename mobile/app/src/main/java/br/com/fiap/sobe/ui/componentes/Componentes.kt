package br.com.fiap.sobe.ui.componentes

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Accessible
import androidx.compose.material.icons.filled.Build
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.CloudOff
import androidx.compose.material.icons.filled.DoNotDisturbOn
import androidx.compose.material.icons.filled.Elevator
import androidx.compose.material.icons.filled.Escalator
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.graphics.toColorInt
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.repeatOnLifecycle
import br.com.fiap.sobe.data.StatusEquipamento
import br.com.fiap.sobe.data.TipoEquipamento
import br.com.fiap.sobe.ui.tema.LocalSituacao
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId
import kotlinx.coroutines.delay

fun corDaLinha(hex: String): Color = runCatching { Color(hex.toColorInt()) }.getOrDefault(Color.Gray)

val TipoEquipamento.nome
    get() = when (this) {
        TipoEquipamento.ELEVADOR -> "Elevador"
        TipoEquipamento.ESCADA_ROLANTE -> "Escada rolante"
        TipoEquipamento.PLATAFORMA -> "Plataforma elevatória"
    }

val TipoEquipamento.icone: ImageVector
    get() = when (this) {
        TipoEquipamento.ELEVADOR -> Icons.Filled.Elevator
        TipoEquipamento.ESCADA_ROLANTE -> Icons.Filled.Escalator
        TipoEquipamento.PLATAFORMA -> Icons.Filled.Accessible
    }

val StatusEquipamento.nome
    get() = when (this) {
        StatusEquipamento.OPERANDO -> "Funcionando"
        StatusEquipamento.PARADO -> "Parado"
        StatusEquipamento.MANUTENCAO -> "Em manutenção"
    }

fun tempoDesde(instante: String): String {
    val minutos = runCatching { Duration.between(Instant.parse(instante), Instant.now()).toMinutes() }.getOrDefault(0)
    return when {
        minutos < 1 -> "agora há pouco"
        minutos < 60 -> "há $minutos min"
        minutos < 60 * 24 -> "há ${minutos / 60} h"
        minutos < 60 * 48 -> "há 1 dia"
        else -> "há ${minutos / (60 * 24)} dias"
    }
}

fun duracao(minutos: Int): String {
    val horas = minutos / 60
    val resto = minutos % 60
    return when {
        horas == 0 -> "$resto min"
        resto == 0 -> "$horas h"
        else -> "$horas h $resto min"
    }
}

@Composable
fun AtualizarPeriodicamente(intervaloEmMs: Long = 6000, acao: suspend () -> Unit) {
    val dono = LocalLifecycleOwner.current
    LaunchedEffect(dono) {
        dono.repeatOnLifecycle(Lifecycle.State.RESUMED) {
            while (true) {
                acao()
                delay(intervaloEmMs)
            }
        }
    }
}

@Composable
fun SimboloDaLinha(numero: Int, cor: String, modifier: Modifier = Modifier, tamanho: Dp = 30.dp) {
    val fundo = corDaLinha(cor)
    Box(
        modifier.size(tamanho).clip(CircleShape).background(fundo).clearAndSetSemantics {},
        contentAlignment = Alignment.Center,
    ) {
        Text(
            numero.toString(),
            color = if (fundo.luminance() > 0.5f) Color(0xFF0E1B2C) else Color.White,
            fontWeight = FontWeight.Bold,
            fontSize = (tamanho.value * 0.55f).sp,
            lineHeight = (tamanho.value * 0.6f).sp,
        )
    }
}

@Composable
fun Situacao(status: StatusEquipamento, modifier: Modifier = Modifier) {
    val cores = LocalSituacao.current
    val (cor, fundo, icone) = when (status) {
        StatusEquipamento.OPERANDO -> Triple(cores.operando, cores.fundoOperando, Icons.Filled.CheckCircle)
        StatusEquipamento.PARADO -> Triple(cores.parado, cores.fundoParado, Icons.Filled.DoNotDisturbOn)
        StatusEquipamento.MANUTENCAO -> Triple(cores.manutencao, cores.fundoManutencao, Icons.Filled.Build)
    }
    Etiqueta(status.nome, cor, fundo, icone, modifier)
}

@Composable
fun Etiqueta(texto: String, cor: Color, fundo: Color, icone: ImageVector?, modifier: Modifier = Modifier) {
    Row(
        modifier.clip(MaterialTheme.shapes.small).background(fundo).padding(horizontal = 10.dp, vertical = 5.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        if (icone != null) Icon(icone, contentDescription = null, tint = cor, modifier = Modifier.size(18.dp))
        Text(texto, color = cor, style = MaterialTheme.typography.labelMedium)
    }
}

@Composable
fun BotaoPrincipal(texto: String, aoClicar: () -> Unit, modifier: Modifier = Modifier, carregando: Boolean = false, ativo: Boolean = true) {
    Button(
        onClick = aoClicar,
        enabled = ativo && !carregando,
        shape = MaterialTheme.shapes.medium,
        modifier = modifier.fillMaxWidth().heightIn(min = 54.dp),
    ) {
        if (carregando) {
            CircularProgressIndicator(Modifier.size(22.dp), strokeWidth = 2.5.dp, color = MaterialTheme.colorScheme.onSurface)
        } else {
            Text(texto)
        }
    }
}

@Composable
fun Carregando(modifier: Modifier = Modifier) {
    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        CircularProgressIndicator()
    }
}

@Composable
fun Aviso(
    icone: ImageVector,
    titulo: String,
    texto: String,
    modifier: Modifier = Modifier,
    acao: String? = null,
    aoAgir: () -> Unit = {},
) {
    Column(
        modifier.fillMaxSize().padding(horizontal = 32.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Icon(icone, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.size(44.dp))
        Spacer(Modifier.height(16.dp))
        Text(titulo, style = MaterialTheme.typography.titleLarge, textAlign = TextAlign.Center)
        Spacer(Modifier.height(8.dp))
        Text(texto, color = MaterialTheme.colorScheme.onSurfaceVariant, textAlign = TextAlign.Center)
        if (acao != null) {
            Spacer(Modifier.height(20.dp))
            OutlinedButton(onClick = aoAgir, shape = MaterialTheme.shapes.medium, modifier = Modifier.heightIn(min = 48.dp)) {
                Text(acao)
            }
        }
    }
}

@Composable
fun SemConexao(mensagem: String, aoTentarDeNovo: () -> Unit, modifier: Modifier = Modifier) {
    Aviso(Icons.Filled.CloudOff, "Não deu para carregar", mensagem, modifier, acao = "Tentar de novo", aoAgir = aoTentarDeNovo)
}

fun momento(milissegundos: Long): String {
    val quando = Instant.ofEpochMilli(milissegundos).atZone(ZoneId.systemDefault())
    val hora = "%02d:%02d".format(quando.hour, quando.minute)
    if (quando.toLocalDate() == LocalDate.now()) return "das $hora"
    return "de %02d/%02d às $hora".format(quando.dayOfMonth, quando.monthValue)
}

@Composable
fun SemSinal(guardadoEm: Long, modifier: Modifier = Modifier) {
    val cores = LocalSituacao.current
    Row(
        modifier.fillMaxWidth().clip(MaterialTheme.shapes.medium).background(cores.fundoManutencao).padding(12.dp)
            .semantics(mergeDescendants = true) { liveRegion = LiveRegionMode.Polite },
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Icon(Icons.Filled.CloudOff, contentDescription = null, tint = cores.manutencao, modifier = Modifier.size(22.dp))
        Text(
            "Sem conexão. Você está vendo a situação ${momento(guardadoEm)}.",
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.padding(start = 10.dp),
        )
    }
}

@Composable
fun Espaco(altura: Dp = 0.dp, largura: Dp = 0.dp) {
    Spacer(Modifier.height(altura).width(largura))
}
