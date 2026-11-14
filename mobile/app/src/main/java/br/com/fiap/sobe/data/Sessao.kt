package br.com.fiap.sobe.data

import android.content.Context
import androidx.core.content.edit
import br.com.fiap.sobe.BuildConfig
import com.google.gson.Gson
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

class Sessao(contexto: Context, private val gson: Gson) {
    private val preferencias = contexto.getSharedPreferences("sessao", Context.MODE_PRIVATE)
    private val usuarioAtual = MutableStateFlow(lerUsuario())

    val usuario: StateFlow<Usuario?> = usuarioAtual

    val token: String? get() = preferencias.getString(TOKEN, null)

    var servidor: String
        get() = preferencias.getString(SERVIDOR, null) ?: BuildConfig.SERVIDOR_PADRAO
        set(valor) = preferencias.edit { putString(SERVIDOR, valor) }

    fun abrir(login: Login) {
        preferencias.edit {
            putString(TOKEN, login.token)
            putString(USUARIO, gson.toJson(login.usuario))
        }
        usuarioAtual.value = login.usuario
    }

    fun encerrar() {
        preferencias.edit {
            remove(TOKEN)
            remove(USUARIO)
        }
        usuarioAtual.value = null
    }

    private fun lerUsuario(): Usuario? {
        val salvo = preferencias.getString(USUARIO, null) ?: return null
        return runCatching { gson.fromJson(salvo, Usuario::class.java) }.getOrNull()
    }

    private companion object {
        const val TOKEN = "token"
        const val USUARIO = "usuario"
        const val SERVIDOR = "servidor"
    }
}
