using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;

namespace HospitalSystem.Application.Services;

// Cadastros de apoio (médicos, convênios, salas e medicamentos).
// São lidos o tempo todo (combos de agendamento, prescrição...) e mudam pouco:
// por isso a LISTAGEM usa cache e toda escrita INVALIDA a chave correspondente.

public interface IMedicoService
{
    Task<ResultWrapper<IReadOnlyList<MedicoDto>>> ListarAsync(CancellationToken ct = default);
    Task<ResultWrapper<MedicoDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<MedicoDto>> CriarAsync(MedicoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<MedicoDto>> AtualizarAsync(int id, MedicoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default);
}

public class MedicoService(IMedicoRepository repo, ICacheService cache) : IMedicoService
{
    public async Task<ResultWrapper<IReadOnlyList<MedicoDto>>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await cache.ObterOuCriarAsync<IReadOnlyList<MedicoDto>>(CacheKeys.Medicos,
            async c => (await repo.ListarAsync(c)).Select(m => m.ToDto()).ToList(), CacheKeys.TempoCatalogo, ct);
        return ResultWrapper<IReadOnlyList<MedicoDto>>.Ok(lista, $"{lista.Count} médico(s) encontrado(s).");
    }

    public async Task<ResultWrapper<MedicoDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var m = await repo.ObterPorIdAsync(id, ct);
        return m is null ? ResultWrapper<MedicoDto>.NotFound("Médico") : ResultWrapper<MedicoDto>.Ok(m.ToDto());
    }

    public async Task<ResultWrapper<MedicoDto>> CriarAsync(MedicoRequest r, CancellationToken ct = default)
    {
        var m = Medico.Criar(r.Nome, r.Crm, r.Especialidade, r.Telefone, r.Email);
        if (await repo.CrmExisteAsync(m.Crm, null, ct)) return ResultWrapper<MedicoDto>.Conflict("RN-MED-03", "Já existe médico com este CRM.");

        m.DefinirId(await repo.AdicionarAsync(m, ct));
        cache.Remover(CacheKeys.Medicos);
        return ResultWrapper<MedicoDto>.Ok(m.ToDto(), "Médico cadastrado com sucesso.");
    }

    public async Task<ResultWrapper<MedicoDto>> AtualizarAsync(int id, MedicoRequest r, CancellationToken ct = default)
    {
        var m = await repo.ObterPorIdAsync(id, ct);
        if (m is null) return ResultWrapper<MedicoDto>.NotFound("Médico");

        m.Atualizar(r.Nome, r.Crm, r.Especialidade, r.Telefone, r.Email);
        if (await repo.CrmExisteAsync(m.Crm, id, ct)) return ResultWrapper<MedicoDto>.Conflict("RN-MED-03", "Já existe outro médico com este CRM.");

        await repo.AtualizarAsync(m, ct);
        cache.Remover(CacheKeys.Medicos);
        return ResultWrapper<MedicoDto>.Ok(m.ToDto(), "Dados do médico atualizados.");
    }

    public async Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default)
    {
        if (await repo.ObterPorIdAsync(id, ct) is null) return ResultWrapper<object?>.NotFound("Médico");
        if (await repo.PossuiConsultasAsync(id, ct))
            return ResultWrapper<object?>.RegraViolada("RN-MED-06", "O médico possui consultas vinculadas e não pode ser excluído.");

        await repo.RemoverAsync(id, ct);
        cache.Remover(CacheKeys.Medicos);
        return ResultWrapper.Ok("Médico removido do sistema.");
    }
}

public interface IConvenioService
{
    Task<ResultWrapper<IReadOnlyList<ConvenioDto>>> ListarAsync(CancellationToken ct = default);
    Task<ResultWrapper<ConvenioDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ConvenioDto>> CriarAsync(ConvenioRequest r, CancellationToken ct = default);
    Task<ResultWrapper<ConvenioDto>> AtualizarAsync(int id, ConvenioRequest r, CancellationToken ct = default);
    Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default);
}

public class ConvenioService(IConvenioRepository repo, ICacheService cache, TimeProvider relogio) : IConvenioService
{
    public async Task<ResultWrapper<IReadOnlyList<ConvenioDto>>> ListarAsync(CancellationToken ct = default)
    {
        var hoje = relogio.Hoje();
        var lista = await cache.ObterOuCriarAsync<IReadOnlyList<ConvenioDto>>(CacheKeys.Convenios,
            async c => (await repo.ListarAsync(c)).Select(x => x.ToDto(hoje)).ToList(), CacheKeys.TempoCatalogo, ct);
        return ResultWrapper<IReadOnlyList<ConvenioDto>>.Ok(lista, $"{lista.Count} convênio(s) encontrado(s).");
    }

    public async Task<ResultWrapper<ConvenioDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var c = await repo.ObterPorIdAsync(id, ct);
        return c is null ? ResultWrapper<ConvenioDto>.NotFound("Convênio") : ResultWrapper<ConvenioDto>.Ok(c.ToDto(relogio.Hoje()));
    }

    public async Task<ResultWrapper<ConvenioDto>> CriarAsync(ConvenioRequest r, CancellationToken ct = default)
    {
        var c = Convenio.Criar(r.Nome, r.ValidadeCarteira, r.Telefone);
        if (await repo.NomeExisteAsync(c.Nome, null, ct)) return ResultWrapper<ConvenioDto>.Conflict("RN-CON-01", "Já existe convênio com este nome.");

        c.DefinirId(await repo.AdicionarAsync(c, ct));
        cache.Remover(CacheKeys.Convenios);
        return ResultWrapper<ConvenioDto>.Ok(c.ToDto(relogio.Hoje()), "Convênio cadastrado com sucesso.");
    }

    public async Task<ResultWrapper<ConvenioDto>> AtualizarAsync(int id, ConvenioRequest r, CancellationToken ct = default)
    {
        var c = await repo.ObterPorIdAsync(id, ct);
        if (c is null) return ResultWrapper<ConvenioDto>.NotFound("Convênio");

        c.Atualizar(r.Nome, r.ValidadeCarteira, r.Telefone);
        if (await repo.NomeExisteAsync(c.Nome, id, ct)) return ResultWrapper<ConvenioDto>.Conflict("RN-CON-01", "Já existe outro convênio com este nome.");

        await repo.AtualizarAsync(c, ct);
        cache.Remover(CacheKeys.Convenios);
        return ResultWrapper<ConvenioDto>.Ok(c.ToDto(relogio.Hoje()), "Convênio atualizado com sucesso.");
    }

    public async Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default)
    {
        if (await repo.ObterPorIdAsync(id, ct) is null) return ResultWrapper<object?>.NotFound("Convênio");
        if (await repo.PossuiConsultasAsync(id, ct))
            return ResultWrapper<object?>.RegraViolada("RN-CON-02", "O convênio possui consultas vinculadas e não pode ser excluído.");

        await repo.RemoverAsync(id, ct);
        cache.Remover(CacheKeys.Convenios);
        return ResultWrapper.Ok("Convênio removido com sucesso.");
    }
}

public interface ISalaService
{
    Task<ResultWrapper<IReadOnlyList<SalaDto>>> ListarAsync(CancellationToken ct = default);
    Task<ResultWrapper<SalaDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<SalaDto>> CriarAsync(CriarSalaRequest r, CancellationToken ct = default);
    Task<ResultWrapper<SalaDto>> AtualizarAsync(int id, AtualizarSalaRequest r, CancellationToken ct = default);
    Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default);
}

public class SalaService(ISalaRepository repo, ILeitoRepository leitos, IUnitOfWork uow, ICacheService cache) : ISalaService
{
    public async Task<ResultWrapper<IReadOnlyList<SalaDto>>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await cache.ObterOuCriarAsync<IReadOnlyList<SalaDto>>(CacheKeys.Salas,
            async c => (await repo.ListarAsync(c)).Select(s => s.ToDto()).ToList(), CacheKeys.TempoCatalogo, ct);
        return ResultWrapper<IReadOnlyList<SalaDto>>.Ok(lista, $"{lista.Count} sala(s) encontrada(s).");
    }

    public async Task<ResultWrapper<SalaDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var s = await repo.ObterPorIdAsync(id, ct);
        return s is null ? ResultWrapper<SalaDto>.NotFound("Sala") : ResultWrapper<SalaDto>.Ok(s.ToDto());
    }

    public async Task<ResultWrapper<SalaDto>> CriarAsync(CriarSalaRequest r, CancellationToken ct = default)
    {
        var sala = Sala.Criar(r.Numero, r.Tipo);
        if (await repo.NumeroExisteAsync(sala.Numero, null, ct)) return ResultWrapper<SalaDto>.Conflict("RN-SAL-01", "Já existe sala com este número.");

        Leito? leito = null;
        if (r.VincularLeito)
        {
            if (string.IsNullOrWhiteSpace(r.Ala))
                return ResultWrapper<SalaDto>.RegraViolada("RN-SAL-04", "Informe a ala para gerar o leito vinculado.");
            leito = Leito.Criar(sala.Numero, r.Ala);
            if (await leitos.NumeroExisteNaAlaAsync(leito.Numero, leito.Ala, null, ct))
                return ResultWrapper<SalaDto>.Conflict("RN-LEI-01", $"Já existe o leito {leito.Numero} na ala {leito.Ala}.");
        }

        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            sala.DefinirId(await repo.AdicionarAsync(sala, ct));
            if (leito is not null) leito.DefinirId(await leitos.AdicionarAsync(leito, ct));
        }, ct);

        cache.Remover(CacheKeys.Salas);
        if (leito is not null)
        {
            cache.RemoverPorPrefixo(CacheKeys.PrefixoLeitos);
            cache.Remover(CacheKeys.Dashboard);
        }

        return ResultWrapper<SalaDto>.Ok(sala.ToDto(leito?.Id),
            leito is null ? "Sala cadastrada com sucesso." : "Sala cadastrada e leito vinculado criado.");
    }

    public async Task<ResultWrapper<SalaDto>> AtualizarAsync(int id, AtualizarSalaRequest r, CancellationToken ct = default)
    {
        var s = await repo.ObterPorIdAsync(id, ct);
        if (s is null) return ResultWrapper<SalaDto>.NotFound("Sala");

        s.Atualizar(r.Numero, r.Tipo, r.Status);
        if (await repo.NumeroExisteAsync(s.Numero, id, ct)) return ResultWrapper<SalaDto>.Conflict("RN-SAL-01", "Já existe outra sala com este número.");

        await repo.AtualizarAsync(s, ct);
        cache.Remover(CacheKeys.Salas);
        return ResultWrapper<SalaDto>.Ok(s.ToDto(), "Sala atualizada com sucesso.");
    }

    public async Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default)
    {
        if (await repo.ObterPorIdAsync(id, ct) is null) return ResultWrapper<object?>.NotFound("Sala");
        if (await repo.PossuiConsultasAsync(id, ct))
            return ResultWrapper<object?>.RegraViolada("RN-SAL-06", "A sala possui consultas vinculadas e não pode ser excluída.");

        await repo.RemoverAsync(id, ct);
        cache.Remover(CacheKeys.Salas);
        return ResultWrapper.Ok("Sala removida com sucesso.");
    }
}

public interface IMedicamentoService
{
    Task<ResultWrapper<IReadOnlyList<MedicamentoDto>>> ListarAsync(CancellationToken ct = default);
    Task<ResultWrapper<MedicamentoDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<MedicamentoDto>> CriarAsync(MedicamentoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<MedicamentoDto>> AtualizarAsync(int id, MedicamentoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default);
}

public class MedicamentoService(IMedicamentoRepository repo, ICacheService cache) : IMedicamentoService
{
    public async Task<ResultWrapper<IReadOnlyList<MedicamentoDto>>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await cache.ObterOuCriarAsync<IReadOnlyList<MedicamentoDto>>(CacheKeys.Medicamentos,
            async c => (await repo.ListarAsync(c)).Select(m => m.ToDto()).ToList(), CacheKeys.TempoCatalogo, ct);
        return ResultWrapper<IReadOnlyList<MedicamentoDto>>.Ok(lista, $"{lista.Count} medicamento(s) encontrado(s).");
    }

    public async Task<ResultWrapper<MedicamentoDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var m = await repo.ObterPorIdAsync(id, ct);
        return m is null ? ResultWrapper<MedicamentoDto>.NotFound("Medicamento") : ResultWrapper<MedicamentoDto>.Ok(m.ToDto());
    }

    public async Task<ResultWrapper<MedicamentoDto>> CriarAsync(MedicamentoRequest r, CancellationToken ct = default)
    {
        var m = Medicamento.Criar(r.Nome, r.Contraindicacoes, r.InteracoesMedicamentosas);
        if (await repo.NomeExisteAsync(m.Nome, null, ct)) return ResultWrapper<MedicamentoDto>.Conflict("RN-PRE-02", "Já existe medicamento com este nome.");

        m.DefinirId(await repo.AdicionarAsync(m, ct));
        cache.Remover(CacheKeys.Medicamentos);
        return ResultWrapper<MedicamentoDto>.Ok(m.ToDto(), "Medicamento cadastrado com sucesso.");
    }

    public async Task<ResultWrapper<MedicamentoDto>> AtualizarAsync(int id, MedicamentoRequest r, CancellationToken ct = default)
    {
        var m = await repo.ObterPorIdAsync(id, ct);
        if (m is null) return ResultWrapper<MedicamentoDto>.NotFound("Medicamento");

        m.Atualizar(r.Nome, r.Contraindicacoes, r.InteracoesMedicamentosas);
        if (await repo.NomeExisteAsync(m.Nome, id, ct)) return ResultWrapper<MedicamentoDto>.Conflict("RN-PRE-02", "Já existe outro medicamento com este nome.");

        await repo.AtualizarAsync(m, ct);
        cache.Remover(CacheKeys.Medicamentos);
        return ResultWrapper<MedicamentoDto>.Ok(m.ToDto(), "Medicamento atualizado com sucesso.");
    }

    public async Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default)
    {
        if (await repo.ObterPorIdAsync(id, ct) is null) return ResultWrapper<object?>.NotFound("Medicamento");
        if (await repo.PossuiPrescricoesAsync(id, ct))
            return ResultWrapper<object?>.RegraViolada("RN-PRE-02", "O medicamento já foi prescrito e não pode ser excluído.");

        await repo.RemoverAsync(id, ct);
        cache.Remover(CacheKeys.Medicamentos);
        return ResultWrapper.Ok("Medicamento removido com sucesso.");
    }
}
