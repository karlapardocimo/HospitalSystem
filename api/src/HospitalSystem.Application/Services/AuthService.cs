using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalSystem.Application.Services;

public interface IAuthService
{
    Task<ResultWrapper<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ResultWrapper<UsuarioDto>> UsuarioLogadoAsync(int idUsuario, CancellationToken ct = default);
}

public class AuthService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    ITokenService tokens,
    TimeProvider relogio,
    ILogger<AuthService> logger) : IAuthService
{
    private const string MensagemGenerica = "E-mail ou senha inválidos.";

    public async Task<ResultWrapper<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
        {
            return ResultWrapper<LoginResponse>.Fail(ErrorType.Validation, "Informe e-mail e senha.",
                new[] { "E-mail e senha são obrigatórios." });
        }

        var agora = relogio.Agora();
        var usuario = await usuarios.ObterPorEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        // RN-USU-07: mesma resposta para e-mail inexistente e senha errada.
        if (usuario is null)
        {
            logger.LogWarning("Login recusado: e-mail não cadastrado.");
            return Falha(MensagemGenerica, "RN-USU-07");
        }

        if (usuario.EstaBloqueado(agora))
        {
            return Falha("Conta temporariamente bloqueada.", "RN-USU-08",
                $"Muitas tentativas inválidas. Tente novamente após {usuario.BloqueadoAte:HH:mm}.");
        }

        if (!hasher.Verificar(request.Senha, usuario.SenhaHash))
        {
            usuario.RegistrarFalhaDeLogin(agora);
            await usuarios.AtualizarAsync(usuario, ct);
            logger.LogWarning("Senha inválida para o usuário {Id}.", usuario.Id);

            return usuario.EstaBloqueado(agora)
                ? Falha("Conta temporariamente bloqueada.", "RN-USU-08",
                    $"Muitas tentativas inválidas. Tente novamente após {usuario.BloqueadoAte:HH:mm}.")
                : Falha(MensagemGenerica, "RN-USU-07");
        }

        if (!usuario.Ativo)
        {
            return Falha("Usuário inativo.", "RN-USU-09", "Usuário inativo. Procure o administrador.");
        }

        usuario.RegistrarLoginComSucesso();
        await usuarios.AtualizarAsync(usuario, ct);

        var token = tokens.Gerar(usuario);
        logger.LogInformation("Usuário {Id} ({Perfil}) autenticado; token expira em {Expira}.", usuario.Id, usuario.Perfil, token.ExpiraEm);

        return ResultWrapper<LoginResponse>.Ok(
            new LoginResponse(token.Token, "Bearer", token.ExpiraEm, usuario.ToDto(agora)),
            "Login realizado com sucesso.");
    }

    public async Task<ResultWrapper<UsuarioDto>> UsuarioLogadoAsync(int idUsuario, CancellationToken ct = default)
    {
        var u = await usuarios.ObterPorIdAsync(idUsuario, ct);
        return u is null
            ? ResultWrapper<UsuarioDto>.NotFound("Usuário")
            : ResultWrapper<UsuarioDto>.Ok(u.ToDto(relogio.Agora()));
    }

    private static ResultWrapper<LoginResponse> Falha(string mensagem, string codigo, string? detalhe = null) =>
        ResultWrapper<LoginResponse>.Fail(ErrorType.Unauthorized, mensagem, new[] { $"{codigo}: {detalhe ?? mensagem}" });
}
