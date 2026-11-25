using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Api.Security;

/// <summary>Combinações de perfis usadas nos [Authorize(Roles = ...)] (matriz de permissões em docs/REGRAS_DE_NEGOCIO.md).</summary>
public static class Papeis
{
    public const string Admin = Perfis.Administrador;
    public const string Medico = Perfis.Medico;
    public const string CadastroPaciente = Perfis.Administrador + "," + Perfis.Recepcao + "," + Perfis.Enfermagem;
    public const string Recepcao = Perfis.Administrador + "," + Perfis.Recepcao;
    public const string Assistencial = Perfis.Administrador + "," + Perfis.Medico + "," + Perfis.Enfermagem;
    public const string Clinico = Perfis.Administrador + "," + Perfis.Medico;
    public const string Estoque = Perfis.Administrador + "," + Perfis.Enfermagem;
    public const string Financeiro = Perfis.Administrador + "," + Perfis.Recepcao;
}
