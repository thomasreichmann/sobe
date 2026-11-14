package br.com.fiap.sobe.data

import androidx.room.Dao
import androidx.room.Database
import androidx.room.Entity
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.PrimaryKey
import androidx.room.Query
import androidx.room.RoomDatabase
import androidx.room.Transaction

@Entity(tableName = "estacao")
data class EstacaoGuardada(
    @PrimaryKey val id: Int,
    val nome: String,
    val ordem: Int,
    val linhaId: Int,
    val linhaNumero: Int,
    val linhaNome: String,
    val linhaCor: String,
    val linhaOperadora: String,
    val equipamentos: Int,
    val foraDeServico: Int,
    val acessivel: Boolean,
    val favorita: Boolean,
    val guardadaEm: Long,
)

@Entity(tableName = "equipamento")
data class EquipamentoGuardado(
    @PrimaryKey val id: Int,
    val estacaoId: Int,
    val tipo: String,
    val localizacao: String,
    val status: String,
    val statusDesde: String,
    val alertaId: Int?,
    val alertaMotivo: String?,
    val alertaAbertoEm: String?,
    val guardadoEm: Long,
)

@Dao
interface CacheDao {
    @Query("SELECT * FROM estacao ORDER BY linhaNumero, ordem")
    suspend fun estacoes(): List<EstacaoGuardada>

    @Query("SELECT * FROM estacao WHERE id = :id")
    suspend fun estacao(id: Int): EstacaoGuardada?

    @Query("SELECT * FROM equipamento WHERE estacaoId = :estacaoId ORDER BY id")
    suspend fun equipamentos(estacaoId: Int): List<EquipamentoGuardado>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun inserirEstacoes(estacoes: List<EstacaoGuardada>)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun inserirEquipamentos(equipamentos: List<EquipamentoGuardado>)

    @Query("DELETE FROM estacao")
    suspend fun apagarEstacoes()

    @Query("DELETE FROM equipamento WHERE estacaoId = :estacaoId")
    suspend fun apagarEquipamentosDa(estacaoId: Int)

    @Query("DELETE FROM equipamento")
    suspend fun apagarEquipamentos()

    @Transaction
    suspend fun guardarEstacoes(estacoes: List<EstacaoGuardada>) {
        apagarEstacoes()
        inserirEstacoes(estacoes)
    }

    @Transaction
    suspend fun guardarEquipamentos(estacaoId: Int, equipamentos: List<EquipamentoGuardado>) {
        apagarEquipamentosDa(estacaoId)
        inserirEquipamentos(equipamentos)
    }

    @Transaction
    suspend fun esvaziar() {
        apagarEstacoes()
        apagarEquipamentos()
    }
}

@Database(entities = [EstacaoGuardada::class, EquipamentoGuardado::class], version = 1, exportSchema = false)
abstract class CacheLocal : RoomDatabase() {
    abstract fun dao(): CacheDao
}

fun EstacaoResumo.paraGuardar(agora: Long) = EstacaoGuardada(
    id, nome, ordem, linha.id, linha.numero, linha.nome, linha.cor, linha.operadora, equipamentos, foraDeServico,
    acessivel, favorita, agora,
)

fun EstacaoGuardada.linha() = Linha(linhaId, linhaNumero, linhaNome, linhaCor, linhaOperadora)

fun EstacaoGuardada.paraResumo() = EstacaoResumo(id, nome, ordem, linha(), equipamentos, foraDeServico, acessivel, favorita)

fun Equipamento.paraGuardar(agora: Long) = EquipamentoGuardado(
    id, estacaoId, tipo.name, localizacao, status.name, statusDesde, alerta?.id, alerta?.motivo, alerta?.abertoEm, agora,
)

fun EquipamentoGuardado.paraEquipamento(): Equipamento {
    val alerta = if (alertaId != null && alertaMotivo != null && alertaAbertoEm != null) {
        AlertaDoEquipamento(alertaId, alertaMotivo, alertaAbertoEm)
    } else {
        null
    }
    return Equipamento(id, estacaoId, TipoEquipamento.valueOf(tipo), localizacao, StatusEquipamento.valueOf(status), statusDesde, alerta)
}
