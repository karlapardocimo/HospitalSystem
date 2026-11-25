namespace HospitalSystem.Infrastructure.Security;

/// <summary>
/// Configuração do JWT (seção "Jwt"). A chave NÃO fica no appsettings.json:
/// vem de variável de ambiente (Jwt__Key) ou de "dotnet user-secrets".
/// </summary>
public sealed class JwtSettings
{
    public const string Secao = "Jwt";

    public string Issuer { get; set; } = "HospitalSystem.Api";
    public string Audience { get; set; } = "HospitalSystem.Clientes";
    public string Key { get; set; } = string.Empty;
    public int ExpiracaoMinutos { get; set; } = 60; // RN-USU-10

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Key) || System.Text.Encoding.UTF8.GetByteCount(Key) < 32)
        {
            throw new InvalidOperationException(
                "Configuração 'Jwt:Key' ausente ou com menos de 32 caracteres. " +
                "Defina com: dotnet user-secrets set \"Jwt:Key\" \"<chave-longa>\" ou a variável de ambiente Jwt__Key.");
        }
    }
}
