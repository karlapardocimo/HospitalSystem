using HospitalSystem.Domain.Entities;

namespace HospitalSystem.Domain.Interfaces;

// Abstrações de persistência. A implementação (MySQL) fica na camada Infrastructure,
// então o domínio não conhece nenhum detalhe de banco de dados.

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default);
    Task<bool> EmailExisteAsync(string email, int? ignorarId = null, CancellationToken ct = default);
    Task<int> ContarAsync(CancellationToken ct = default);
    Task<int> AdicionarAsync(Usuario usuario, CancellationToken ct = default);
    Task AtualizarAsync(Usuario usuario, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IPacienteRepository
{
    Task<Paciente?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Paciente>> ListarAsync(string? busca, CancellationToken ct = default);
    Task<bool> CpfExisteAsync(string cpf, int? ignorarId = null, CancellationToken ct = default);
    Task<bool> PossuiHistoricoAsync(int id, CancellationToken ct = default);
    Task<int> AdicionarAsync(Paciente paciente, CancellationToken ct = default);
    Task AtualizarAsync(Paciente paciente, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IMedicoRepository
{
    Task<Medico?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Medico>> ListarAsync(CancellationToken ct = default);
    Task<bool> CrmExisteAsync(string crm, int? ignorarId = null, CancellationToken ct = default);
    Task<bool> PossuiConsultasAsync(int id, CancellationToken ct = default);
    Task<int> AdicionarAsync(Medico medico, CancellationToken ct = default);
    Task AtualizarAsync(Medico medico, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IConvenioRepository
{
    Task<Convenio?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Convenio>> ListarAsync(CancellationToken ct = default);
    Task<bool> NomeExisteAsync(string nome, int? ignorarId = null, CancellationToken ct = default);
    Task<bool> PossuiConsultasAsync(int id, CancellationToken ct = default);
    Task<int> AdicionarAsync(Convenio convenio, CancellationToken ct = default);
    Task AtualizarAsync(Convenio convenio, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface ISalaRepository
{
    Task<Sala?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Sala>> ListarAsync(CancellationToken ct = default);
    Task<bool> NumeroExisteAsync(string numero, int? ignorarId = null, CancellationToken ct = default);
    Task<bool> PossuiConsultasAsync(int id, CancellationToken ct = default);
    Task<int> AdicionarAsync(Sala sala, CancellationToken ct = default);
    Task AtualizarAsync(Sala sala, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IConsultaRepository
{
    Task<Consulta?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteConflitoMedicoAsync(int idMedico, DateOnly data, TimeOnly horario, int? ignorarId = null, CancellationToken ct = default);
    Task<bool> ExisteConflitoPacienteAsync(int idPaciente, DateOnly data, TimeOnly horario, int? ignorarId = null, CancellationToken ct = default);
    Task<int> AdicionarAsync(Consulta consulta, CancellationToken ct = default);
    Task AtualizarStatusAsync(Consulta consulta, CancellationToken ct = default);
    Task<int> AdicionarTriagemAsync(Triagem triagem, CancellationToken ct = default);
}

public interface IProntuarioRepository
{
    Task<RegistroProntuario?> ObterRegistroAsync(int idProntuario, CancellationToken ct = default);
    Task<int> AdicionarRegistroAsync(RegistroProntuario registro, CancellationToken ct = default);
    Task<Exame?> ObterExameAsync(int idExame, CancellationToken ct = default);
    Task<int> AdicionarExameAsync(Exame exame, CancellationToken ct = default);
    Task AtualizarExameAsync(Exame exame, CancellationToken ct = default);
    Task<int> AdicionarPrescricaoAsync(Prescricao prescricao, CancellationToken ct = default);
    /// <summary>Medicamentos prescritos ao paciente a partir de <paramref name="desde"/> (RN-PRE-04).</summary>
    Task<IReadOnlyList<Medicamento>> MedicamentosPrescritosDesdeAsync(int idPaciente, DateTime desde, CancellationToken ct = default);
}

public interface IMedicamentoRepository
{
    Task<Medicamento?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Medicamento>> ListarAsync(CancellationToken ct = default);
    Task<bool> NomeExisteAsync(string nome, int? ignorarId = null, CancellationToken ct = default);
    Task<bool> PossuiPrescricoesAsync(int id, CancellationToken ct = default);
    Task<int> AdicionarAsync(Medicamento medicamento, CancellationToken ct = default);
    Task AtualizarAsync(Medicamento medicamento, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface ILeitoRepository
{
    Task<Leito?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Leito>> ListarAsync(string? status, CancellationToken ct = default);
    Task<bool> NumeroExisteNaAlaAsync(string numero, string ala, int? ignorarId = null, CancellationToken ct = default);
    Task<bool> PossuiInternacoesAsync(int id, CancellationToken ct = default);
    Task<int> AdicionarAsync(Leito leito, CancellationToken ct = default);
    Task AtualizarAsync(Leito leito, CancellationToken ct = default);
    Task RemoverAsync(int id, CancellationToken ct = default);
}

public interface IInternacaoRepository
{
    Task<Internacao?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Internacao?> ObterAtivaPorPacienteAsync(int idPaciente, CancellationToken ct = default);
    Task<Internacao?> ObterAtivaPorLeitoAsync(int idLeito, CancellationToken ct = default);
    Task<int> AdicionarAsync(Internacao internacao, CancellationToken ct = default);
    Task AtualizarAsync(Internacao internacao, CancellationToken ct = default);
}

public interface IInsumoRepository
{
    Task<Insumo?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Insumo>> ListarAsync(CancellationToken ct = default);
    Task<bool> NomeExisteAsync(string nome, int? ignorarId = null, CancellationToken ct = default);
    Task<int> AdicionarAsync(Insumo insumo, CancellationToken ct = default);
    Task AtualizarAsync(Insumo insumo, CancellationToken ct = default);
    Task<int> AdicionarUsoAsync(UsoInsumo uso, CancellationToken ct = default);
}

public interface IFaturamentoRepository
{
    Task<Faturamento?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<int> AdicionarAsync(Faturamento faturamento, CancellationToken ct = default);
    Task AtualizarAsync(Faturamento faturamento, CancellationToken ct = default);
    Task AdicionarAuditoriaAsync(Auditoria auditoria, CancellationToken ct = default);
}
