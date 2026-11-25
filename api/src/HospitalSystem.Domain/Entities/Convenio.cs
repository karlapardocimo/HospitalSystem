using HospitalSystem.Domain.Common;

namespace HospitalSystem.Domain.Entities;

public class Convenio
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public DateOnly? ValidadeCarteira { get; private set; }
    public string? Telefone { get; private set; }

    public Convenio(int id, string nome, DateOnly? validadeCarteira, string? telefone)
    {
        Id = id; Nome = nome; ValidadeCarteira = validadeCarteira; Telefone = telefone;
    }

    public static Convenio Criar(string nome, DateOnly? validade, string? telefone)
    {
        var c = new Convenio(0, string.Empty, null, null);
        c.Atualizar(nome, validade, telefone);
        return c;
    }

    public void Atualizar(string nome, DateOnly? validade, string? telefone)
    {
        Validacao.Garantir(!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length <= 100,
            "RN-CON-01", "Nome do convênio é obrigatório (até 100 caracteres).");
        Nome = nome.Trim();
        ValidadeCarteira = validade;
        Telefone = Texto.Limpar(telefone);
    }

    public void DefinirId(int id) => Id = id;

    /// <summary>RN-CON-03: o convênio cobre uma consulta nesta data?</summary>
    public bool ValidoEm(DateOnly data) => ValidadeCarteira is null || data <= ValidadeCarteira.Value;
}
