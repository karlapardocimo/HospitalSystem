using HospitalSystem.Infrastructure.Caching;

namespace HospitalSystem.Api.Middleware;

/// <summary>
/// Adiciona o cabeçalho "X-Cache: HIT|MISS" nas respostas que passaram pelo cache,
/// para mostrar (no navegador, Postman ou Swagger) de onde o dado veio.
/// </summary>
public sealed class CacheHeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICacheDiagnostico diagnostico)
    {
        context.Response.OnStarting(() =>
        {
            if (diagnostico.Status is { } status)
            {
                context.Response.Headers["X-Cache"] = status;
            }
            return Task.CompletedTask;
        });
        await next(context);
    }
}
