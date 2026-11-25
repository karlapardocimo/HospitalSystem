using HospitalSystem.Api.Common;
using HospitalSystem.Api.Security;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Api.Controllers;

[Route("api/pacientes")]
[Authorize]
public class PacientesController(IPacienteService service) : ApiControllerBase
{
    /// <summary>Lista pacientes; <paramref name="busca"/> filtra por nome ou CPF.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca, CancellationToken ct) => Responder(await service.ListarAsync(busca, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));

    /// <summary>Cadastra o paciente e, por padrão, o envia para a fila de triagem (RN-PAC-07).</summary>
    [HttpPost]
    [Authorize(Roles = Papeis.CadastroPaciente)]
    public async Task<IActionResult> Criar([FromBody] PacienteRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Papeis.CadastroPaciente)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] PacienteRequest r, CancellationToken ct) =>
        Responder(await service.AtualizarAsync(id, r, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Papeis.Admin)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct) => Responder(await service.RemoverAsync(id, ct));

    /// <summary>Prontuário eletrônico: triagens, evoluções, prescrições e exames do paciente.</summary>
    [HttpGet("{id:int}/prontuario")]
    public async Task<IActionResult> Prontuario(int id, CancellationToken ct) => Responder(await service.ProntuarioAsync(id, ct));

    /// <summary>Consolidação de gastos do paciente (RN-FAT-02).</summary>
    [HttpGet("{id:int}/gastos")]
    [Authorize(Roles = Papeis.Financeiro)]
    public async Task<IActionResult> Gastos(int id, CancellationToken ct) => Responder(await service.GastosAsync(id, ct));
}
