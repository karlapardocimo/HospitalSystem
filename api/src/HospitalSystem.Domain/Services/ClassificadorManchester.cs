using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Services;

/// <summary>Sinais vitais coletados na triagem.</summary>
public sealed record SinaisVitais(decimal Temperatura, int FrequenciaCardiaca, int SaturacaoOxigenio, int EscalaDor);

/// <summary>
/// RN-TRI-04 / RN-TRI-07: classificação de risco do Protocolo de Manchester.
/// Mesmos critérios da função classificarRiscoManchester() do sistema PHP.
/// </summary>
public static class ClassificadorManchester
{
    public static string Classificar(SinaisVitais s)
    {
        if (s.Temperatura > 40 || s.SaturacaoOxigenio < 90 || s.FrequenciaCardiaca > 130 || s.EscalaDor >= 8)
            return ClassificacaoRisco.Vermelho;
        if (s.Temperatura > 39 || s.SaturacaoOxigenio < 93 || s.FrequenciaCardiaca > 110 || s.EscalaDor >= 7)
            return ClassificacaoRisco.Laranja;
        if (s.Temperatura > 38 || s.SaturacaoOxigenio < 95 || s.FrequenciaCardiaca > 100 || s.EscalaDor >= 5)
            return ClassificacaoRisco.Amarelo;
        if (s.EscalaDor >= 2)
            return ClassificacaoRisco.Verde;
        return ClassificacaoRisco.Azul;
    }

    /// <summary>1 = mais grave (Vermelho) ... 5 = menos grave (Azul); 6 = desconhecida.</summary>
    public static int Prioridade(string? cor)
    {
        var i = ClassificacaoRisco.PorGravidade.ToList().IndexOf(cor ?? string.Empty);
        return i < 0 ? 6 : i + 1;
    }

    /// <summary>RN-TRI-07: tempo-alvo de atendimento por cor, em minutos.</summary>
    public static int TempoAlvoMinutos(string? cor) => cor switch
    {
        ClassificacaoRisco.Vermelho => 0,
        ClassificacaoRisco.Laranja => 10,
        ClassificacaoRisco.Amarelo => 60,
        ClassificacaoRisco.Verde => 120,
        _ => 240
    };

    /// <summary>RN-TRI-05: a classificação final nunca é menos grave que a calculada.</summary>
    public static bool EMenosGrave(string informada, string calculada) => Prioridade(informada) > Prioridade(calculada);
}
