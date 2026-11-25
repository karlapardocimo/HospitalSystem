using HospitalSystem.Api.Common;
using HospitalSystem.Api.Security;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Api.Controllers;

/// <summary>Cadastro de usuários do sistema (somente Administrador — RN-USU-06).</summary>
[Route("api/usuarios")]
[Authorize(Roles = Papeis.Admin)]
public class UsuariosController(IUsuarioService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Responder(await service.ListarAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct) => Responder(await service.ObterAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest r, CancellationToken ct) => Criado(await service.CriarAsync(r, ct));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarUsuarioRequest r, CancellationToken ct) =>
        Responder(await service.AtualizarAsync(id, r, ct));

    [HttpPatch("{id:int}/senha")]
    public async Task<IActionResult> AlterarSenha(int id, [FromBody] AlterarSenhaRequest r, CancellationToken ct) =>
        Responder(await service.AlterarSenhaAsync(id, r, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct) => Responder(await service.RemoverAsync(id, ct));
}
