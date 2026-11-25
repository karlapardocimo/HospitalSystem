using System.Text.Encodings.Web;
using System.Text.Json;
using HospitalSystem.Application.Common;
using HospitalSystem.Domain.Common;

namespace HospitalSystem.Api.Middleware;

/// <summary>
/// Tratamento global de erros: qualquer exceção vira um ResultWrapper, mantendo o
/// mesmo formato de resposta em todos os casos.
/// - BusinessRuleException (regra violada no domínio) → 422 com os códigos RN-XXX-NN;
/// - qualquer outra → 500 com mensagem genérica (o detalhe vai só para o log).
/// </summary>
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // mantém acentos legíveis
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessRuleException ex)
        {
            logger.LogInformation("Regra de negócio violada em {Rota}: {Erros}", context.Request.Path, string.Join(" | ", ex.Erros));
            await EscreverAsync(context, StatusCodes.Status422UnprocessableEntity,
                ResultWrapper<object?>.Fail(ErrorType.BusinessRule, ex.Message, ex.Erros));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // cliente desistiu da requisição: nada a responder
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado em {Metodo} {Rota}", context.Request.Method, context.Request.Path);
            await EscreverAsync(context, StatusCodes.Status500InternalServerError,
                ResultWrapper<object?>.Fail(ErrorType.Unexpected, "Ocorreu um erro inesperado ao processar a solicitação.",
                    new[] { "Erro interno. Consulte o log da API." }));
        }
    }

    public static Task EscreverAsync(HttpContext context, int status, ResultWrapper<object?> corpo)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(corpo, Json));
    }
}
