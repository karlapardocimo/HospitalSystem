using System.Collections.Concurrent;
using HospitalSystem.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HospitalSystem.Infrastructure.Caching;

/// <summary>Status do cache na requisição atual (vira o cabeçalho HTTP X-Cache).</summary>
public interface ICacheDiagnostico
{
    string? Status { get; }
}

/// <summary>Guarda as chaves existentes para permitir invalidar por prefixo (IMemoryCache não lista chaves).</summary>
public sealed class RegistroDeChaves
{
    public ConcurrentDictionary<string, byte> Chaves { get; } = new();
}

/// <summary>
/// Cache em memória do processo (IMemoryCache) com a estratégia cache-aside:
///   1. procura a chave no cache → HIT: devolve sem ir ao banco;
///   2. MISS: busca na fonte original (banco), guarda com tempo de expiração absoluto e devolve;
///   3. escritas chamam Remover/RemoverPorPrefixo → próxima leitura será MISS (dado novo).
/// Cada etapa gera um log, para que seja possível ver no console quando o dado veio do banco
/// e quando veio do cache.
/// </summary>
public sealed class MemoryCacheService(IMemoryCache cache, RegistroDeChaves registro, ILogger<MemoryCacheService> logger)
    : ICacheService, ICacheDiagnostico
{
    private sealed record Entrada<T>(T Valor, DateTime ExpiraEm);

    public string? Status { get; private set; }

    public async Task<T> ObterOuCriarAsync<T>(string chave, Func<CancellationToken, Task<T>> buscarNaFonte, TimeSpan expiracao, CancellationToken ct = default)
    {
        if (cache.TryGetValue(chave, out Entrada<T>? entrada) && entrada is not null)
        {
            var restante = (int)(entrada.ExpiraEm - DateTime.Now).TotalSeconds;
            logger.LogInformation("[CACHE HIT]  {Chave} → dado servido da memória (expira em {Segundos}s)", chave, restante);
            Status = "HIT";
            return entrada.Valor;
        }

        logger.LogInformation("[CACHE MISS] {Chave} → buscando na fonte original (banco de dados)", chave);
        var valor = await buscarNaFonte(ct);

        var opcoes = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiracao };
        opcoes.RegisterPostEvictionCallback((k, _, motivo, _) =>
        {
            registro.Chaves.TryRemove(k.ToString()!, out _);
            if (motivo == EvictionReason.Expired)
            {
                logger.LogInformation("[CACHE EXPIROU] {Chave}", k);
            }
        });

        cache.Set(chave, new Entrada<T>(valor, DateTime.Now.Add(expiracao)), opcoes);
        registro.Chaves[chave] = 0;
        logger.LogInformation("[CACHE SET]  {Chave} armazenado por {Segundos}s", chave, (int)expiracao.TotalSeconds);
        Status = "MISS";
        return valor;
    }

    public void Remover(string chave)
    {
        cache.Remove(chave);
        if (registro.Chaves.TryRemove(chave, out _))
        {
            logger.LogInformation("[CACHE INVALIDADO] {Chave} → a próxima leitura buscará dados atualizados no banco", chave);
        }
    }

    public void RemoverPorPrefixo(string prefixo)
    {
        foreach (var chave in registro.Chaves.Keys.Where(k => k.StartsWith(prefixo, StringComparison.Ordinal)))
        {
            Remover(chave);
        }
    }

    public void Limpar()
    {
        foreach (var chave in registro.Chaves.Keys)
        {
            Remover(chave);
        }
        logger.LogInformation("[CACHE LIMPO] todas as chaves foram removidas");
    }
}
