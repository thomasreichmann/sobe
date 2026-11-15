package br.com.fiap.sobe

import android.Manifest
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import br.com.fiap.sobe.data.Alerta

private const val CANAL = "paradas"

fun podeNotificar(contexto: Context) =
    Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU ||
        ContextCompat.checkSelfPermission(contexto, Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED

fun notificarParada(contexto: Context, alerta: Alerta) {
    if (!podeNotificar(contexto)) return

    val gerenciador = contexto.getSystemService(NotificationManager::class.java)
    gerenciador.createNotificationChannel(
        NotificationChannel(CANAL, contexto.getString(R.string.canal_de_paradas), NotificationManager.IMPORTANCE_HIGH),
    )
    val abrirApp = PendingIntent.getActivity(
        contexto,
        0,
        Intent(contexto, MainActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP),
        PendingIntent.FLAG_IMMUTABLE,
    )
    val notificacao = NotificationCompat.Builder(contexto, CANAL)
        .setSmallIcon(R.drawable.ic_parada)
        .setContentTitle("Parada em ${alerta.local.estacao} (Linha ${alerta.local.linhaNumero})")
        .setContentText("${alerta.local.localizacao}. ${alerta.motivo}")
        .setStyle(NotificationCompat.BigTextStyle().bigText("${alerta.local.localizacao}. ${alerta.motivo}"))
        .setPriority(NotificationCompat.PRIORITY_HIGH)
        .setContentIntent(abrirApp)
        .setAutoCancel(true)
        .build()
    NotificationManagerCompat.from(contexto).notify(alerta.id, notificacao)
}
