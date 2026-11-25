using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;

namespace HospitalSystem.Application.Services;

public interface IInsumoService
{
    Task<ResultWrapper<IReadOnlyList<InsumoDto>>> ListarAsync(CancellationToken ct = default);
    Task<ResultWrapper<InsumoDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<InsumoDto>> CriarAsync(CriarInsumoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<InsumoDto>> AtualizarAsync(int id, AtualizarInsumoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<InsumoDto>> MovimentarAsync(int id, MovimentacaoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<InsumoDto>> RegistrarUsoAsync(int id, UsoInsumoRequest r, CancellationToken ct = default);
}

public class InsumoService(IInsumoRepository repo, ILeitoRepository leitos, IInternacaoRepository internacoes, IUnitOfWork uow, TimeProvider relogio) : IInsumoService
{
    public async Task<ResultWrapper<IReadOnlyList<InsumoDto>>> ListarAsync(CancellationToken ct = default)
    {
        var lista = (await repo.ListarAsync(ct)).Select(i => i.ToDto()).ToList();
        var criticos = lista.Count(i => i.EstoqueCritico);
        return ResultWrapper<IReadOnlyList<InsumoDto>>.Ok(lista, $"{lista.Count} insumo(s), {criticos} em estoque crítico.");
    }

    public async Task<ResultWrapper<InsumoDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var i = await repo.ObterPorIdAsync(id, ct);
        return i is null ? ResultWrapper<InsumoDto>.NotFound("Insumo") : ResultWrapper<InsumoDto>.Ok(i.ToDto());
    }

    public async Task<ResultWrapper<InsumoDto>> CriarAsync(CriarInsumoRequest r, CancellationToken ct = default)
    {
        var insumo = Insumo.Criar(r.Nome, r.QuantidadeInicial, r.ValorUnitario);
        if (await repo.NomeExisteAsync(insumo.Nome, null, ct)) return ResultWrapper<InsumoDto>.Conflict("RN-EST-01", "Já existe insumo com este nome.");

        insumo.DefinirId(await repo.AdicionarAsync(insumo, ct));
        return ResultWrapper<InsumoDto>.Ok(insumo.ToDto(), "Insumo cadastrado com sucesso.");
    }

    public async Task<ResultWrapper<InsumoDto>> AtualizarAsync(int id, AtualizarInsumoRequest r, CancellationToken ct = default)
    {
        var insumo = await repo.ObterPorIdAsync(id, ct);
        if (insumo is null) return ResultWrapper<InsumoDto>.NotFound("Insumo");

        insumo.AtualizarDados(r.Nome, r.ValorUnitario);
        if (await repo.NomeExisteAsync(insumo.Nome, id, ct)) return ResultWrapper<InsumoDto>.Conflict("RN-EST-01", "Já existe outro insumo com este nome.");

        await repo.AtualizarAsync(insumo, ct);
        return ResultWrapper<InsumoDto>.Ok(insumo.ToDto(), "Insumo atualizado com sucesso.");
    }

    public async Task<ResultWrapper<InsumoDto>> MovimentarAsync(int id, MovimentacaoRequest r, CancellationToken ct = default)
    {
        var insumo = await repo.ObterPorIdAsync(id, ct);
        if (insumo is null) return ResultWrapper<InsumoDto>.NotFound("Insumo");

        switch (r.Tipo?.Trim().ToLowerInvariant())
        {
            case "entrada": insumo.Entrada(r.Quantidade); break;
            case "saida" or "saída": insumo.Saida(r.Quantidade); break;
            default:
                return ResultWrapper<InsumoDto>.Fail(ErrorType.Validation, "Tipo de movimentação inválido.", new[] { "Use 'entrada' ou 'saida'." });
        }

        await repo.AtualizarAsync(insumo, ct);
        var aviso = insumo.EstoqueCritico ? " Atenção: estoque crítico (RN-EST-03)." : string.Empty;
        return ResultWrapper<InsumoDto>.Ok(insumo.ToDto(), $"Estoque ajustado: {insumo.QuantidadeEstoque} unidade(s).{aviso}");
    }

    public async Task<ResultWrapper<InsumoDto>> RegistrarUsoAsync(int id, UsoInsumoRequest r, CancellationToken ct = default)
    {
        var insumo = await repo.ObterPorIdAsync(id, ct);
        if (insumo is null) return ResultWrapper<InsumoDto>.NotFound("Insumo");
        var leito = await leitos.ObterPorIdAsync(r.IdLeito, ct);
        if (leito is null) return ResultWrapper<InsumoDto>.NotFound("Leito");

        var internacao = await internacoes.ObterAtivaPorLeitoAsync(leito.Id, ct);
        var uso = UsoInsumo.Registrar(insumo, leito, internacao, r.Quantidade, relogio.Agora()); // RN-EST-04

        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            await repo.AdicionarUsoAsync(uso, ct);
            await repo.AtualizarAsync(insumo, ct);
        }, ct);

        return ResultWrapper<InsumoDto>.Ok(insumo.ToDto(), $"Uso de {r.Quantidade} {insumo.Nome} lançado no leito {leito.Numero}.");
    }
}
