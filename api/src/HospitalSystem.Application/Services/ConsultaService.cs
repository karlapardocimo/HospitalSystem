using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;
using HospitalSystem.Domain.Services;

namespace HospitalSystem.Application.Services;

public interface IConsultaService
{
    Task<ResultWrapper<IReadOnlyList<ConsultaDto>>> ListarAsync(DateOnly? data, string? status, CancellationToken ct = default);
    Task<ResultWrapper<ConsultaDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ConsultaDto>> AgendarAsync(AgendarConsultaRequest r, CancellationToken ct = default);
    Task<ResultWrapper<ConsultaDto>> EnviarParaTriagemAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ConsultaDto>> CancelarAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ConsultaDto>> MarcarNaoCompareceuAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ConsultaDto>> LiberarAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<IReadOnlyList<FilaTriagemItemDto>>> FilaTriagemAsync(CancellationToken ct = default);
    Task<ResultWrapper<IReadOnlyList<FilaAtendimentoItemDto>>> FilaAtendimentoAsync(CancellationToken ct = default);
    Task<ResultWrapper<TriagemDto>> RegistrarTriagemAsync(int idConsulta, TriagemRequest r, CancellationToken ct = default);
    Task<ResultWrapper<RegistroProntuarioDto>> RegistrarEvolucaoAsync(int idConsulta, RegistroProntuarioRequest r, CancellationToken ct = default);
}

public class ConsultaService(
    IConsultaRepository consultas,
    IConsultaQueries queries,
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    ISalaRepository salas,
    IConvenioRepository convenios,
    IProntuarioRepository prontuarios,
    IUnitOfWork uow,
    ICacheService cache,
    IUsuarioAtual usuarioAtual,
    TimeProvider relogio) : IConsultaService
{
    public async Task<ResultWrapper<IReadOnlyList<ConsultaDto>>> ListarAsync(DateOnly? data, string? status, CancellationToken ct = default)
    {
        var lista = await queries.ListarAsync(data, status, ct);
        return ResultWrapper<IReadOnlyList<ConsultaDto>>.Ok(lista, $"{lista.Count} consulta(s) encontrada(s).");
    }

    public async Task<ResultWrapper<ConsultaDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var c = await queries.ObterAsync(id, ct);
        return c is null ? ResultWrapper<ConsultaDto>.NotFound("Consulta") : ResultWrapper<ConsultaDto>.Ok(c);
    }

    public async Task<ResultWrapper<ConsultaDto>> AgendarAsync(AgendarConsultaRequest r, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(r.IdPaciente, ct);
        if (paciente is null) return ResultWrapper<ConsultaDto>.RegraViolada("RN-CNS-01", "Paciente não encontrado.");

        Medico? medico = r.IdMedico is int idM ? await medicos.ObterPorIdAsync(idM, ct) : null;
        if (r.IdMedico is not null && medico is null) return ResultWrapper<ConsultaDto>.RegraViolada("RN-CNS-02", "Médico não encontrado.");

        Sala? sala = r.IdSala is int idS ? await salas.ObterPorIdAsync(idS, ct) : null;
        if (r.IdSala is not null && sala is null) return ResultWrapper<ConsultaDto>.NotFound("Sala");

        Convenio? convenio = r.IdConvenio is int idC ? await convenios.ObterPorIdAsync(idC, ct) : null;
        if (r.IdConvenio is not null && convenio is null) return ResultWrapper<ConsultaDto>.NotFound("Convênio");

        // Regras da própria consulta (data no passado, sala em manutenção, convênio vencido...)
        var consulta = Consulta.Agendar(paciente, medico, sala, convenio, r.Data, r.Horario, r.EnviarParaTriagem, relogio.Agora());

        // Regras que dependem de outros registros
        if (await consultas.ExisteConflitoMedicoAsync(medico!.Id, r.Data, r.Horario, null, ct))
            return ResultWrapper<ConsultaDto>.RegraViolada("RN-CNS-04", $"{medico.Nome} já possui consulta em {r.Data:dd/MM/yyyy} às {r.Horario:HH\\:mm}.");
        if (await consultas.ExisteConflitoPacienteAsync(paciente.Id, r.Data, r.Horario, null, ct))
            return ResultWrapper<ConsultaDto>.RegraViolada("RN-CNS-05", $"{paciente.Nome} já possui consulta neste dia e horário.");

        consulta.DefinirId(await consultas.AdicionarAsync(consulta, ct));
        cache.Remover(CacheKeys.Dashboard);
        return ResultWrapper<ConsultaDto>.Ok((await queries.ObterAsync(consulta.Id, ct))!,
            r.EnviarParaTriagem ? "Consulta agendada e paciente enviado para a fila de triagem." : "Consulta agendada com sucesso.");
    }

    public Task<ResultWrapper<ConsultaDto>> EnviarParaTriagemAsync(int id, CancellationToken ct = default) =>
        MudarStatusAsync(id, c => c.EnviarParaTriagem(), "Paciente enviado para a fila de triagem.", ct);

    public Task<ResultWrapper<ConsultaDto>> CancelarAsync(int id, CancellationToken ct = default) =>
        MudarStatusAsync(id, c => c.Cancelar(), "Consulta cancelada.", ct);

    public Task<ResultWrapper<ConsultaDto>> MarcarNaoCompareceuAsync(int id, CancellationToken ct = default) =>
        MudarStatusAsync(id, c => c.MarcarNaoCompareceu(), "Ausência registrada.", ct);

    public Task<ResultWrapper<ConsultaDto>> LiberarAsync(int id, CancellationToken ct = default) =>
        MudarStatusAsync(id, c => c.Liberar(), "Paciente liberado com sucesso.", ct);

    private async Task<ResultWrapper<ConsultaDto>> MudarStatusAsync(int id, Action<Consulta> acao, string mensagem, CancellationToken ct)
    {
        var consulta = await consultas.ObterPorIdAsync(id, ct);
        if (consulta is null) return ResultWrapper<ConsultaDto>.NotFound("Consulta");

        acao(consulta); // a entidade valida a transição (RN-CNS-07..10)
        await consultas.AtualizarStatusAsync(consulta, ct);
        cache.Remover(CacheKeys.Dashboard);
        return ResultWrapper<ConsultaDto>.Ok((await queries.ObterAsync(id, ct))!, mensagem);
    }

    public async Task<ResultWrapper<IReadOnlyList<FilaTriagemItemDto>>> FilaTriagemAsync(CancellationToken ct = default)
    {
        var fila = await queries.FilaTriagemAsync(ct);
        return ResultWrapper<IReadOnlyList<FilaTriagemItemDto>>.Ok(fila, $"{fila.Count} paciente(s) aguardando triagem.");
    }

    /// <summary>RN-TRI-06 e RN-TRI-07: ordena pela gravidade e depois pela chegada; marca atrasos.</summary>
    public async Task<ResultWrapper<IReadOnlyList<FilaAtendimentoItemDto>>> FilaAtendimentoAsync(CancellationToken ct = default)
    {
        var agora = relogio.Agora();
        var fila = (await queries.FilaAtendimentoAsync(ct))
            .Select(i =>
            {
                var alvo = ClassificadorManchester.TempoAlvoMinutos(i.ClassificacaoRisco);
                var espera = i.DataTriagem is DateTime d ? (int)Math.Max(0, (agora - d).TotalMinutes) : 0;
                return i with
                {
                    Prioridade = ClassificadorManchester.Prioridade(i.ClassificacaoRisco),
                    TempoAlvoMinutos = alvo,
                    MinutosAguardando = espera,
                    Atrasado = espera > alvo
                };
            })
            .OrderBy(i => i.Prioridade)
            .ThenBy(i => i.DataTriagem ?? DateTime.MaxValue)
            .ThenBy(i => i.IdTriagem)
            .ToList();

        return ResultWrapper<IReadOnlyList<FilaAtendimentoItemDto>>.Ok(fila, $"{fila.Count} paciente(s) em atendimento.");
    }

    public async Task<ResultWrapper<TriagemDto>> RegistrarTriagemAsync(int idConsulta, TriagemRequest r, CancellationToken ct = default)
    {
        var consulta = await consultas.ObterPorIdAsync(idConsulta, ct);
        if (consulta is null) return ResultWrapper<TriagemDto>.NotFound("Consulta");

        var triagem = consulta.RegistrarTriagem(r.Temperatura, r.PressaoArterial, r.FrequenciaCardiaca, r.SaturacaoOxigenio,
            r.EscalaDor, r.ClassificacaoRisco, r.QueixaPrincipal, relogio.Agora());

        // Triagem + mudança de status da consulta na mesma transação.
        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            triagem.DefinirId(await consultas.AdicionarTriagemAsync(triagem, ct));
            await consultas.AtualizarStatusAsync(consulta, ct);
        }, ct);

        cache.Remover(CacheKeys.Dashboard);
        var msg = triagem.ClassificacaoRisco == triagem.ClassificacaoCalculada
            ? $"Triagem registrada. Classificação: {triagem.ClassificacaoRisco}."
            : $"Triagem registrada. Classificação elevada de {triagem.ClassificacaoCalculada} para {triagem.ClassificacaoRisco}.";
        return ResultWrapper<TriagemDto>.Ok(triagem.ToDto(), msg);
    }

    public async Task<ResultWrapper<RegistroProntuarioDto>> RegistrarEvolucaoAsync(int idConsulta, RegistroProntuarioRequest r, CancellationToken ct = default)
    {
        var consulta = await consultas.ObterPorIdAsync(idConsulta, ct);
        if (consulta is null) return ResultWrapper<RegistroProntuarioDto>.NotFound("Consulta");

        var registro = RegistroProntuario.Criar(consulta, r.EvolucaoClinica, r.Observacoes,
            $"{usuarioAtual.Nome} ({usuarioAtual.Perfil})", relogio.Agora()); // RN-PRT-03
        registro.DefinirId(await prontuarios.AdicionarRegistroAsync(registro, ct));

        return ResultWrapper<RegistroProntuarioDto>.Ok(new RegistroProntuarioDto
        {
            Id = registro.Id,
            IdConsulta = registro.IdConsulta,
            EvolucaoClinica = registro.EvolucaoClinica,
            Observacoes = registro.Observacoes,
            DataRegistro = registro.DataRegistro,
            Profissional = registro.Profissional
        }, "Evolução registrada no prontuário.");
    }
}
