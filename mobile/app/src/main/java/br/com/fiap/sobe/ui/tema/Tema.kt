package br.com.fiap.sobe.ui.tema

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import br.com.fiap.sobe.R

private val Tinta = Color(0xFF0E1B2C)
private val Neblina = Color(0xFFE8EEF5)

private val Claro = lightColorScheme(
    primary = Tinta,
    onPrimary = Color.White,
    primaryContainer = Color(0xFFE3E9F1),
    onPrimaryContainer = Tinta,
    secondary = Tinta,
    onSecondary = Color.White,
    secondaryContainer = Color(0xFFE3E9F1),
    onSecondaryContainer = Tinta,
    background = Color(0xFFF1F4F8),
    onBackground = Tinta,
    surface = Color.White,
    onSurface = Tinta,
    surfaceVariant = Color(0xFFE9EDF2),
    onSurfaceVariant = Color(0xFF445263),
    surfaceContainerLowest = Color.White,
    surfaceContainerLow = Color.White,
    surfaceContainer = Color.White,
    surfaceContainerHigh = Color(0xFFF1F4F8),
    surfaceContainerHighest = Color(0xFFE9EDF2),
    outline = Color(0xFF6E7C8D),
    outlineVariant = Color(0xFFD5DBE3),
    error = Color(0xFFB3261E),
    onError = Color.White,
    errorContainer = Color(0xFFFBE9E7),
    onErrorContainer = Color(0xFF7A1410),
)

private val Escuro = darkColorScheme(
    primary = Neblina,
    onPrimary = Tinta,
    primaryContainer = Color(0xFF24364D),
    onPrimaryContainer = Neblina,
    secondary = Neblina,
    onSecondary = Tinta,
    secondaryContainer = Color(0xFF24364D),
    onSecondaryContainer = Neblina,
    background = Color(0xFF0A1420),
    onBackground = Color(0xFFEFF3F7),
    surface = Color(0xFF111E2E),
    onSurface = Color(0xFFEFF3F7),
    surfaceVariant = Color(0xFF1B2B3F),
    onSurfaceVariant = Color(0xFFB1BCC9),
    surfaceContainerLowest = Color(0xFF0A1420),
    surfaceContainerLow = Color(0xFF111E2E),
    surfaceContainer = Color(0xFF111E2E),
    surfaceContainerHigh = Color(0xFF1B2B3F),
    surfaceContainerHighest = Color(0xFF24364D),
    outline = Color(0xFF8593A4),
    outlineVariant = Color(0xFF2A3B50),
    error = Color(0xFFFF8A80),
    onError = Color(0xFF4A0A06),
    errorContainer = Color(0xFF5C1A15),
    onErrorContainer = Color(0xFFFFDAD6),
)

data class CoresDeSituacao(
    val operando: Color,
    val fundoOperando: Color,
    val parado: Color,
    val fundoParado: Color,
    val manutencao: Color,
    val fundoManutencao: Color,
    val favorita: Color,
)

private val SituacaoClara = CoresDeSituacao(
    operando = Color(0xFF0B6B3A),
    fundoOperando = Color(0xFFE2F2E9),
    parado = Color(0xFFB3261E),
    fundoParado = Color(0xFFFBE9E7),
    manutencao = Color(0xFF855000),
    fundoManutencao = Color(0xFFFAEEDB),
    favorita = Color(0xFF855000),
)

private val SituacaoEscura = CoresDeSituacao(
    operando = Color(0xFF7BD8A4),
    fundoOperando = Color(0xFF123524),
    parado = Color(0xFFFF8A80),
    fundoParado = Color(0xFF4A1512),
    manutencao = Color(0xFFFFC56E),
    fundoManutencao = Color(0xFF43300E),
    favorita = Color(0xFFFFC56E),
)

val LocalSituacao = staticCompositionLocalOf { SituacaoClara }

private val Atkinson = FontFamily(
    Font(R.font.atkinson_hyperlegible_regular, FontWeight.Normal),
    Font(R.font.atkinson_hyperlegible_bold, FontWeight.Bold),
)

private fun estilo(tamanho: Int, entrelinha: Int, peso: FontWeight = FontWeight.Normal) =
    TextStyle(fontFamily = Atkinson, fontWeight = peso, fontSize = tamanho.sp, lineHeight = entrelinha.sp)

private val Tipografia = Typography(
    displaySmall = estilo(34, 40, FontWeight.Bold),
    headlineMedium = estilo(28, 34, FontWeight.Bold),
    headlineSmall = estilo(24, 30, FontWeight.Bold),
    titleLarge = estilo(21, 28, FontWeight.Bold),
    titleMedium = estilo(18, 24, FontWeight.Bold),
    titleSmall = estilo(16, 22, FontWeight.Bold),
    bodyLarge = estilo(17, 26),
    bodyMedium = estilo(16, 23),
    bodySmall = estilo(14, 20),
    labelLarge = estilo(16, 22, FontWeight.Bold),
    labelMedium = estilo(14, 20, FontWeight.Bold),
    labelSmall = estilo(13, 18, FontWeight.Bold),
)

private val Formas = Shapes(
    extraSmall = RoundedCornerShape(4.dp),
    small = RoundedCornerShape(8.dp),
    medium = RoundedCornerShape(10.dp),
    large = RoundedCornerShape(14.dp),
    extraLarge = RoundedCornerShape(18.dp),
)

@Composable
fun TemaSobe(conteudo: @Composable () -> Unit) {
    val escuro = isSystemInDarkTheme()
    CompositionLocalProvider(LocalSituacao provides if (escuro) SituacaoEscura else SituacaoClara) {
        MaterialTheme(
            colorScheme = if (escuro) Escuro else Claro,
            typography = Tipografia,
            shapes = Formas,
            content = conteudo,
        )
    }
}
