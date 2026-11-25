using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Entities;

namespace HospitalSystem.Domain.Services;

public sealed record AnalisePrescricao(IReadOnlyList<string> Interacoes, string? Contraindicacoes)
{
    public bool PossuiInteracoes => Interacoes.Count > 0;
}

/// <summary>Regras de segurança da prescrição (RN-PRE-03, RN-PRE-04, RN-PRE-05).</summary>
public static class AnalisadorPrescricao
{
    /// <summary>
    /// Lança exceção se o paciente for alérgico; caso contrário devolve os alertas
    /// de interação (com os medicamentos recentes do paciente) e de contraindicação.
    /// </summary>
    public static AnalisePrescricao Analisar(Paciente paciente, Medicamento novo, IEnumerable<Medicamento> medicamentosRecentes)
    {
        Validacao.Garantir(!paciente.EAlergicoA(novo.Nome), "RN-PRE-03",
            $"Prescrição bloqueada: o paciente possui alergia registrada a {novo.Nome} (alergias: {paciente.Alergias}).");

        var interacoes = medicamentosRecentes
            .Where(novo.InterageCom)
            .Select(m => $"Interação entre {novo.Nome} e {m.Nome}, prescrito ao paciente nos últimos 30 dias.")
            .Distinct()
            .ToList();

        return new AnalisePrescricao(interacoes, novo.Contraindicacoes);
    }
}
