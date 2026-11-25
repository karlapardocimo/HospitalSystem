using HospitalSystem.Domain.Common;

namespace HospitalSystem.Domain.ValueObjects;

/// <summary>CPF validado pelos dígitos verificadores (mesmo algoritmo do validarCPF() do PHP).</summary>
public static class Cpf
{
    public static bool Valido(string? cpf)
    {
        var d = Texto.SomenteDigitos(cpf);
        if (d.Length != 11 || d.Distinct().Count() == 1)
        {
            return false;
        }

        return DigitoVerificador(d, 9) == d[9] - '0' && DigitoVerificador(d, 10) == d[10] - '0';
    }

    private static int DigitoVerificador(string d, int tamanho)
    {
        var soma = 0;
        for (var i = 0; i < tamanho; i++)
        {
            soma += (d[i] - '0') * (tamanho + 1 - i);
        }
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public static string Formatar(string? cpf)
    {
        var d = Texto.SomenteDigitos(cpf);
        return d.Length == 11 ? $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}" : cpf ?? string.Empty;
    }
}
