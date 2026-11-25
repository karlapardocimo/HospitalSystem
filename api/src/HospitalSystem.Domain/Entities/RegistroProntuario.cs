using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

/// <summary>
/// Registro de evolução clínica (tabela "prontuario"). Imutável depois de criado (RN-PRT-04):
/// não existe método de alteração.
/// </summary>
public class RegistroProntuario
{
    public int Id { get; private set; }
    public int IdConsulta { get; private set; }
    public string EvolucaoClinica { get; private set; }
    public string? Observacoes { get; private set; }
    public DateTime DataRegistro { get; private set; }
    public string? Profissional { get; private set; }

    public RegistroProntuario(int id, int idConsulta, string evolucaoClinica, string? observacoes, DateTime dataRegistro, string? profissional)
    {
        Id = id; IdConsulta = idConsulta; EvolucaoClinica = evolucaoClinica; Observacoes = observacoes;
        DataRegistro = dataRegistro; Profissional = profissional;
    }

    public static RegistroProntuario Criar(Consulta consulta, string evolucao, string? observacoes, string profissional, DateTime agora)
    {
        new Validacao()
            .Se(consulta.Status != StatusConsulta.EmAtendimento, "RN-PRT-01",
                $"Só é possível registrar evolução em consulta 'Em Atendimento' (status atual: {consulta.Status}).")
            .Se(string.IsNullOrWhiteSpace(evolucao) || evolucao.Trim().Length < 10, "RN-PRT-02",
                "A evolução clínica é obrigatória e deve ter no mínimo 10 caracteres.")
            .LancarSeInvalido();

        return new RegistroProntuario(0, consulta.Id, evolucao.Trim(), Texto.Limpar(observacoes), agora, profissional);
    }

    public void DefinirId(int id) => Id = id;
}
