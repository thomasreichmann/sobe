package br.com.fiap.sobe.ui

import androidx.lifecycle.ViewModelProvider.AndroidViewModelFactory.Companion.APPLICATION_KEY
import androidx.lifecycle.createSavedStateHandle
import androidx.lifecycle.viewmodel.CreationExtras
import androidx.lifecycle.viewmodel.initializer
import androidx.lifecycle.viewmodel.viewModelFactory
import br.com.fiap.sobe.SobeApplication
import br.com.fiap.sobe.ui.alertas.AlertasViewModel
import br.com.fiap.sobe.ui.conta.PainelViewModel
import br.com.fiap.sobe.ui.entrada.EntradaViewModel
import br.com.fiap.sobe.ui.estacoes.EstacaoViewModel
import br.com.fiap.sobe.ui.estacoes.EstacoesViewModel
import br.com.fiap.sobe.ui.relatos.NovoRelatoViewModel
import br.com.fiap.sobe.ui.relatos.RelatosViewModel

private fun CreationExtras.repositorio() = (this[APPLICATION_KEY] as SobeApplication).repositorio

val Fabrica = viewModelFactory {
    initializer { EntradaViewModel(repositorio()) }
    initializer { EstacoesViewModel(repositorio()) }
    initializer { EstacaoViewModel(createSavedStateHandle(), repositorio()) }
    initializer { AlertasViewModel(repositorio()) }
    initializer { RelatosViewModel(repositorio()) }
    initializer { NovoRelatoViewModel(createSavedStateHandle(), repositorio()) }
    initializer { PainelViewModel(repositorio()) }
}
