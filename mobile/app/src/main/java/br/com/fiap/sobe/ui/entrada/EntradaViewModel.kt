package br.com.fiap.sobe.ui.entrada

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import br.com.fiap.sobe.data.Login
import br.com.fiap.sobe.data.Repositorio
import br.com.fiap.sobe.data.Resultado
import kotlinx.coroutines.launch

class EntradaViewModel(private val repositorio: Repositorio) : ViewModel() {
    var nome by mutableStateOf("")
    var email by mutableStateOf("")
    var senha by mutableStateOf("")
    var carregando by mutableStateOf(false)
        private set
    var erro by mutableStateOf<String?>(null)
        private set

    val servidor get() = repositorio.servidor

    fun trocarServidor(endereco: String) = repositorio.trocarServidor(endereco)

    fun limparErro() {
        erro = null
    }

    fun entrar() {
        if (email.isBlank() || senha.isBlank()) {
            erro = "Preencha o e-mail e a senha."
            return
        }
        enviar { repositorio.entrar(email, senha) }
    }

    fun criarConta() {
        val problema = when {
            nome.trim().length < 2 -> "Escreva seu nome."
            !email.contains('@') -> "Confira o e-mail."
            senha.length < 8 || senha.none(Char::isLetter) || senha.none(Char::isDigit) ->
                "A senha precisa de pelo menos 8 caracteres, com letra e número."
            else -> null
        }
        if (problema != null) {
            erro = problema
            return
        }
        enviar { repositorio.registrar(nome, email, senha) }
    }

    private fun limparCampos() {
        nome = ""
        email = ""
        senha = ""
    }

    private fun enviar(pedido: suspend () -> Resultado<Login>) {
        if (carregando) return
        carregando = true
        erro = null
        viewModelScope.launch {
            when (val resultado = pedido()) {
                is Resultado.Falha -> erro = resultado.mensagem
                is Resultado.Ok -> limparCampos()
            }
            carregando = false
        }
    }
}
