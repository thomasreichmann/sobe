using System.Security.Cryptography;

namespace Sobe.Api.Security;

public static class SenhaHasher
{
    private const string Algoritmo = "PBKDF2-SHA256";
    private const int Iteracoes = 600_000;
    private const int BytesDoSalt = 16;
    private const int BytesDoHash = 32;

    // Conferido quando o e-mail não existe, para o login levar o mesmo tempo nos dois casos.
    public static readonly string HashDeDescarte = Gerar(Guid.NewGuid().ToString());

    public static string Gerar(string senha, int iteracoes = Iteracoes)
    {
        var salt = RandomNumberGenerator.GetBytes(BytesDoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, BytesDoHash);
        return $"{Algoritmo}${iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Conferir(string senha, string armazenado)
    {
        var partes = armazenado.Split('$');
        if (partes.Length != 4 || partes[0] != Algoritmo || !int.TryParse(partes[1], out var iteracoes)) return false;

        var salt = Convert.FromBase64String(partes[2]);
        var esperado = Convert.FromBase64String(partes[3]);
        var obtido = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(obtido, esperado);
    }
}
