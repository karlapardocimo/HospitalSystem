using System.Text.Json.Serialization;

namespace HospitalSystem.Application.Common;

/// <summary>Tipo do erro: usado pela API para escolher o status HTTP. Não vai no JSON.</summary>
public enum ErrorType
{
    None,
    Validation,     // 400 - dados de entrada mal formados
    Unauthorized,   // 401 - não autenticado / credenciais inválidas
    Forbidden,      // 403 - autenticado mas sem permissão
    NotFound,       // 404 - recurso inexistente
    Conflict,       // 409 - duplicidade (CPF, CRM, e-mail...)
    BusinessRule,   // 422 - violação de regra de negócio
    Unexpected      // 500 - erro inesperado
}

/// <summary>
/// ResultWrapper: formato único de TODAS as respostas da API.
/// <code>
/// { "success": true, "message": "...", "data": {...}, "errors": [] }
/// </code>
/// Os serviços da camada Application devolvem este objeto e os controllers apenas o
/// convertem em resposta HTTP, sem montar JSON manualmente.
/// </summary>
public class ResultWrapper<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    [JsonIgnore]
    public ErrorType ErrorType { get; init; } = ErrorType.None;

    public static ResultWrapper<T> Ok(T data, string message = "Operação realizada com sucesso.") =>
        new() { Success = true, Message = message, Data = data };

    public static ResultWrapper<T> Fail(ErrorType tipo, string message, IEnumerable<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            ErrorType = tipo,
            Errors = (errors ?? Array.Empty<string>()).ToList()
        };

    public static ResultWrapper<T> NotFound(string recurso) =>
        Fail(ErrorType.NotFound, $"{recurso} não encontrado(a).", new[] { $"{recurso} não encontrado(a)." });

    public static ResultWrapper<T> Conflict(string codigoRegra, string erro) =>
        Fail(ErrorType.Conflict, "Registro duplicado.", new[] { $"{codigoRegra}: {erro}" });

    public static ResultWrapper<T> RegraViolada(string codigoRegra, string erro) =>
        Fail(ErrorType.BusinessRule, "A operação viola regras de negócio.", new[] { $"{codigoRegra}: {erro}" });
}

/// <summary>Atalhos para respostas sem dados (ex.: exclusão).</summary>
public static class ResultWrapper
{
    public static ResultWrapper<object?> Ok(string message) => ResultWrapper<object?>.Ok(null, message);
}
