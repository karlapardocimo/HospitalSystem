using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;
using HospitalSystem.Domain.Services;
using HospitalSystem.Domain.ValueObjects;
using HospitalSystem.Domain.Common;

namespace HospitalSystem.Application.Services;

public interface IPacienteService
{
    Task<ResultWrapper<IReadOnlyList<PacienteDto>>> ListarAsync(string? busca, CancellationToken ct = default);
    Task<ResultWrapper<PacienteDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<PacienteDto>> CriarAsync(PacienteRequest request, CancellationToken ct = default);
    Task<ResultWrapper<PacienteDto>> AtualizarAsync(int id, PacienteRequest request, CancellationToken ct = default);
    Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ProntuarioPacienteDto>> ProntuarioAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ConsolidacaoGastosDto>> GastosAsync(int id, CancellationToken ct = default);
}

public class PacienteService(
    IPacienteRepository pacientes,
    IConsultaRepository consultas,
    IConsultaQueries consultaQueries,
    IProntuarioQueries prontuarioQueries,
    IFaturamentoQueries faturamentoQueries,
    IUnitOfWork uow,
    ICacheService cache,
    TimeProvider relogio) : IPacienteService
{
    public async Task<ResultWrapper<IReadOnlyList<PacienteDto>>> ListarAsync(string? busca, CancellationToken ct = default)
    {
        var hoje = relogio.Hoje();
        var lista = (await pacientes.ListarAsync(busca, ct)).Select(p => p.ToDto(hoje)).ToList();
        return ResultWrapper<IReadOnlyList<PacienteDto>>.Ok(lista, $"{lista.Count} paciente(s) encontrado(s).");
    }

    public async Task<ResultWrapper<PacienteDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var p = await pacientes.ObterPorIdAsync(id, ct);
        return p is null ? ResultWrapper<PacienteDto>.NotFound("Paciente") : ResultWrapper<PacienteDto>.Ok(p.ToDto(relogio.Hoje()));
    }

    public async Task<ResultWrapper<PacienteDto>> CriarAsync(PacienteRequest r, CancellationToken ct = default)
    {
        var agora = relogio.Agora();
        var paciente = Paciente.Criar(r.Nome, r.Cpf, r.DataNascimento, r.Telefone, r.Endereco, r.Alergias,
            r.HistoricoClinico, r.TipoSanguineo, DateOnly.FromDateTime(agora));

        if (await pacientes.CpfExisteAsync(paciente.Cpf, null, ct))
        {
            return ResultWrapper<PacienteDto>.Conflict("RN-PAC-03", "Já existe um paciente cadastrado com este CPF.");
        }

        // Paciente + entrada na fila de triagem são gravados juntos (RN-PAC-07).
        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            paciente.DefinirId(await pacientes.AdicionarAsync(paciente, ct));
            if (r.EnviarParaTriagem)
            {
                await consultas.AdicionarAsync(Consulta.EntradaNaFilaDeTriagem(paciente.Id, agora), ct);
            }
        }, ct);

        cache.Remover(CacheKeys.Dashboard);
        var msg = r.EnviarParaTriagem ? "Paciente cadastrado e enviado para a fila de triagem." : "Paciente cadastrado com sucesso.";
        return ResultWrapper<PacienteDto>.Ok(paciente.ToDto(DateOnly.FromDateTime(agora)), msg);
    }

    public async Task<ResultWrapper<PacienteDto>> AtualizarAsync(int id, PacienteRequest r, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(id, ct);
        if (paciente is null) return ResultWrapper<PacienteDto>.NotFound("Paciente");

        var hoje = relogio.Hoje();
        paciente.Atualizar(r.Nome, r.Cpf, r.DataNascimento, r.Telefone, r.Endereco, r.Alergias, r.HistoricoClinico, r.TipoSanguineo, hoje);

        if (await pacientes.CpfExisteAsync(paciente.Cpf, id, ct))
        {
            return ResultWrapper<PacienteDto>.Conflict("RN-PAC-03", "Já existe outro paciente cadastrado com este CPF.");
        }

        await pacientes.AtualizarAsync(paciente, ct);
        cache.Remover(CacheKeys.Dashboard);
        return ResultWrapper<PacienteDto>.Ok(paciente.ToDto(hoje), "Dados do paciente atualizados com sucesso.");
    }

    public async Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default)
    {
        if (await pacientes.ObterPorIdAsync(id, ct) is null) return ResultWrapper<object?>.NotFound("Paciente");

        if (await pacientes.PossuiHistoricoAsync(id, ct))
        {
            return ResultWrapper<object?>.RegraViolada("RN-PAC-08",
                "O paciente possui consultas, internações ou faturamentos e não pode ser excluído.");
        }

        await pacientes.RemoverAsync(id, ct);
        return ResultWrapper.Ok("Paciente removido do sistema.");
    }

    public async Task<ResultWrapper<ProntuarioPacienteDto>> ProntuarioAsync(int id, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(id, ct);
        if (paciente is null) return ResultWrapper<ProntuarioPacienteDto>.NotFound("Paciente");

        var emAtendimento = (await consultaQueries.ListarAsync(null, Domain.Constants.StatusConsulta.EmAtendimento, ct))
            .Where(c => c.IdPaciente == id).ToList();
        var triagens = await prontuarioQueries.TriagensDoPacienteAsync(id, ct);
        var registros = await prontuarioQueries.RegistrosDoPacienteAsync(id, ct);

        return ResultWrapper<ProntuarioPacienteDto>.Ok(
            new ProntuarioPacienteDto(paciente.ToDto(relogio.Hoje()), emAtendimento, triagens, registros));
    }

    public async Task<ResultWrapper<ConsolidacaoGastosDto>> GastosAsync(int id, CancellationToken ct = default)
    {
        if (await pacientes.ObterPorIdAsync(id, ct) is null) return ResultWrapper<ConsolidacaoGastosDto>.NotFound("Paciente");

        var (insumos, exames, prescricoes) = await faturamentoQueries.DadosConsolidacaoAsync(id, relogio.Agora(), ct);
        var c = new ConsolidacaoGastos(insumos, exames, prescricoes); // RN-FAT-02 (cálculo no domínio)
        return ResultWrapper<ConsolidacaoGastosDto>.Ok(new ConsolidacaoGastosDto(id, c.TotalInsumos, c.QuantidadeExames,
            c.TotalExames, c.QuantidadePrescricoes, c.TotalPrescricoes, c.ValorTotal), "Consolidação de gastos calculada.");
    }
}
