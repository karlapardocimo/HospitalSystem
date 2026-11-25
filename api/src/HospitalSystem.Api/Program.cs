using System.Text;
using System.Text.Json.Serialization;
using HospitalSystem.Api.Middleware;
using HospitalSystem.Api.Security;
using HospitalSystem.Application;
using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Infrastructure;
using HospitalSystem.Infrastructure.Persistence;
using HospitalSystem.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });

// ---------------------------------------------------------------- Camadas (injeção de dependência)
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------------------------------------------------------------- Controllers + ResultWrapper para erros de entrada
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // O cliente PHP envia campos de formulário; aceita números enviados como texto.
        o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        // Sem ProblemDetails automático: erros como 415 passam pelo UseStatusCodePages e saem em ResultWrapper.
        o.SuppressMapClientErrors = true;

        // JSON mal formado / tipo errado → mesmo formato ResultWrapper, com status 400.
        o.InvalidModelStateResponseFactory = contexto =>
        {
            var entradas = contexto.ModelState.Where(e => e.Value?.Errors.Count > 0).ToList();
            var temErroDeCampo = entradas.Any(e => e.Key.StartsWith('$'));
            var erros = entradas
                // quando um campo do JSON é inválido, o ASP.NET também acusa "corpo obrigatório": omitimos esse ruído
                .Where(e => !temErroDeCampo || e.Key.StartsWith('$'))
                .Select(e => e.Key.TrimStart('$', '.') is { Length: > 0 } campo && e.Key.StartsWith('$')
                    ? $"{campo}: valor em formato inválido."
                    : "O corpo da requisição (JSON) é obrigatório ou está mal formado.")
                .Distinct();
            return new BadRequestObjectResult(
                ResultWrapper<object?>.Fail(ErrorType.Validation, "Dados de entrada inválidos.", erros));
        };
    });

// ---------------------------------------------------------------- Autenticação JWT
var jwt = builder.Configuration.GetSection(JwtSettings.Secao).Get<JwtSettings>() ?? new JwtSettings();
jwt.Validar();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // mantém os nomes das claims como foram gerados ("sub", "name", "role")
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        // 401 e 403 também no formato ResultWrapper
        o.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                var expirado = ctx.AuthenticateFailure is SecurityTokenExpiredException;
                var mensagem = expirado ? "Token expirado. Faça login novamente."
                    : string.IsNullOrEmpty(ctx.Request.Headers.Authorization) ? "Token não informado. Faça login para acessar este recurso."
                    : "Token inválido.";
                ctx.Response.Headers["WWW-Authenticate"] = "Bearer";
                await ExceptionMiddleware.EscreverAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized,
                    ResultWrapper<object?>.Fail(ErrorType.Unauthorized, mensagem, new[] { mensagem }));
            },
            OnForbidden = ctx => ExceptionMiddleware.EscreverAsync(ctx.HttpContext, StatusCodes.Status403Forbidden,
                ResultWrapper<object?>.Fail(ErrorType.Forbidden, "Acesso negado.",
                    new[] { $"O perfil '{ctx.HttpContext.User.FindFirst("role")?.Value}' não tem permissão para esta operação." }))
        };
    });

// Seguro por padrão: todo endpoint exige token, exceto os marcados com [AllowAnonymous].
builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---------------------------------------------------------------- Swagger (documentação e testes)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hospital System API",
        Version = "v1",
        Description = "API REST do Sistema de Gestão Hospitalar. Faça POST /api/auth/login, copie o token e clique em Authorize."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole somente o token JWT (sem a palavra Bearer)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    var xml = Path.Combine(AppContext.BaseDirectory, "HospitalSystem.Api.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

// ---------------------------------------------------------------- Banco de dados (criação/migração/seed)
if (app.Configuration.GetValue("Database:InicializarAutomaticamente", true))
{
    await InicializarBancoAsync(app);
}

// ---------------------------------------------------------------- Pipeline HTTP
app.UseMiddleware<ExceptionMiddleware>();

// Respostas de erro geradas pelo próprio ASP.NET sem corpo (rota inexistente, método não permitido,
// tipo de conteúdo não suportado...) também saem no formato ResultWrapper.
app.UseStatusCodePages(async contexto =>
{
    var http = contexto.HttpContext;
    var status = http.Response.StatusCode;
    var (tipo, mensagem) = status switch
    {
        StatusCodes.Status404NotFound => (ErrorType.NotFound, "Recurso não encontrado. Verifique a URL."),
        StatusCodes.Status405MethodNotAllowed => (ErrorType.Validation, "Método HTTP não permitido para esta rota."),
        StatusCodes.Status415UnsupportedMediaType => (ErrorType.Validation, "Tipo de conteúdo não suportado. Envie JSON (Content-Type: application/json)."),
        _ => (ErrorType.Unexpected, "Não foi possível processar a requisição.")
    };
    await ExceptionMiddleware.EscreverAsync(http, status,
        ResultWrapper<object?>.Fail(tipo, mensagem, new[] { mensagem }));
});

if (app.Configuration.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital System API v1");
        c.DocumentTitle = "Hospital System API";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();
}

app.UseMiddleware<CacheHeaderMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task InicializarBancoAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacao");
    for (var tentativa = 1; ; tentativa++)
    {
        try
        {
            await using var escopo = app.Services.CreateAsyncScope();
            await escopo.ServiceProvider.GetRequiredService<DatabaseInitializer>().InicializarAsync();
            return;
        }
        catch (MySqlConnector.MySqlException ex) when (tentativa < 15)
        {
            // No Docker o banco pode demorar alguns segundos para aceitar conexões.
            logger.LogWarning("Banco indisponível (tentativa {Tentativa}/15): {Erro}. Nova tentativa em 3s...", tentativa, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

/// <summary>Exposto para testes de integração.</summary>
public partial class Program;
