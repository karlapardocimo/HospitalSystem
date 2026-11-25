using HospitalSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalSystem.Application;

public static class DependencyInjection
{
    /// <summary>Registra os casos de uso (serviços) da camada Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<IMedicoService, MedicoService>();
        services.AddScoped<IConvenioService, ConvenioService>();
        services.AddScoped<ISalaService, SalaService>();
        services.AddScoped<IMedicamentoService, MedicamentoService>();
        services.AddScoped<IConsultaService, ConsultaService>();
        services.AddScoped<IProntuarioService, ProntuarioService>();
        services.AddScoped<ILeitoService, LeitoService>();
        services.AddScoped<IInternacaoService, InternacaoService>();
        services.AddScoped<IInsumoService, InsumoService>();
        services.AddScoped<IFaturamentoService, FaturamentoService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
