using System.Globalization;

namespace HospitalSystem.Domain.Common;

/// <summary>Formata valores em reais (R$ 1.234,56) sem depender da cultura instalada no servidor.</summary>
public static class Moeda
{
    private static readonly NumberFormatInfo Formato = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberDecimalDigits = 2
    };

    public static string Formatar(decimal valor) => "R$ " + valor.ToString("N2", Formato);
}
