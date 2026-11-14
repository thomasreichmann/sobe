package br.com.fiap.sobe.data

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface SobeApi {
    @POST("api/auth/login")
    suspend fun entrar(@Body credenciais: Credenciais): Login

    @POST("api/auth/registrar")
    suspend fun registrar(@Body usuario: NovoUsuario): Login

    @GET("api/linhas")
    suspend fun linhas(): List<Linha>

    @GET("api/estacoes")
    suspend fun estacoes(): List<EstacaoResumo>

    @GET("api/estacoes/{id}")
    suspend fun estacao(@Path("id") id: Int): EstacaoDetalhe

    @PUT("api/favoritos/{estacaoId}")
    suspend fun acompanhar(@Path("estacaoId") estacaoId: Int): Response<Unit>

    @DELETE("api/favoritos/{estacaoId}")
    suspend fun deixarDeAcompanhar(@Path("estacaoId") estacaoId: Int): Response<Unit>

    @GET("api/alertas")
    suspend fun alertas(@Query("somenteFavoritas") somenteFavoritas: Boolean): List<Alerta>

    @GET("api/relatos")
    suspend fun relatos(): List<Relato>

    @POST("api/relatos")
    suspend fun relatar(@Body relato: NovoRelato): Relato

    @PUT("api/relatos/{id}")
    suspend fun editarRelato(@Path("id") id: Int, @Body texto: TextoDoRelato): Relato

    @PUT("api/relatos/{id}/status")
    suspend fun tratarRelato(@Path("id") id: Int, @Body situacao: SituacaoDoRelato): Relato

    @DELETE("api/relatos/{id}")
    suspend fun apagarRelato(@Path("id") id: Int): Response<Unit>

    @GET("api/equipamentos/{id}")
    suspend fun equipamento(@Path("id") id: Int): Equipamento

    @PUT("api/equipamentos/{id}")
    suspend fun atualizarEquipamento(@Path("id") id: Int, @Body situacao: SituacaoDoEquipamento): Equipamento

    @GET("api/painel")
    suspend fun painel(): Painel
}
