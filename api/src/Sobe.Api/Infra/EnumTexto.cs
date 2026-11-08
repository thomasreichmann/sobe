using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sobe.Api.Infra;

public static class EnumTexto
{
    public static string ParaTexto<T>(this T valor) where T : struct, Enum =>
        JsonNamingPolicy.SnakeCaseUpper.ConvertName(valor.ToString());

    public static T DeTexto<T>(string texto) where T : struct, Enum =>
        Enum.Parse<T>(texto.Replace("_", ""), ignoreCase: true);
}

public class EnumTextoConverter<T>() : ValueConverter<T, string>(v => v.ParaTexto(), v => EnumTexto.DeTexto<T>(v))
    where T : struct, Enum;

public class UtcConverter() : ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
