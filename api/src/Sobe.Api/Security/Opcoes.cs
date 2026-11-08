namespace Sobe.Api.Security;

public class JwtOptions
{
    public string Chave { get; set; } = "";
    public string Emissor { get; set; } = "sobe-api";
    public int ValidadeMinutos { get; set; } = 120;
}

public class DispositivoOptions
{
    public string Chave { get; set; } = "";
}

public static class Perfis
{
    public const string Admin = "ADMIN";
    public const string Cidadao = "CIDADAO";
    public const string Dispositivo = "DISPOSITIVO";
}
