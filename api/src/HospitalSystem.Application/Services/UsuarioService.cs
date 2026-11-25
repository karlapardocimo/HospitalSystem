using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;

namespace HospitalSystem.Application.Services;

public interface IUsuarioService
{
    Task<ResultWrapper<IReadOnlyList<UsuarioDto>>> ListarAsync(CancellationToken ct = default);
    Task<ResultWrapper<UsuarioDto>> ObterAsync(int id, CancellationToken ct = default);
    Task<ResultWrapper<UsuarioDto>> CriarAsync(CriarUsuarioRequest request, CancellationToken ct = default);
    Task<ResultWrapper<UsuarioDto>> AtualizarAsync(int id, AtualizarUsuarioRequest request, CancellationToken ct = default);
    Task<ResultWrapper<object?>> AlterarSenhaAsync(int id, AlterarSenhaRequest request, CancellationToken ct = default);
    Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default);
}

public class UsuarioService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    IUsuarioAtual usuarioAtual,
    TimeProvider relogio) : IUsuarioService
{
    public async Task<ResultWrapper<IReadOnlyList<UsuarioDto>>> ListarAsync(CancellationToken ct = default)
    {
        var agora = relogio.Agora();
        var lista = (await usuarios.ListarAsync(ct)).Select(u => u.ToDto(agora)).ToList();
        return ResultWrapper<IReadOnlyList<UsuarioDto>>.Ok(lista, $"{lista.Count} usuário(s) encontrado(s).");
    }

    public async Task<ResultWrapper<UsuarioDto>> ObterAsync(int id, CancellationToken ct = default)
    {
        var u = await usuarios.ObterPorIdAsync(id, ct);
        return u is null ? ResultWrapper<UsuarioDto>.NotFound("Usuário") : ResultWrapper<UsuarioDto>.Ok(u.ToDto(relogio.Agora()));
    }

    public async Task<ResultWrapper<UsuarioDto>> CriarAsync(CriarUsuarioRequest request, CancellationToken ct = default)
    {
        Usuario.ValidarForcaSenha(request.Senha);                                   // RN-USU-03
        var usuario = Usuario.Criar(request.Nome, request.Email, hasher.Hash(request.Senha), request.Perfil, relogio.Agora()); // RN-USU-04

        if (await usuarios.EmailExisteAsync(usuario.Email, null, ct))
        {
            return ResultWrapper<UsuarioDto>.Conflict("RN-USU-02", "Já existe um usuário com este e-mail.");
        }

        usuario.DefinirId(await usuarios.AdicionarAsync(usuario, ct));
        return ResultWrapper<UsuarioDto>.Ok(usuario.ToDto(relogio.Agora()), "Usuário cadastrado com sucesso.");
    }

    public async Task<ResultWrapper<UsuarioDto>> AtualizarAsync(int id, AtualizarUsuarioRequest request, CancellationToken ct = default)
    {
        var u = await usuarios.ObterPorIdAsync(id, ct);
        if (u is null) return ResultWrapper<UsuarioDto>.NotFound("Usuário");

        if (id == usuarioAtual.Id && (!request.Ativo || request.Perfil != u.Perfil))
        {
            return ResultWrapper<UsuarioDto>.RegraViolada("RN-USU-11",
                "Você não pode desativar a própria conta nem alterar o próprio perfil.");
        }

        u.Atualizar(request.Nome, request.Perfil, request.Ativo);
        await usuarios.AtualizarAsync(u, ct);
        return ResultWrapper<UsuarioDto>.Ok(u.ToDto(relogio.Agora()), "Usuário atualizado com sucesso.");
    }

    public async Task<ResultWrapper<object?>> AlterarSenhaAsync(int id, AlterarSenhaRequest request, CancellationToken ct = default)
    {
        var u = await usuarios.ObterPorIdAsync(id, ct);
        if (u is null) return ResultWrapper<object?>.NotFound("Usuário");

        Usuario.ValidarForcaSenha(request.NovaSenha);
        u.AlterarSenha(hasher.Hash(request.NovaSenha));
        await usuarios.AtualizarAsync(u, ct);
        return ResultWrapper.Ok("Senha alterada com sucesso.");
    }

    public async Task<ResultWrapper<object?>> RemoverAsync(int id, CancellationToken ct = default)
    {
        if (id == usuarioAtual.Id)
        {
            return ResultWrapper<object?>.RegraViolada("RN-USU-11", "Você não pode remover a própria conta.");
        }
        if (await usuarios.ObterPorIdAsync(id, ct) is null) return ResultWrapper<object?>.NotFound("Usuário");

        await usuarios.RemoverAsync(id, ct);
        return ResultWrapper.Ok("Usuário removido com sucesso.");
    }
}
