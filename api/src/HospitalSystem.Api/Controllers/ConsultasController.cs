using HospitalSystem.Api.Common;
using HospitalSystem.Api.Security;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Api.Controllers;

[Route("api/consultas")]
[Authorize]
public class ConsultasController(IConsultaService service) : ApiControllerBase
{
    /// <summary>Lista consultas, com filtros opcionais por data (yyyy-MM-dd) e status.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? data, [FromQuery] string? status, CancellationToken ct) =>
        Responder(await service.ListarAsync(data, status, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));

    /// <summary>Fila de espera para triagem (status "Aguardando Triagem").</summary>
    [HttpGet("fila-triagem")]
    public async Task<IActionResult> FilaTriagem(CancellationToken ct) => Responder(await service.FilaTriagemAsync(ct));

    /// <summary>Fila de atendimento ordenada pelo Protocolo de Manchester (RN-TRI-06/07).</summary>
    [HttpGet("fila-atendimento")]
    public async Task<IActionResult> FilaAtendimento(CancellationToken ct) => Responder(await service.FilaAtendimentoAsync(ct));

    /// <summary>Agenda uma consulta (RN-CNS-01..05).</summary>
    [HttpPost]
    [Authorize(Roles = Papeis.Recepcao)]
    public async Task<IActionResult> Agendar([FromBody] AgendarConsultaRequest r, CancellationToken ct) => Criado(await service.AgendarAsync(r, ct));

    [HttpPatch("{id:int}/enviar-triagem")]
    [Authorize(Roles = Papeis.Recepcao)]
    public async Task<IActionResult> EnviarParaTriagem(int id, CancellationToken ct) => Responder(await service.EnviarParaTriagemAsync(id, ct));

    [HttpPatch("{id:int}/cancelar")]
    [Authorize(Roles = Papeis.Recepcao)]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct) => Responder(await service.CancelarAsync(id, ct));

    [HttpPatch("{id:int}/nao-compareceu")]
    [Authorize(Roles = Papeis.Recepcao)]
    public async Task<IActionResult> NaoCompareceu(int id, CancellationToken ct) => Responder(await service.MarcarNaoCompareceuAsync(id, ct));

    /// <summary>Finaliza o atendimento (RN-CNS-10).</summary>
    [HttpPatch("{id:int}/liberar")]
    [Authorize(Roles = Papeis.Clinico)]
    public async Task<IActionResult> Liberar(int id, CancellationToken ct) => Responder(await service.LiberarAsync(id, ct));

    /// <summary>Registra a triagem; a classificação de risco é calculada automaticamente (RN-TRI-04).</summary>
    [HttpPost("{id:int}/triagem")]
    [Authorize(Roles = Papeis.Assistencial)]
    public async Task<IActionResult> Triagem(int id, [FromBody] TriagemRequest r, CancellationToken ct) =>
        Criado(await service.RegistrarTriagemAsync(id, r, ct));

    /// <summary>Registra evolução clínica no prontuário (RN-PRT-01..04).</summary>
    [HttpPost("{id:int}/prontuario")]
    [Authorize(Roles = Papeis.Assistencial)]
    public async Task<IActionResult> RegistrarEvolucao(int id, [FromBody] RegistroProntuarioRequest r, CancellationToken ct) =>
        Criado(await service.RegistrarEvolucaoAsync(id, r, ct));
}

[Route("api")]
[Authorize]
public class ProntuariosController(IProntuarioService service) : ApiControllerBase
{
    /// <summary>Prescreve medicamento (somente Médico — RN-PRE-01), com verificação de alergia e interação.</summary>
    [HttpPost("prontuarios/{idProntuario:int}/prescricoes")]
    [Authorize(Roles = Papeis.Medico)]
    public async Task<IActionResult> Prescrever(int idProntuario, [FromBody] PrescricaoRequest r, CancellationToken ct) =>
        Criado(await service.PrescreverAsync(idProntuario, r, ct));

    [HttpPost("prontuarios/{idProntuario:int}/exames")]
    [Authorize(Roles = Papeis.Clinico)]
    public async Task<IActionResult> SolicitarExame(int idProntuario, [FromBody] ExameRequest r, CancellationToken ct) =>
        Criado(await service.SolicitarExameAsync(idProntuario, r, ct));

    [HttpPatch("exames/{idExame:int}/resultado")]
    [Authorize(Roles = Papeis.Clinico)]
    public async Task<IActionResult> RegistrarResultado(int idExame, [FromBody] ResultadoExameRequest r, CancellationToken ct) =>
        Responder(await service.RegistrarResultadoExameAsync(idExame, r, ct));
}
