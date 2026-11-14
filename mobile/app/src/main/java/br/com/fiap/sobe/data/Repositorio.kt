package br.com.fiap.sobe.data

import com.google.gson.Gson
import com.google.gson.JsonObject
import java.io.IOException
import java.util.concurrent.TimeUnit
import kotlin.math.abs
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import okhttp3.OkHttpClient
import retrofit2.HttpException
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

sealed interface Resultado<out T> {
    data class Ok<T>(val valor: T, val guardadoEm: Long? = null) : Resultado<T>
    data class Falha(val mensagem: String) : Resultado<Nothing>
}

private data class Guardado<T>(val valor: T, val em: Long)

class Repositorio(private val sessao: Sessao, private val gson: Gson, private val cache: CacheDao) {
    private val emSegundoPlano = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    private val http = OkHttpClient.Builder()
        .connectTimeout(8, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .addInterceptor { cadeia ->
            val pedido = cadeia.request().newBuilder()
            sessao.token?.let { pedido.header("Authorization", "Bearer $it") }
            cadeia.proceed(pedido.build())
        }
        .build()

    private var api = criarApi()

    val servidor get() = sessao.servidor

    fun trocarServidor(endereco: String): Boolean {
        val limpo = endereco.trim().trimEnd('/') + "/"
        if (!limpo.startsWith("http://") && !limpo.startsWith("https://")) return false
        sessao.servidor = limpo
        api = criarApi()
        return true
    }

    suspend fun entrar(email: String, senha: String) =
        chamar { api.entrar(Credenciais(email.trim(), senha)) }.tambem(sessao::abrir)

    suspend fun registrar(nome: String, email: String, senha: String) =
        chamar { api.registrar(NovoUsuario(nome.trim(), email.trim(), senha)) }.tambem(sessao::abrir)

    fun sair() {
        sessao.encerrar()
        emSegundoPlano.launch { cache.esvaziar() }
    }

    suspend fun estacoes() = chamar(::estacoesGuardadas) {
        val agora = System.currentTimeMillis()
        api.estacoes().also { lista -> cache.guardarEstacoes(lista.map { it.paraGuardar(agora) }) }
    }

    suspend fun estacao(id: Int) = chamar({ estacaoGuardada(id) }) {
        val agora = System.currentTimeMillis()
        api.estacao(id).also { detalhe -> cache.guardarEquipamentos(id, detalhe.equipamentos.map { it.paraGuardar(agora) }) }
    }

    suspend fun acompanhar(estacaoId: Int, acompanhar: Boolean) = chamar {
        val resposta = if (acompanhar) api.acompanhar(estacaoId) else api.deixarDeAcompanhar(estacaoId)
        resposta.exigirSucesso()
    }

    suspend fun alertas(somenteFavoritas: Boolean) = chamar { api.alertas(somenteFavoritas) }

    suspend fun relatos() = chamar { api.relatos() }

    suspend fun relatar(equipamentoId: Int, descricao: String) = chamar { api.relatar(NovoRelato(equipamentoId, descricao.trim())) }

    suspend fun editarRelato(id: Int, descricao: String) = chamar { api.editarRelato(id, TextoDoRelato(descricao.trim())) }

    suspend fun tratarRelato(id: Int, status: StatusRelato) = chamar { api.tratarRelato(id, SituacaoDoRelato(status)) }

    suspend fun apagarRelato(id: Int) = chamar { api.apagarRelato(id).exigirSucesso() }

    suspend fun equipamento(id: Int) = chamar { api.equipamento(id) }

    suspend fun mudarSituacao(equipamento: Equipamento, status: StatusEquipamento) =
        chamar { api.atualizarEquipamento(equipamento.id, SituacaoDoEquipamento(equipamento.localizacao, status)) }

    suspend fun painel() = chamar { api.painel() }

    private fun criarApi(): SobeApi = Retrofit.Builder()
        .baseUrl(sessao.servidor)
        .client(http)
        .addConverterFactory(GsonConverterFactory.create(gson))
        .build()
        .create(SobeApi::class.java)

    private suspend fun estacoesGuardadas(): Guardado<List<EstacaoResumo>>? {
        val guardadas = cache.estacoes()
        if (guardadas.isEmpty()) return null
        return Guardado(guardadas.map { it.paraResumo() }, guardadas.minOf { it.guardadaEm })
    }

    private suspend fun estacaoGuardada(id: Int): Guardado<EstacaoDetalhe>? {
        val estacao = cache.estacao(id) ?: return null
        val equipamentos = cache.equipamentos(id)
        val detalhe = EstacaoDetalhe(
            estacao.id, estacao.nome, estacao.linha(), estacao.acessivel, estacao.favorita,
            equipamentos.map { it.paraEquipamento() }, alternativaGuardada(estacao),
        )
        return Guardado(detalhe, equipamentos.minOfOrNull { it.guardadoEm } ?: estacao.guardadaEm)
    }

    private suspend fun alternativaGuardada(estacao: EstacaoGuardada): Alternativa? {
        if (estacao.acessivel) return null
        val maisProxima = cache.estacoes()
            .filter { it.linhaId == estacao.linhaId && it.id != estacao.id && it.acessivel }
            .minWithOrNull(compareBy({ abs(it.ordem - estacao.ordem) }, { it.ordem }))
            ?: return null
        return Alternativa(maisProxima.id, maisProxima.nome, abs(maisProxima.ordem - estacao.ordem))
    }

    private suspend fun <T> chamar(guardado: (suspend () -> Guardado<T>?)? = null, pedido: suspend () -> T): Resultado<T> = try {
        Resultado.Ok(pedido())
    } catch (erro: HttpException) {
        if (erro.code() == 401 && sessao.token != null) sair()
        Resultado.Falha(mensagemDe(erro))
    } catch (erro: IOException) {
        val ultimo = guardado?.invoke()
        if (ultimo != null) Resultado.Ok(ultimo.valor, ultimo.em) else Resultado.Falha("Sem conexão com o servidor. Confira a internet e tente de novo.")
    }

    private fun mensagemDe(erro: HttpException): String {
        val corpo = runCatching { gson.fromJson(erro.response()?.errorBody()?.string(), JsonObject::class.java) }.getOrNull()
        val validacao = corpo?.getAsJsonObject("errors")?.entrySet()?.firstOrNull()?.value?.asJsonArray?.firstOrNull()?.asString
        val detalhe = corpo?.get("detail")?.takeIf { !it.isJsonNull }?.asString
        return validacao ?: detalhe ?: when (erro.code()) {
            401 -> "Sua sessão terminou. Entre de novo."
            403 -> "Seu perfil não tem permissão para isso."
            429 -> "Muitas tentativas. Espere um minuto e tente de novo."
            else -> "O servidor respondeu com erro ${erro.code()}. Tente de novo."
        }
    }

    private fun Response<Unit>.exigirSucesso() {
        if (!isSuccessful) throw HttpException(this)
    }

    private inline fun <T> Resultado<T>.tambem(acao: (T) -> Unit): Resultado<T> {
        if (this is Resultado.Ok) acao(valor)
        return this
    }
}
