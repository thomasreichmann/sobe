package br.com.fiap.sobe.data

enum class Perfil { ADMIN, CIDADAO }

enum class TipoEquipamento { ELEVADOR, ESCADA_ROLANTE, PLATAFORMA }

enum class StatusEquipamento { OPERANDO, PARADO, MANUTENCAO }

enum class StatusRelato { ABERTO, CONFIRMADO, RESOLVIDO, DESCARTADO }

data class Usuario(val id: Int, val nome: String, val email: String, val perfil: Perfil) {
    val admin get() = perfil == Perfil.ADMIN
}

data class Login(val token: String, val usuario: Usuario)

data class Linha(val id: Int, val numero: Int, val nome: String, val cor: String, val operadora: String)

data class EstacaoResumo(
    val id: Int,
    val nome: String,
    val ordem: Int,
    val linha: Linha,
    val equipamentos: Int,
    val foraDeServico: Int,
    val acessivel: Boolean,
    val favorita: Boolean,
)

data class AlertaDoEquipamento(val id: Int, val motivo: String, val abertoEm: String)

data class Equipamento(
    val id: Int,
    val estacaoId: Int,
    val tipo: TipoEquipamento,
    val localizacao: String,
    val status: StatusEquipamento,
    val statusDesde: String,
    val alerta: AlertaDoEquipamento?,
)

data class Alternativa(val id: Int, val nome: String, val estacoesDeDistancia: Int)

data class EstacaoDetalhe(
    val id: Int,
    val nome: String,
    val linha: Linha,
    val acessivel: Boolean,
    val favorita: Boolean,
    val equipamentos: List<Equipamento>,
    val alternativa: Alternativa?,
)

data class Local(
    val equipamentoId: Int,
    val tipo: TipoEquipamento,
    val localizacao: String,
    val estacaoId: Int,
    val estacao: String,
    val linhaNumero: Int,
    val linhaNome: String,
    val linhaCor: String,
)

data class Alerta(val id: Int, val motivo: String, val abertoEm: String, val local: Local)

data class Relato(
    val id: Int,
    val descricao: String,
    val status: StatusRelato,
    val criadoEm: String,
    val autor: String,
    val meu: Boolean,
    val local: Local,
)

data class TempoParado(val local: Local, val paradas: Int, val minutosParado: Int)

data class Disponibilidade(val linhaNumero: Int, val linhaNome: String, val equipamentos: Int, val percentual: Double)

data class Painel(
    val equipamentos: Int,
    val operando: Int,
    val parados: Int,
    val emManutencao: Int,
    val alertasAbertos: Int,
    val relatosAbertos: Int,
    val maisParados: List<TempoParado>,
    val disponibilidade: List<Disponibilidade>,
)

data class Credenciais(val email: String, val senha: String)

data class NovoUsuario(val nome: String, val email: String, val senha: String)

data class NovoRelato(val equipamentoId: Int, val descricao: String)

data class TextoDoRelato(val descricao: String)

data class SituacaoDoRelato(val status: StatusRelato)

data class SituacaoDoEquipamento(val localizacao: String, val status: StatusEquipamento)
