using HospitalSystem.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSystem.Api.Common;

/// <summary>
/// Base dos controllers: converte o ResultWrapper devolvido pela Application em resposta HTTP.
/// Os controllers não têm regra de negócio, só recebem a requisição, chamam o serviço e respondem.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Responder<T>(ResultWrapper<T> resultado, int statusSucesso = StatusCodes.Status200OK)
    {
        if (resultado.Success)
        {
            return StatusCode(statusSucesso, resultado);
        }

        var status = resultado.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
        return StatusCode(status, resultado);
    }

    /// <summary>201 Created para POST que cria recurso.</summary>
    protected IActionResult Criado<T>(ResultWrapper<T> resultado) => Responder(resultado, StatusCodes.Status201Created);
}
