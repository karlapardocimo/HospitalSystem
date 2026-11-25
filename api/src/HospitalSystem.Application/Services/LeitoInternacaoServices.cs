using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;

namespace HospitalSystem.Application.Services;

public interface ILeitoService
{
    Task<ResultWrapper<IReadOnlyList<LeitoDto>>> MapaAsync(string? status, CancellationToken ct = default);
    Task<ResultWrapper<LeitoDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<LeitoDto>> CriarAsync(LeitoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<LeitoDto>> AtualizarAsync(int id, LeitoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<LeitoDto>> AlterarStatusAsync(int id, AlterarStatusLeitoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default);
}

public class LeitoService(ILeitoRepository repo, ILeitoQueries queries, ICacheService cache) : ILeitoService
{
    /// <summary>Mapa de leitos com cache de 1 minuto por filtro de status.</summary>
    public async Task<ResultWrapper<IReadOnlyList<LeitoDto>>> MapaAsync(string? status, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(status) && status != "Todos" && !StatusLeito.Todos.Contains(status))
            return ResultWrapper<IReadOnlyList<LeitoDto>>.Fail(ErrorType.Validation, "Filtro de status inválido.",
                new[] { $"Use: Todos, {string.Join(", ", StatusLeito.Todos)}." });

        var filtro = status == "Todos" ? null : status;
        var mapa = await cache.ObterOuCriarAsync(CacheKeys.Leitos(filtro), c => queries.MapaAsync(filtro, c), CacheKeys.TempoLeitos, ct);
        return ResultWrapper<IReadOnlyList<LeitoDto>>.Ok(mapa, $"{mapa.Count} leito(s) encontrado(s).");
    }

    public async Task<ResultWrapper<LeitoDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var l = await repo.ObterPorIdAsync(id, ct);
        return l is null ? ResultWrapper<LeitoDto>.NotFound("Leito") : ResultWrapper<LeitoDto>.Ok(l.ToDto());
    }

    public async Task<ResultWrapper<LeitoDto>> CriarAsync(LeitoRequest r, CancellationToken ct = default)
    {
        var leito = Leito.Criar(r.Numero, r.Ala);
        if (await repo.NumeroExisteNaAlaAsync(leito.Numero, leito.Ala, null, ct))
            return ResultWrapper<LeitoDto>.Conflict("RN-LEI-01", $"Já existe o leito {leito.Numero} na ala {leito.Ala}.");

        leito.DefinirId(await repo.AdicionarAsync(leito, ct));
        Invalidar();
        return ResultWrapper<LeitoDto>.Ok(leito.ToDto(), "Leito cadastrado com sucesso.");
    }

    public async Task<ResultWrapper<LeitoDto>> AtualizarAsync(int id, LeitoRequest r, CancellationToken ct = default)
    {
        var leito = await repo.ObterPorIdAsync(id, ct);
        if (leito is null) return ResultWrapper<LeitoDto>.NotFound("Leito");

        leito.AtualizarDados(r.Numero, r.Ala);
        if (await repo.NumeroExisteNaAlaAsync(leito.Numero, leito.Ala, id, ct))
            return ResultWrapper<LeitoDto>.Conflict("RN-LEI-01", $"Já existe o leito {leito.Numero} na ala {leito.Ala}.");

        await repo.AtualizarAsync(leito, ct);
        Invalidar();
        return ResultWrapper<LeitoDto>.Ok(leito.ToDto(), "Leito atualizado com sucesso.");
    }

    public async Task<ResultWrapper<LeitoDto>> AlterarStatusAsync(int id, AlterarStatusLeitoRequest r, CancellationToken ct = default)
    {
        var leito = await repo.ObterPorIdAsync(id, ct);
        if (leito is null) return ResultWrapper<LeitoDto>.NotFound("Leito");

        leito.AlterarStatusManualmente(r.Status); // RN-LEI-03 / RN-LEI-04
        await repo.AtualizarAsync(leito, ct);
        Invalidar();
        return ResultWrapper<LeitoDto>.Ok(leito.ToDto(), $"Leito {leito.Numero} agora está {leito.Status}.");
    }

    public async Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default)
    {
        var leito = await repo.ObterPorIdAsync(id, ct);
        if (leito is null) return ResultWrapper<object?>.NotFound("Leito");
        if (leito.Status == StatusLeito.Ocupado || await repo.PossuiInternacoesAsync(id, ct))
            return ResultWrapper<object?>.RegraViolada("RN-LEI-05", "Leito ocupado ou com histórico de internações não pode ser excluído.");

        await repo.RemoverAsync(id, ct);
        Invalidar();
        return ResultWrapper.Ok("Leito removido com sucesso.");
    }

    private void Invalidar()
    {
        cache.RemoverPorPrefixo(CacheKeys.PrefixoLeitos);
        cache.Remover(CacheKeys.Dashboard);
    }
}

public interface IInternacaoService
{
    Task<ResultWrapper<IReadOnlyList<InternacaoDto>>> ListarAsync(bool somenteAtivas, CancellationToken ct = default);
    Task<ResultWrapper<InternacaoDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<InternacaoDto>> InternarAsync(InternarRequest r, CancellationToken ct = default);
    Task<ResultWrapper<InternacaoDto>> DarAltaAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<InternacaoDto>> TransferirAsync(int id, TransferirRequest r, CancellationToken ct = default);
}

public class InternacaoService(
    IInternacaoRepository repo,
    IInternacaoQueries queries,
    IPacienteRepository pacientes,
    ILeitoRepository leitos,
    IUnitOfWork uow,
    ICacheService cache,
    TimeProvider relogio) : IInternacaoService
{
    public async Task<ResultWrapper<IReadOnlyList<InternacaoDto>>> ListarAsync(bool somenteAtivas, CancellationToken ct = default)
    {
        var lista = await queries.ListarAsync(somenteAtivas, ct);
        return ResultWrapper<IReadOnlyList<InternacaoDto>>.Ok(lista, $"{lista.Count} internação(ões) encontrada(s).");
    }

    public async Task<ResultWrapper<InternacaoDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var i = await queries.ObterAsync(id, ct);
        return i is null ? ResultWrapper<InternacaoDto>.NotFound("Internação") : ResultWrapper<InternacaoDto>.Ok(i);
    }

    public async Task<ResultWrapper<InternacaoDto>> InternarAsync(InternarRequest r, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(r.IdPaciente, ct);
        if (paciente is null) return ResultWrapper<InternacaoDto>.RegraViolada("RN-INT-01", "Paciente não encontrado.");
        if (await repo.ObterAtivaPorPacienteAsync(paciente.Id, ct) is not null)
            return ResultWrapper<InternacaoDto>.RegraViolada("RN-INT-01", $"{paciente.Nome} já possui uma internação ativa.");

        var leito = await leitos.ObterPorIdAsync(r.IdLeito, ct);
        if (leito is null) return ResultWrapper<InternacaoDto>.NotFound("Leito");

        var internacao = Internacao.Internar(paciente, leito, relogio.Agora()); // RN-INT-02 (leito.Ocupar)

        await uow.ExecutarEmTransacaoAsync(async () =>                          // RN-INT-03
        {
            internacao.DefinirId(await repo.AdicionarAsync(internacao, ct));
            await leitos.AtualizarAsync(leito, ct);
        }, ct);

        Invalidar();
        return ResultWrapper<InternacaoDto>.Ok((await queries.ObterAsync(internacao.Id, ct))!,
            $"{paciente.Nome} internado(a) no leito {leito.Numero} ({leito.Ala}).");
    }

    public async Task<ResultWrapper<InternacaoDto>> DarAltaAsync(int id, CancellationToken ct = default)
    {
        var internacao = await repo.ObterPorIdAsync(id, ct);
        if (internacao is null) return ResultWrapper<InternacaoDto>.NotFound("Internação");
        var leito = (await leitos.ObterPorIdAsync(internacao.IdLeito, ct))!;

        internacao.DarAlta(leito, relogio.Agora()); // RN-INT-04

        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            await repo.AtualizarAsync(internacao, ct);
            await leitos.AtualizarAsync(leito, ct);
        }, ct);

        Invalidar();
        return ResultWrapper<InternacaoDto>.Ok((await queries.ObterAsync(id, ct))!,
            $"Alta registrada. Leito {leito.Numero} enviado para higienização.");
    }

    public async Task<ResultWrapper<InternacaoDto>> TransferirAsync(int id, TransferirRequest r, CancellationToken ct = default)
    {
        var internacao = await repo.ObterPorIdAsync(id, ct);
        if (internacao is null) return ResultWrapper<InternacaoDto>.NotFound("Internação");
        var atual = (await leitos.ObterPorIdAsync(internacao.IdLeito, ct))!;
        var novo = await leitos.ObterPorIdAsync(r.IdNovoLeito, ct);
        if (novo is null) return ResultWrapper<InternacaoDto>.NotFound("Leito de destino");

        internacao.Transferir(atual, novo); // RN-INT-05

        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            await repo.AtualizarAsync(internacao, ct);
            await leitos.AtualizarAsync(atual, ct);
            await leitos.AtualizarAsync(novo, ct);
        }, ct);

        Invalidar();
        return ResultWrapper<InternacaoDto>.Ok((await queries.ObterAsync(id, ct))!,
            $"Paciente transferido para o leito {novo.Numero} ({novo.Ala}).");
    }

    private void Invalidar()
    {
        cache.RemoverPorPrefixo(CacheKeys.PrefixoLeitos);
        cache.Remover(CacheKeys.Dashboard);
    }
}
