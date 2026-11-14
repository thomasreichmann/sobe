package br.com.fiap.sobe.ui.entrada

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Elevator
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.FocusDirection
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.semantics.LiveRegionMode
import androidx.compose.ui.semantics.liveRegion
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import br.com.fiap.sobe.ui.componentes.BotaoPrincipal
import br.com.fiap.sobe.ui.componentes.Espaco

@Composable
fun EntrarScreen(vm: EntradaViewModel, aoCriarConta: () -> Unit) {
    var ajustandoServidor by rememberSaveable { mutableStateOf(false) }
    val foco = LocalFocusManager.current

    Box(Modifier.fillMaxSize().background(MaterialTheme.colorScheme.surface).safeDrawingPadding().imePadding()) {
        IconButton(onClick = { ajustandoServidor = true }, modifier = Modifier.align(Alignment.TopEnd).padding(8.dp)) {
            Icon(Icons.Filled.Settings, contentDescription = "Endereço do servidor", tint = MaterialTheme.colorScheme.onSurfaceVariant)
        }
        Column(
            Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 28.dp, vertical = 24.dp),
            verticalArrangement = Arrangement.Center,
        ) {
            Marca()
            Espaco(12.dp)
            Text(
                "Saiba se o elevador da sua estação está funcionando antes de sair de casa.",
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Espaco(32.dp)
            CampoDeEmail(vm.email, { vm.email = it }, ImeAction.Next) { foco.moveFocus(FocusDirection.Down) }
            Espaco(14.dp)
            CampoDeSenha(vm.senha, { vm.senha = it }, "Senha", aoConcluir = vm::entrar)
            MensagemDeErro(vm.erro)
            Espaco(22.dp)
            BotaoPrincipal("Entrar", vm::entrar, carregando = vm.carregando)
            Espaco(6.dp)
            TextButton(
                onClick = {
                    vm.limparErro()
                    aoCriarConta()
                },
                modifier = Modifier.fillMaxWidth().heightIn(min = 48.dp),
            ) {
                Text("Criar conta")
            }
        }
    }

    if (ajustandoServidor) {
        DialogoDoServidor(vm.servidor, vm::trocarServidor) { ajustandoServidor = false }
    }
}

@Composable
fun CriarContaScreen(vm: EntradaViewModel, aoVoltar: () -> Unit) {
    val foco = LocalFocusManager.current

    Column(Modifier.fillMaxSize().background(MaterialTheme.colorScheme.surface).safeDrawingPadding().imePadding()) {
        IconButton(
            onClick = {
                vm.limparErro()
                aoVoltar()
            },
            modifier = Modifier.padding(8.dp),
        ) {
            Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Voltar")
        }
        Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = 28.dp)) {
            Text("Criar conta", style = MaterialTheme.typography.headlineMedium)
            Espaco(8.dp)
            Text(
                "Com a conta você acompanha suas estações e avisa quando encontra um equipamento parado.",
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Espaco(28.dp)
            OutlinedTextField(
                value = vm.nome,
                onValueChange = { vm.nome = it },
                label = { Text("Nome") },
                singleLine = true,
                keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words, imeAction = ImeAction.Next),
                keyboardActions = KeyboardActions(onNext = { foco.moveFocus(FocusDirection.Down) }),
                modifier = Modifier.fillMaxWidth(),
            )
            Espaco(14.dp)
            CampoDeEmail(vm.email, { vm.email = it }, ImeAction.Next) { foco.moveFocus(FocusDirection.Down) }
            Espaco(14.dp)
            CampoDeSenha(vm.senha, { vm.senha = it }, "Senha", "Pelo menos 8 caracteres, com letra e número.", vm::criarConta)
            MensagemDeErro(vm.erro)
            Espaco(22.dp)
            BotaoPrincipal("Criar conta", vm::criarConta, carregando = vm.carregando)
            Espaco(24.dp)
        }
    }
}

@Composable
private fun Marca() {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            Modifier.size(52.dp).clip(MaterialTheme.shapes.large).background(MaterialTheme.colorScheme.primary),
            contentAlignment = Alignment.Center,
        ) {
            Icon(Icons.Filled.Elevator, contentDescription = null, tint = MaterialTheme.colorScheme.onPrimary, modifier = Modifier.size(32.dp))
        }
        Espaco(largura = 14.dp)
        Text("Sobe", style = MaterialTheme.typography.displaySmall)
    }
}

@Composable
private fun CampoDeEmail(valor: String, aoMudar: (String) -> Unit, acao: ImeAction, aoConcluir: () -> Unit) {
    OutlinedTextField(
        value = valor,
        onValueChange = aoMudar,
        label = { Text("E-mail") },
        singleLine = true,
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email, imeAction = acao),
        keyboardActions = KeyboardActions(onNext = { aoConcluir() }, onDone = { aoConcluir() }),
        modifier = Modifier.fillMaxWidth(),
    )
}

@Composable
private fun CampoDeSenha(valor: String, aoMudar: (String) -> Unit, rotulo: String, ajuda: String? = null, aoConcluir: () -> Unit) {
    var visivel by rememberSaveable { mutableStateOf(false) }
    OutlinedTextField(
        value = valor,
        onValueChange = aoMudar,
        label = { Text(rotulo) },
        supportingText = ajuda?.let { { Text(it) } },
        singleLine = true,
        visualTransformation = if (visivel) VisualTransformation.None else PasswordVisualTransformation(),
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password, imeAction = ImeAction.Done),
        keyboardActions = KeyboardActions(onDone = { aoConcluir() }),
        trailingIcon = {
            IconButton(onClick = { visivel = !visivel }) {
                Icon(
                    if (visivel) Icons.Filled.VisibilityOff else Icons.Filled.Visibility,
                    contentDescription = if (visivel) "Esconder a senha" else "Mostrar a senha",
                )
            }
        },
        modifier = Modifier.fillMaxWidth(),
    )
}

@Composable
private fun MensagemDeErro(mensagem: String?) {
    if (mensagem == null) return
    Text(
        mensagem,
        color = MaterialTheme.colorScheme.error,
        style = MaterialTheme.typography.bodyMedium,
        modifier = Modifier.padding(top = 12.dp).semantics { liveRegion = LiveRegionMode.Polite },
    )
}

@Composable
private fun DialogoDoServidor(atual: String, aoSalvar: (String) -> Boolean, aoFechar: () -> Unit) {
    var endereco by remember { mutableStateOf(atual) }
    var invalido by remember { mutableStateOf(false) }
    AlertDialog(
        onDismissRequest = aoFechar,
        title = { Text("Endereço do servidor") },
        text = {
            OutlinedTextField(
                value = endereco,
                onValueChange = {
                    endereco = it
                    invalido = false
                },
                singleLine = true,
                isError = invalido,
                supportingText = { Text(if (invalido) "Comece com http:// ou https://" else "No emulador, a API local fica em http://10.0.2.2:5080/") },
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri),
            )
        },
        confirmButton = {
            TextButton(onClick = {
                if (aoSalvar(endereco)) aoFechar() else invalido = true
            }) { Text("Salvar") }
        },
        dismissButton = { TextButton(onClick = aoFechar) { Text("Cancelar") } },
    )
}
