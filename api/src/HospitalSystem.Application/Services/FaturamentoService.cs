using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;
using HospitalSystem.Domain.Services;

namespace HospitalSystem.Application.Services;

public interface IFaturamentoService
{
    Task<ResultWrapper<IReadOnlyList<FaturamentoDto>>> ListarAsync(string? status, CancellationToken ct = default);
    Task<ResultWrapper<FaturamentoDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<ResumoFaturamentoDto>> ResumoAsync(CancellationToken ct = default);
    Task<ResultWrapper<FaturamentoDto>> EmitirAsync(EmitirGuiaRequest r, CancellationToken ct = default);
    Task<ResultWrapper<FaturamentoDto>> PagarAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<FaturamentoDto>> EnviarParaAuditoriaAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<FaturamentoDto>> AprovarAuditoriaAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<FaturamentoDto>> CancelarAsync(int id, CancelarGuiaRequest r, CancellationToken ct = default);
}

public class FaturamentoService(
    IFaturamentoRepository repo,
    IFaturamentoQueries queries,
    IPacienteRepository pacientes,
    IUnitOfWork uow,
    IUsuarioAtual usuarioAtual,
    TimeProvider relogio) : IFaturamentoService
{
    public async Task<ResultWrapper<IReadOnlyList<FaturamentoDto>>> ListarAsync(string? status, CancellationToken ct = default)
    {
        var lista = await queries.ListarAsync(status, ct);
        return ResultWrapper<IReadOnlyList<FaturamentoDto>>.Ok(lista, $"{lista.Count} guia(s) encontrada(s).");
    }

    public async Task<ResultWrapper<FaturamentoDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var f = await queries.ObterAsync(id, ct);
        return f is null ? ResultWrapper<FaturamentoDto>.NotFound("Guia de faturamento") : ResultWrapper<FaturamentoDto>.Ok(f);
    }

    /// <summary>RN-FAT-07: indicadores do mês corrente.</summary>
    public async Task<ResultWrapper<ResumoFaturamentoDto>> ResumoAsync(CancellationToken ct = default)
    {
        var hoje = relogio.Hoje();
        var inicio = new DateOnly(hoje.Year, hoje.Month, 1);
        var resumo = await queries.ResumoAsync(inicio, inicio.AddMonths(1).AddDays(-1), ct);
        return ResultWrapper<ResumoFaturamentoDto>.Ok(resumo);
    }

    public async Task<ResultWrapper<FaturamentoDto>> EmitirAsync(EmitirGuiaRequest r, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(r.IdPaciente, ct);
        if (paciente is null) return ResultWrapper<FaturamentoDto>.RegraViolada("RN-FAT-01", "Paciente não encontrado.");

        var agora = relogio.Agora();
        var valor = r.Valor;
        if (valor is null)                                                  // RN-FAT-03
        {
            var (insumos, exames, prescricoes) = await queries.DadosConsolidacaoAsync(paciente.Id, agora, ct);
            valor = new ConsolidacaoGastos(insumos, exames, prescricoes).ValorTotal;
            if (valor <= 0)
                return ResultWrapper<FaturamentoDto>.RegraViolada("RN-FAT-03", "Não há gastos consolidados para este paciente; informe o valor da guia.");
        }

        var guia = Faturamento.Emitir(paciente.Id, valor.Value, DateOnly.FromDateTime(agora)); // RN-FAT-01 / RN-FAT-04

        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            guia.DefinirId(await repo.AdicionarAsync(guia, ct));
            if (guia.EmAuditoria)
            {
                await repo.AdicionarAuditoriaAsync(Auditoria.Registrar(guia.Id,
                    $"Guia de {Moeda.Formatar(guia.ValorTotal)} enviada automaticamente para auditoria (valor igual ou acima de {Moeda.Formatar(Faturamento.LimiteAuditoria)}).",
                    "Aberta", agora), ct);
            }
        }, ct);

        var msg = guia.EmAuditoria
            ? "Guia emitida e enviada automaticamente para auditoria (RN-FAT-04)."
            : "Guia emitida com sucesso.";
        return ResultWrapper<FaturamentoDto>.Ok((await queries.ObterAsync(guia.Id, ct))!, msg);
    }

    public Task<ResultWrapper<FaturamentoDto>> PagarAsync(int id, CancellationToken ct = default) =>
        MudarAsync(id, g => g.Pagar(relogio.Hoje()), null, "Pagamento registrado (guia baixada).", ct);

    public Task<ResultWrapper<FaturamentoDto>> EnviarParaAuditoriaAsync(int id, CancellationToken ct = default) =>
        MudarAsync(id, g => g.EnviarParaAuditoria(), "Guia enviada manualmente para auditoria", "Guia enviada para auditoria.", ct);

    public Task<ResultWrapper<FaturamentoDto>> AprovarAuditoriaAsync(int id, CancellationToken ct = default) =>
        MudarAsync(id, g => g.AprovarAuditoria(), "Auditoria aprovada; guia liberada para pagamento", "Auditoria aprovada. Guia voltou para Pendente.", ct);

    public Task<ResultWrapper<FaturamentoDto>> CancelarAsync(int id, CancelarGuiaRequest r, CancellationToken ct = default) =>
        MudarAsync(id, g => g.Cancelar(r.Motivo), $"Guia cancelada. Motivo: {r.Motivo}", "Guia cancelada.", ct);

    private async Task<ResultWrapper<FaturamentoDto>> MudarAsync(int id, Action<Faturamento> acao, string? registroAuditoria,
        string mensagem, CancellationToken ct)
    {
        var guia = await repo.ObterPorIdAsync(id, ct);
        if (guia is null) return ResultWrapper<FaturamentoDto>.NotFound("Guia de faturamento");

        acao(guia); // RN-FAT-05 / RN-FAT-06

        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            await repo.AtualizarAsync(guia, ct);
            if (registroAuditoria is not null)
            {
                await repo.AdicionarAuditoriaAsync(Auditoria.Registrar(guia.Id,
                    $"{registroAuditoria} (por {usuarioAtual.Nome}).",
                    guia.Status == StatusPagamento.Auditoria ? "Aberta" : "Concluída", relogio.Agora()), ct);
            }
        }, ct);

        return ResultWrapper<FaturamentoDto>.Ok((await queries.ObterAsync(id, ct))!, mensagem);
    }
}
