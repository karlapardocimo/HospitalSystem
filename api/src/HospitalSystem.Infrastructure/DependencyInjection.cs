using HospitalSystem.Application.Abstractions;
using HospitalSystem.Domain.Interfaces;
using HospitalSystem.Infrastructure.Caching;
using HospitalSystem.Infrastructure.Persistence;
using HospitalSystem.Infrastructure.Persistence.Queries;
using HospitalSystem.Infrastructure.Persistence.Repositories;
using HospitalSystem.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalSystem.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra as implementações concretas das abstrações do Domain e da Application.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("HospitalDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'HospitalDb' não configurada (ConnectionStrings__HospitalDb ou user-secrets).");
        }

        // Banco: uma sessão (conexão + transação) por requisição
        services.AddScoped(_ => new DbSession(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DbSession>());

        // Repositórios (escrita / entidades)
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IMedicoRepository, MedicoRepository>();
        services.AddScoped<IConvenioRepository, ConvenioRepository>();
        services.AddScoped<ISalaRepository, SalaRepository>();
        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<IProntuarioRepository, ProntuarioRepository>();
        services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();
        services.AddScoped<ILeitoRepository, LeitoRepository>();
        services.AddScoped<IInternacaoRepository, InternacaoRepository>();
        services.AddScoped<IInsumoRepository, InsumoRepository>();
        services.AddScoped<IFaturamentoRepository, FaturamentoRepository>();

        // Consultas de leitura (listagens com JOIN)
        services.AddScoped<IConsultaQueries, ConsultaQueries>();
        services.AddScoped<IProntuarioQueries, ProntuarioQueries>();
        services.AddScoped<ILeitoQueries, LeitoQueries>();
        services.AddScoped<IInternacaoQueries, InternacaoQueries>();
        services.AddScoped<IFaturamentoQueries, FaturamentoQueries>();
        services.AddScoped<IDashboardQueries, DashboardQueries>();

        // Segurança
        services.AddOptions<JwtSettings>().Bind(config.GetSection(JwtSettings.Secao))
            .Validate(s => { s.Validar(); return true; }).ValidateOnStart();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Cache em memória
        services.AddMemoryCache();
        services.AddSingleton<RegistroDeChaves>();
        services.AddScoped<MemoryCacheService>();
        services.AddScoped<ICacheService>(sp => sp.GetRequiredService<MemoryCacheService>());
        services.AddScoped<ICacheDiagnostico>(sp => sp.GetRequiredService<MemoryCacheService>());

        services.AddScoped<DatabaseInitializer>();
        return services;
    }
}
