package br.com.fiap.sobe.ui.estacoes

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import br.com.fiap.sobe.data.Equipamento
import br.com.fiap.sobe.data.EstacaoDetalhe
import br.com.fiap.sobe.data.EstacaoResumo
import br.com.fiap.sobe.data.Linha
import br.com.fiap.sobe.data.Repositorio
import br.com.fiap.sobe.data.Resultado
import br.com.fiap.sobe.data.StatusEquipamento
import java.text.Normalizer
import kotlinx.coroutines.launch

private val ACENTOS = Regex("\\p{Mn}+")

private fun String.semAcento() = Normalizer.normalize(trim().lowercase(), Normalizer.Form.NFD).replace(ACENTOS, "")

class EstacoesViewModel(private val repositorio: Repositorio) : ViewModel() {
    var busca by mutableStateOf("")
    var linhaEscolhida by mutableStateOf<Int?>(null)
    var somenteFavoritas by mutableStateOf(false)
    var estacoes by mutableStateOf<List<EstacaoResumo>?>(null)
        private set
    var guardadoEm by mutableStateOf<Long?>(null)
        private set
    var erro by mutableStateOf<String?>(null)
        private set
    var aviso by mutableStateOf<String?>(null)

    val linhas: List<Linha>
        get() = estacoes.orEmpty().map { it.linha }.distinctBy { it.id }.sortedBy { it.numero }

    val visiveis: List<EstacaoResumo>
        get() {
            val termo = busca.semAcento()
            return estacoes.orEmpty().filter {
                (linhaEscolhida == null || it.linha.id == linhaEscolhida) &&
                    (!somenteFavoritas || it.favorita) &&
                    (termo.isEmpty() || termo in it.nome.semAcento())
            }
        }

    suspend fun atualizar() {
        when (val resultado = repositorio.estacoes()) {
            is Resultado.Ok -> {
                estacoes = resultado.valor
                guardadoEm = resultado.guardadoEm
                erro = null
            }
            is Resultado.Falha -> erro = resultado.mensagem
        }
    }

    fun alternarFavorita(estacao: EstacaoResumo) {
        val nova = !estacao.favorita
        marcar(estacao.id, nova)
        viewModelScope.launch {
            val resultado = repositorio.acompanhar(estacao.id, nova)
            if (resultado is Resultado.Falha) {
                marcar(estacao.id, !nova)
                aviso = resultado.mensagem
            }
        }
    }

    private fun marcar(id: Int, favorita: Boolean) {
        estacoes = estacoes?.map { if (it.id == id) it.copy(favorita = favorita) else it }
    }
}

class EstacaoViewModel(estado: SavedStateHandle, private val repositorio: Repositorio) : ViewModel() {
    private val id: Int = checkNotNull(estado["id"])

    var estacao by mutableStateOf<EstacaoDetalhe?>(null)
        private set
    var guardadoEm by mutableStateOf<Long?>(null)
        private set
    var erro by mutableStateOf<String?>(null)
        private set
    var aviso by mutableStateOf<String?>(null)

    suspend fun atualizar() {
        when (val resultado = repositorio.estacao(id)) {
            is Resultado.Ok -> {
                estacao = resultado.valor
                guardadoEm = resultado.guardadoEm
                erro = null
            }
            is Resultado.Falha -> erro = resultado.mensagem
        }
    }

    fun alternarFavorita() {
        val atual = estacao ?: return
        val nova = !atual.favorita
        estacao = atual.copy(favorita = nova)
        viewModelScope.launch {
            val resultado = repositorio.acompanhar(id, nova)
            if (resultado is Resultado.Falha) {
                estacao = estacao?.copy(favorita = !nova)
                aviso = resultado.mensagem
            } else {
                aviso = if (nova) "Você vai ser avisado das paradas em ${atual.nome}." else "Você deixou de acompanhar ${atual.nome}."
            }
        }
    }

    fun mudarSituacao(equipamento: Equipamento, status: StatusEquipamento) {
        viewModelScope.launch {
            when (val resultado = repositorio.mudarSituacao(equipamento, status)) {
                is Resultado.Ok -> atualizar()
                is Resultado.Falha -> aviso = resultado.mensagem
            }
        }
    }
}
