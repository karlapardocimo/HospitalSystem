using HospitalSystem.Application.DTOs;

namespace HospitalSystem.Application.Abstractions;

// Consultas de leitura que juntam várias tabelas (listagens de tela).
// Ficam separadas dos repositórios, que trabalham só com entidades do domínio.

public interface IConsultaQueries
{
    Task<ConsultaDto?> ObterAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ConsultaDto>> ListarAsync(DateOnly? data, string? status, CancellationToken ct = default);
    Task<IReadOnlyList<FilaTriagemItemDto>> FilaTriagemAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FilaAtendimentoItemDto>> FilaAtendimentoAsync(CancellationToken ct = default);
    Task<TriagemDto?> ObterTriagemAsync(int idTriagem, CancellationToken ct = default);
}

public interface IProntuarioQueries
{
    Task<IReadOnlyList<TriagemDto>> TriagensDoPacienteAsync(int idPaciente, CancellationToken ct = default);
    Task<IReadOnlyList<RegistroProntuarioDto>> RegistrosDoPacienteAsync(int idPaciente, CancellationToken ct = default);
    Task<int?> IdPacienteDoProntuarioAsync(int idProntuario, CancellationToken ct = default);
    Task<PrescricaoDto?> ObterPrescricaoAsync(int idPrescricao, CancellationToken ct = default);
}

public interface ILeitoQueries
{
    Task<IReadOnlyList<LeitoDto>> MapaAsync(string? status, CancellationToken ct = default);
}

public interface IInternacaoQueries
{
    Task<InternacaoDto?> ObterAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<InternacaoDto>> ListarAsync(bool somenteAtivas, CancellationToken ct = default);
}

public interface IFaturamentoQueries
{
    Task<FaturamentoDto?> ObterAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<FaturamentoDto>> ListarAsync(string? status, CancellationToken ct = default);
    Task<ResumoFaturamentoDto> ResumoAsync(DateOnly inicioMes, DateOnly fimMes, CancellationToken ct = default);
    /// <summary>Dados brutos para RN-FAT-02 (o cálculo fica no domínio).</summary>
    Task<(decimal TotalInsumos, int Exames, int Prescricoes)> DadosConsolidacaoAsync(int idPaciente, DateTime agora, CancellationToken ct = default);
}

public interface IDashboardQueries
{
    Task<DashboardDto> ObterAsync(DateOnly hoje, CancellationToken ct = default);
}
