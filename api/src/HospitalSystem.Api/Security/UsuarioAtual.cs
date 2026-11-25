using HospitalSystem.Application.Abstractions;

namespace HospitalSystem.Api.Security;

/// <summary>Lê o usuário autenticado das claims do JWT validado.</summary>
public sealed class UsuarioAtual(IHttpContextAccessor accessor) : IUsuarioAtual
{
    private System.Security.Claims.ClaimsPrincipal? Usuario => accessor.HttpContext?.User;

    public int? Id => int.TryParse(Usuario?.FindFirst("sub")?.Value, out var id) ? id : null;
    public string Nome => Usuario?.FindFirst("name")?.Value ?? "Sistema";
    public string? Perfil => Usuario?.FindFirst("role")?.Value;
}
