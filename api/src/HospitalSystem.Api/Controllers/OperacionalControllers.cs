using HospitalSystem.Api.Common;
using HospitalSystem.Api.Security;
using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Api.Controllers;

[Route("api/leitos")]
[Authorize]
public class LeitosController(ILeitoService service) : ApiControllerBase
{
    /// <summary>Mapa de leitos (com cache de 1 minuto). Filtro: Todos, Disponível, Ocupado, Higienização, Manutenção.</summary>
    [HttpGet]
    public async Task<IActionResult> Mapa([FromQuery] string? status, CancellationToken ct) => Responder(await service.MapaAsync(status, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));

    [HttpPost, Authorize(Roles = Papeis.Admin)]
    public async Task<IActionResult> Criar([FromBody] LeitoRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));

    [HttpPut("{id:int}"), Authorize(Roles = Papeis.Admin)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] LeitoRequest r, CancellationToken ct) => Responder(await service.AtualizarAsync(id, r, ct));

    /// <summary>Mudança manual de status (RN-LEI-03/04): não permite ocupar nem liberar leito ocupado.</summary>
    [HttpPatch("{id:int}/status"), Authorize(Roles = Papeis.Assistencial)]
    public async Task<IActionResult> AlterarStatus(int id, [FromBody] AlterarStatusLeitoRequest r, CancellationToken ct) =>
        Responder(await service.AlterarStatusAsync(id, r, ct));

    [HttpDelete("{id:int}"), Authorize(Roles = Papeis.Admin)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct) => Responder(await service.RemoverAsync(id, ct));
}

[Route("api/internacoes")]
[Authorize]
public class InternacoesController(IInternacaoService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool somenteAtivas = true, CancellationToken ct = default) =>
        Responder(await service.ListarAsync(somenteAtivas, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));

    /// <summary>Interna o paciente: leito vira Ocupado na mesma transação (RN-INT-01..03).</summary>
    [HttpPost, Authorize(Roles = Papeis.Assistencial)]
    public async Task<IActionResult> Internar([FromBody] InternarRequest r, CancellationToken ct) => Criado(await service.InternarAsync(r, ct));

    /// <summary>Alta: leito vai para Higienização (RN-INT-04).</summary>
    [HttpPatch("{id:int}/alta"), Authorize(Roles = Papeis.Clinico)]
    public async Task<IActionResult> Alta(int id, CancellationToken ct) => Responder(await service.DarAltaAsync(id, ct));

    [HttpPatch("{id:int}/transferencia"), Authorize(Roles = Papeis.Assistencial)]
    public async Task<IActionResult> Transferir(int id, [FromBody] TransferirRequest r, CancellationToken ct) =>
        Responder(await service.TransferirAsync(id, r, ct));
}

[Route("api/insumos")]
[Authorize]
public class InsumosController(IInsumoService service) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(CancellationToken ct) => Responder(await service.ListarAsync(ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));

    [HttpPost, Authorize(Roles = Papeis.Estoque)]
    public async Task<IActionResult> Criar([FromBody] CriarInsumoRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));

    /// <summary>Altera nome/valor. A quantidade só muda por movimentação (RN-EST-05).</summary>
    [HttpPut("{id:int}"), Authorize(Roles = Papeis.Estoque)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarInsumoRequest r, CancellationToken ct) => Responder(await service.AtualizarAsync(id, r, ct));

    /// <summary>Entrada ou saída de estoque (RN-EST-02).</summary>
    [HttpPost("{id:int}/movimentacoes"), Authorize(Roles = Papeis.Estoque)]
    public async Task<IActionResult> Movimentar(int id, [FromBody] MovimentacaoRequest r, CancellationToken ct) => Responder(await service.MovimentarAsync(id, r, ct));

    /// <summary>Lança uso do insumo em um leito ocupado (RN-EST-04).</summary>
    [HttpPost("{id:int}/usos"), Authorize(Roles = Papeis.Estoque)]
    public async Task<IActionResult> RegistrarUso(int id, [FromBody] UsoInsumoRequest r, CancellationToken ct) => Responder(await service.RegistrarUsoAsync(id, r, ct));
}

[Route("api/faturamentos")]
[Authorize(Roles = Papeis.Financeiro)]
public class FaturamentosController(IFaturamentoService service) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar([FromQuery] string? status, CancellationToken ct) => Responder(await service.ListarAsync(status, ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));

    /// <summary>Indicadores: pendente, recebido no mês e em auditoria (RN-FAT-07).</summary>
    [HttpGet("resumo")] public async Task<IActionResult> Resumo(CancellationToken ct) => Responder(await service.ResumoAsync(ct));

    /// <summary>Emite guia; sem valor usa a consolidação de gastos (RN-FAT-03); ≥ R$ 10.000 vai para auditoria (RN-FAT-04).</summary>
    [HttpPost] public async Task<IActionResult> Emitir([FromBody] EmitirGuiaRequest r, CancellationToken ct) => Criado(await service.EmitirAsync(r, ct));

    [HttpPatch("{id:int}/pagar")] public async Task<IActionResult> Pagar(int id, CancellationToken ct) => Responder(await service.PagarAsync(id, ct));
    [HttpPatch("{id:int}/auditoria")] public async Task<IActionResult> Auditoria(int id, CancellationToken ct) => Responder(await service.EnviarParaAuditoriaAsync(id, ct));
    [HttpPatch("{id:int}/aprovar")] public async Task<IActionResult> Aprovar(int id, CancellationToken ct) => Responder(await service.AprovarAuditoriaAsync(id, ct));
    [HttpPatch("{id:int}/cancelar")] public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarGuiaRequest r, CancellationToken ct) => Responder(await service.CancelarAsync(id, r, ct));
}

[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService service) : ApiControllerBase
{
    /// <summary>Indicadores da página inicial (cache de 30 segundos).</summary>
    [HttpGet] public async Task<IActionResult> Obter(CancellationToken ct) => Responder(await service.ObterAsync(ct));
}

[Route("api/cache")]
[Authorize(Roles = Papeis.Admin)]
public class CacheController(ICacheService cache) : ApiControllerBase
{
    /// <summary>Invalida todo o cache manualmente (demonstração / manutenção).</summary>
    [HttpDelete]
    public IActionResult Limpar()
    {
        cache.Limpar();
        return Responder(ResultWrapper.Ok("Cache limpo. As próximas leituras virão do banco de dados."));
    }
}
