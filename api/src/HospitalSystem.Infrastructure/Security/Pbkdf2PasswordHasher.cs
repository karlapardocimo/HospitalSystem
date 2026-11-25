using System.Security.Cryptography;
using HospitalSystem.Application.Abstractions;

namespace HospitalSystem.Infrastructure.Security;

/// <summary>
/// RN-USU-04: hash de senha com PBKDF2 (HMAC-SHA256, 100.000 iterações, salt aleatório de 16 bytes).
/// Formato gravado no banco: PBKDF2$SHA256$iteracoes$saltBase64$hashBase64
/// - O salt diferente por usuário impede que duas senhas iguais tenham o mesmo hash
///   e inviabiliza tabelas pré-calculadas (rainbow tables).
/// - As muitas iterações deixam cada tentativa de força bruta lenta.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;
    private const int Iteracoes = 100_000;
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, Algoritmo, TamanhoHash);
        return $"PBKDF2$SHA256${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string senha, string hashArmazenado)
    {
        var partes = hashArmazenado.Split('$');
        if (partes.Length != 5 || partes[0] != "PBKDF2" || !int.TryParse(partes[2], out var iteracoes))
        {
            return false;
        }

        var salt = Convert.FromBase64String(partes[3]);
        var esperado = Convert.FromBase64String(partes[4]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, Algoritmo, esperado.Length);

        // Comparação em tempo constante: não "vaza" quantos bytes coincidiram.
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
