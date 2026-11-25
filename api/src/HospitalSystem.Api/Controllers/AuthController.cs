using HospitalSystem.Api.Common;
using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Api.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService auth, IUsuarioAtual usuarioAtual) : ApiControllerBase
{
    /// <summary>Autentica o usuário e devolve o token JWT (endpoint público).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        Responder(await auth.LoginAsync(request, ct));

    /// <summary>Dados do usuário dono do token (útil para testar o JWT).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Responder(await auth.UsuarioLogadoAsync(usuarioAtual.Id ?? 0, ct));
}

[Route("api/status")]
public class StatusController(TimeProvider relogio) : ApiControllerBase
{
    /// <summary>Verifica se a API está no ar (endpoint público, não exige token).</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Responder(Application.Common.ResultWrapper<object>.Ok(new
    {
        servico = "HospitalSystem.Api",
        versao = "2.0.0",
        horarioServidor = relogio.GetLocalNow().DateTime
    }, "API em funcionamento."));
}
