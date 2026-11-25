using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

public class Medico
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Crm { get; private set; } = string.Empty;
    public string Especialidade { get; private set; } = string.Empty;
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }

    public Medico(int id, string nome, string crm, string especialidade, string? telefone, string? email)
    {
        Id = id; Nome = nome; Crm = crm; Especialidade = especialidade; Telefone = telefone; Email = email;
    }

    public static Medico Criar(string nome, string crm, string especialidade, string? telefone, string? email)
    {
        var m = new Medico(0, string.Empty, string.Empty, string.Empty, null, null);
        m.Atualizar(nome, crm, especialidade, telefone, email);
        return m;
    }

    public void Atualizar(string nome, string crm, string especialidade, string? telefone, string? email)
    {
        var crmDigitos = Texto.SomenteDigitos(crm);
        var mail = Texto.Limpar(email);

        new Validacao()
            .Se(string.IsNullOrWhiteSpace(nome) || nome.Trim().Length < 3 || nome.Trim().Length > 100,
                "RN-MED-01", "Nome é obrigatório e deve ter entre 3 e 100 caracteres.")
            .Se(crmDigitos.Length is < 4 or > 10, "RN-MED-02", "CRM deve ter de 4 a 10 dígitos.")
            .Se(!Especialidades.Todas.Contains(especialidade?.Trim() ?? string.Empty),
                "RN-MED-04", $"Especialidade inválida. Use: {string.Join(", ", Especialidades.Todas)}.")
            .Se(mail is not null && !Texto.EmailValido(mail), "RN-MED-05", "E-mail inválido.")
            .LancarSeInvalido();

        Nome = nome!.Trim();
        Crm = crmDigitos;
        Especialidade = especialidade!.Trim();
        Telefone = Texto.Limpar(telefone);
        Email = mail?.ToLowerInvariant();
    }

    public void DefinirId(int id) => Id = id;
}
