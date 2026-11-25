namespace HospitalSystem.Application.DTOs;

public sealed record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;
}

public sealed record LoginResponse(string Token, string Tipo, DateTime ExpiraEm, UsuarioDto Usuario);

public sealed record UsuarioDto(int Id, string Nome, string Email, string Perfil, bool Ativo, DateTime CriadoEm, bool Bloqueado);

public sealed record CriarUsuarioRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;
    public string Perfil { get; init; } = string.Empty;
}

public sealed record AtualizarUsuarioRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Perfil { get; init; } = string.Empty;
    public bool Ativo { get; init; } = true;
}

public sealed record AlterarSenhaRequest
{
    public string NovaSenha { get; init; } = string.Empty;
}
