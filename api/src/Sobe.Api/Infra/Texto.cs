using System.Globalization;
using System.Text;

namespace Sobe.Api.Infra;

public static class Texto
{
    public static string SemAcento(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var letras = decomposto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(letras.ToArray()).Normalize(NormalizationForm.FormC);
    }
}
