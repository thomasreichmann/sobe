package br.com.fiap.sobe

import android.app.Application
import androidx.room.Room
import br.com.fiap.sobe.data.CacheLocal
import br.com.fiap.sobe.data.Repositorio
import br.com.fiap.sobe.data.Sessao
import com.google.gson.Gson

class SobeApplication : Application() {
    lateinit var sessao: Sessao
        private set
    lateinit var repositorio: Repositorio
        private set

    override fun onCreate() {
        super.onCreate()
        val gson = Gson()
        val cache = Room.databaseBuilder(this, CacheLocal::class.java, "cache").build()
        sessao = Sessao(this, gson)
        repositorio = Repositorio(sessao, gson, cache.dao())
    }
}
