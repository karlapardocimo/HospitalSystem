using HospitalSystem.Domain.Entities;

namespace HospitalSystem.Application.Abstractions;

/// <summary>Gera e confere hash de senhas (implementado com PBKDF2 na Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verificar(string senha, string hash);
}

public sealed record TokenGerado(string Token, DateTime ExpiraEm);

/// <summary>Gera o JWT do usuário autenticado (implementado na Infrastructure).</summary>
public interface ITokenService
{
    TokenGerado Gerar(Usuario usuario);
}

/// <summary>Usuário da requisição atual (lido do JWT pela camada Api).</summary>
public interface IUsuarioAtual
{
    int? Id { get; }
    string Nome { get; }
    string? Perfil { get; }
}

/// <summary>
/// Abstração do cache. A camada Application decide O QUE e POR QUANTO TEMPO cachear;
/// a Infrastructure decide COMO (IMemoryCache).
/// </summary>
public interface ICacheService
{
    /// <summary>Cache-aside: devolve do cache (HIT) ou executa <paramref name="buscarNaFonte"/> e guarda (MISS).</summary>
    Task<T> ObterOuCriarAsync<T>(string chave, Func<CancellationToken, Task<T>> buscarNaFonte, TimeSpan expiracao, CancellationToken ct = default);

    /// <summary>Invalida uma chave específica.</summary>
    void Remover(string chave);

    /// <summary>Invalida todas as chaves que começam com o prefixo (ex.: "leitos:").</summary>
    void RemoverPorPrefixo(string prefixo);

    /// <summary>Invalida todo o cache.</summary>
    void Limpar();
}
