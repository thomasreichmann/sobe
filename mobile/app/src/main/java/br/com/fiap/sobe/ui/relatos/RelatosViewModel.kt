package br.com.fiap.sobe.ui.relatos

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import br.com.fiap.sobe.data.Equipamento
import br.com.fiap.sobe.data.EstacaoDetalhe
import br.com.fiap.sobe.data.Relato
import br.com.fiap.sobe.data.Repositorio
import br.com.fiap.sobe.data.Resultado
import br.com.fiap.sobe.data.StatusRelato
import kotlinx.coroutines.launch

const val MINIMO_DE_CARACTERES = 5
const val MAXIMO_DE_CARACTERES = 500

class RelatosViewModel(private val repositorio: Repositorio) : ViewModel() {
    var relatos by mutableStateOf<List<Relato>?>(null)
        private set
    var erro by mutableStateOf<String?>(null)
        private set
    var aviso by mutableStateOf<String?>(null)

    suspend fun atualizar() {
        when (val resultado = repositorio.relatos()) {
            is Resultado.Ok -> {
                relatos = resultado.valor
                erro = null
            }
            is Resultado.Falha -> erro = resultado.mensagem
        }
    }

    fun editar(relato: Relato, descricao: String) = executar("Aviso corrigido.") { repositorio.editarRelato(relato.id, descricao) }

    fun apagar(relato: Relato) = executar("Aviso apagado.") { repositorio.apagarRelato(relato.id) }

    fun tratar(relato: Relato, status: StatusRelato) = executar(null) { repositorio.tratarRelato(relato.id, status) }

    private fun executar(mensagem: String?, pedido: suspend () -> Resultado<*>) {
        viewModelScope.launch {
            when (val resultado = pedido()) {
                is Resultado.Ok -> {
                    aviso = mensagem
                    atualizar()
                }
                is Resultado.Falha -> aviso = resultado.mensagem
            }
        }
    }
}

class NovoRelatoViewModel(estado: SavedStateHandle, private val repositorio: Repositorio) : ViewModel() {
    private val equipamentoId: Int = checkNotNull(estado["equipamentoId"])

    var equipamento by mutableStateOf<Equipamento?>(null)
        private set
    var estacao by mutableStateOf<EstacaoDetalhe?>(null)
        private set
    var descricao by mutableStateOf("")
    var enviando by mutableStateOf(false)
        private set
    var enviado by mutableStateOf(false)
        private set
    var erro by mutableStateOf<String?>(null)
        private set

    val podeEnviar get() = descricao.trim().length in MINIMO_DE_CARACTERES..MAXIMO_DE_CARACTERES

    init {
        viewModelScope.launch {
            val encontrado = repositorio.equipamento(equipamentoId)
            if (encontrado is Resultado.Falha) {
                erro = encontrado.mensagem
                return@launch
            }
            equipamento = (encontrado as Resultado.Ok).valor
            (repositorio.estacao(encontrado.valor.estacaoId) as? Resultado.Ok)?.let { estacao = it.valor }
        }
    }

    fun enviar() {
        if (enviando || !podeEnviar) return
        enviando = true
        erro = null
        viewModelScope.launch {
            when (val resultado = repositorio.relatar(equipamentoId, descricao)) {
                is Resultado.Ok -> enviado = true
                is Resultado.Falha -> erro = resultado.mensagem
            }
            enviando = false
        }
    }
}
