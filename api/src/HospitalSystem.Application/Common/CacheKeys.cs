namespace HospitalSystem.Application.Common;

/// <summary>
/// Estratégia de cache (detalhada no README):
/// - Catálogos que mudam pouco (médicos, convênios, salas, medicamentos): 10 minutos.
/// - Mapa de leitos: 1 minuto.
/// - Dashboard: 30 segundos.
/// Toda escrita invalida as chaves afetadas imediatamente, então o cliente nunca vê dado
/// desatualizado depois de uma alteração feita pela própria API.
/// </summary>
public static class CacheKeys
{
    public const string Medicos = "catalogo:medicos";
    public const string Convenios = "catalogo:convenios";
    public const string Salas = "catalogo:salas";
    public const string Medicamentos = "catalogo:medicamentos";
    public const string PrefixoLeitos = "leitos:";
    public const string Dashboard = "dashboard";

    public static string Leitos(string? status) => $"{PrefixoLeitos}{(string.IsNullOrWhiteSpace(status) ? "todos" : status)}";

    public static readonly TimeSpan TempoCatalogo = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan TempoLeitos = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan TempoDashboard = TimeSpan.FromSeconds(30);
}
