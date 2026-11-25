using HospitalSystem.Api.Common;
using HospitalSystem.Api.Security;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Api.Controllers;

// Leitura liberada a qualquer usuário autenticado; escrita só para Administrador.
// As listagens destes controllers usam CACHE (veja o cabeçalho X-Cache na resposta).

[Route("api/medicos")]
[Authorize]
public class MedicosController(IMedicoService service) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(CancellationToken ct) => Responder(await service.ListarAsync(ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));
    [HttpPost, Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Criar([FromBody] MedicoRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));
    [HttpPut("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Atualizar(int id, [FromBody] MedicoRequest r, CancellationToken ct) => Responder(await service.AtualizarAsync(id, r, ct));
    [HttpDelete("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Remover(int id, CancellationToken ct) => Responder(await service.RemoverAsync(id, ct));
}

[Route("api/convenios")]
[Authorize]
public class ConveniosController(IConvenioService service) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(CancellationToken ct) => Responder(await service.ListarAsync(ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));
    [HttpPost, Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Criar([FromBody] ConvenioRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));
    [HttpPut("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Atualizar(int id, [FromBody] ConvenioRequest r, CancellationToken ct) => Responder(await service.AtualizarAsync(id, r, ct));
    [HttpDelete("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Remover(int id, CancellationToken ct) => Responder(await service.RemoverAsync(id, ct));
}

[Route("api/salas")]
[Authorize]
public class SalasController(ISalaService service) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(CancellationToken ct) => Responder(await service.ListarAsync(ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));
    /// <summary>Cadastra sala; com vincularLeito = true cria também um leito (RN-SAL-04).</summary>
    [HttpPost, Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Criar([FromBody] CriarSalaRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));
    [HttpPut("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarSalaRequest r, CancellationToken ct) => Responder(await service.AtualizarAsync(id, r, ct));
    [HttpDelete("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Remover(int id, CancellationToken ct) => Responder(await service.RemoverAsync(id, ct));
}

[Route("api/medicamentos")]
[Authorize]
public class MedicamentosController(IMedicamentoService service) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(CancellationToken ct) => Responder(await service.ListarAsync(ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));
    [HttpPost, Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Criar([FromBody] MedicamentoRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));
    [HttpPut("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Atualizar(int id, [FromBody] MedicamentoRequest r, CancellationToken ct) => Responder(await service.AtualizarAsync(id, r, ct));
    [HttpDelete("{id:int}"), Authorize(Roles = Papeis.Admin)] public async Task<IActionResult> Remover(int id, CancellationToken ct) => Responder(await service.RemoverAsync(id, ct));
}
