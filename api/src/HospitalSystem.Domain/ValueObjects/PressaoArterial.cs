using System.Text.RegularExpressions;

namespace HospitalSystem.Domain.ValueObjects;

/// <summary>RN-TRI-03: pressão arterial "sistólica/diastólica" em mmHg (aceita "12/8" em cmHg).</summary>
public sealed partial record PressaoArterial(int Sistolica, int Diastolica)
{
    public static bool TentarCriar(string? valor, out PressaoArterial? pressao)
    {
        pressao = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var m = Formato().Match(valor.Trim());
        if (!m.Success)
        {
            return false;
        }

        var sis = int.Parse(m.Groups[1].Value);
        var dia = int.Parse(m.Groups[2].Value);

        // Formato antigo em cmHg (ex.: 12/8 → 120/80)
        if (sis < 30 && dia < 30)
        {
            sis *= 10;
            dia *= 10;
        }

        if (sis < 50 || sis > 300 || dia < 30 || dia > 200 || sis <= dia)
        {
            return false;
        }

        pressao = new PressaoArterial(sis, dia);
        return true;
    }

    public override string ToString() => $"{Sistolica}/{Diastolica}";

    [GeneratedRegex(@"^(\d{1,3})\s*[/xX]\s*(\d{1,3})$")]
    private static partial Regex Formato();
}
