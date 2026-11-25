using HospitalSystem.Domain.Interfaces;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Persistence;

/// <summary>
/// Uma conexão por requisição HTTP (registrado como Scoped). Todos os repositórios da
/// mesma requisição compartilham esta sessão, o que permite que várias gravações
/// participem da mesma transação (padrão Unit of Work).
/// </summary>
public sealed class DbSession(string connectionString) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private MySqlConnection? _conexao;
    private MySqlTransaction? _transacao;

    private async Task<MySqlConnection> ConexaoAsync(CancellationToken ct)
    {
        if (_conexao is null)
        {
            _conexao = new MySqlConnection(connectionString);
            await _conexao.OpenAsync(ct);
        }
        return _conexao;
    }

    private async Task<MySqlCommand> ComandoAsync(string sql, object?[] parametros, CancellationToken ct)
    {
        var cmd = (await ConexaoAsync(ct)).CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = _transacao;
        foreach (var p in parametros.OfType<MySqlParameter>())
        {
            cmd.Parameters.Add(p);
        }
        return cmd;
    }

    /// <summary>Cria um parâmetro tratando null, DateOnly e TimeOnly.</summary>
    public static MySqlParameter P(string nome, object? valor) => new(nome, valor switch
    {
        null => DBNull.Value,
        DateOnly d => d.ToDateTime(TimeOnly.MinValue),
        TimeOnly t => t.ToTimeSpan(),
        bool b => b ? 1 : 0,
        _ => valor
    });

    public async Task<List<T>> ListarAsync<T>(string sql, Func<MySqlDataReader, T> mapear, CancellationToken ct, params object?[] parametros)
    {
        await using var cmd = await ComandoAsync(sql, parametros, ct);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var lista = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            lista.Add(mapear(reader));
        }
        return lista;
    }

    public async Task<T?> PrimeiroAsync<T>(string sql, Func<MySqlDataReader, T> mapear, CancellationToken ct, params object?[] parametros)
        where T : class
        => (await ListarAsync(sql, mapear, ct, parametros)).FirstOrDefault();

    public async Task<T> EscalarAsync<T>(string sql, CancellationToken ct, params object?[] parametros)
    {
        await using var cmd = await ComandoAsync(sql, parametros, ct);
        var valor = await cmd.ExecuteScalarAsync(ct);
        if (valor is null || valor is DBNull)
        {
            return default!;
        }
        var tipo = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(valor, tipo);
    }

    public async Task<int> ExecutarAsync(string sql, CancellationToken ct, params object?[] parametros)
    {
        await using var cmd = await ComandoAsync(sql, parametros, ct);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Executa um INSERT e devolve o id gerado (AUTO_INCREMENT).</summary>
    public async Task<int> InserirAsync(string sql, CancellationToken ct, params object?[] parametros)
    {
        await using var cmd = await ComandoAsync(sql, parametros, ct);
        await cmd.ExecuteNonQueryAsync(ct);
        return (int)cmd.LastInsertedId;
    }

    public async Task ExecutarEmTransacaoAsync(Func<Task> operacao, CancellationToken ct = default) =>
        await ExecutarEmTransacaoAsync(async () => { await operacao(); return true; }, ct);

    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken ct = default)
    {
        if (_transacao is not null)
        {
            return await operacao(); // já está dentro de uma transação
        }

        _transacao = await (await ConexaoAsync(ct)).BeginTransactionAsync(ct);
        try
        {
            var resultado = await operacao();
            await _transacao.CommitAsync(ct);
            return resultado;
        }
        catch
        {
            await _transacao.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await _transacao.DisposeAsync();
            _transacao = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexao is not null)
        {
            await _conexao.DisposeAsync();
        }
    }

    public void Dispose() => _conexao?.Dispose();
}
