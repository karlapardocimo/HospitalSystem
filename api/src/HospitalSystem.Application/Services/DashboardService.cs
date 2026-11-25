using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;

namespace HospitalSystem.Application.Services;

public interface IDashboardService
{
    Task<ResultWrapper<DashboardDto>> ObterAsync(CancellationToken ct = default);
}

/// <summary>
/// O dashboard roda 4 contagens + 2 listagens a cada acesso à página inicial.
/// Com cache de 30 s, várias pessoas abrindo a tela ao mesmo tempo geram 1 ida ao banco.
/// Qualquer alteração em consultas, leitos ou internações invalida a chave na hora.
/// </summary>
public class DashboardService(IDashboardQueries queries, ICacheService cache, TimeProvider relogio) : IDashboardService
{
    public async Task<ResultWrapper<DashboardDto>> ObterAsync(CancellationToken ct = default)
    {
        var dto = await cache.ObterOuCriarAsync(CacheKeys.Dashboard, c => queries.ObterAsync(relogio.Hoje(), c), CacheKeys.TempoDashboard, ct);
        return ResultWrapper<DashboardDto>.Ok(dto, $"Indicadores gerados em {dto.GeradoEm:dd/MM/yyyy HH:mm:ss}.");
    }
}
