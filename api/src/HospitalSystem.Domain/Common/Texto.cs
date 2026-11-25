using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HospitalSystem.Domain.Common;

/// <summary>Funções de texto usadas pelas regras (comparação sem acento, só dígitos etc.).</summary>
public static partial class Texto
{
    /// <summary>Remove acentos, converte para minúsculas e tira espaços das pontas.</summary>
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var decomposto = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Verifica se <paramref name="texto"/> menciona <paramref name="termo"/> como palavra inteira, ignorando acentos e maiúsculas.</summary>
    public static bool Menciona(string? texto, string? termo)
    {
        var t = Normalizar(texto);
        var p = Normalizar(termo);
        if (t.Length == 0 || p.Length == 0)
        {
            return false;
        }
        return Regex.IsMatch(t, $@"(^|[^\p{{L}}\p{{N}}]){Regex.Escape(p)}($|[^\p{{L}}\p{{N}}])");
    }

    public static string SomenteDigitos(string? valor) =>
        string.IsNullOrEmpty(valor) ? string.Empty : NaoDigitos().Replace(valor, string.Empty);

    /// <summary>Converte string vazia em null e remove espaços das pontas.</summary>
    public static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public static bool EmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email) && EmailRegex().IsMatch(email.Trim());

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigitos();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
