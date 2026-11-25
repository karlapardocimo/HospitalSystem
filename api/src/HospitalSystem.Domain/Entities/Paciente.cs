using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.ValueObjects;

namespace HospitalSystem.Domain.Entities;

public class Paciente
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    /// <summary>Somente os 11 dígitos (RN-PAC-02).</summary>
    public string Cpf { get; private set; } = string.Empty;
    public DateOnly DataNascimento { get; private set; }
    public string? Telefone { get; private set; }
    public string? Endereco { get; private set; }
    public string? Alergias { get; private set; }
    public string? HistoricoClinico { get; private set; }
    public string? TipoSanguineo { get; private set; }

    public Paciente(int id, string nome, string cpf, DateOnly dataNascimento, string? telefone, string? endereco,
        string? alergias, string? historicoClinico, string? tipoSanguineo)
    {
        Id = id; Nome = nome; Cpf = cpf; DataNascimento = dataNascimento; Telefone = telefone; Endereco = endereco;
        Alergias = alergias; HistoricoClinico = historicoClinico; TipoSanguineo = tipoSanguineo;
    }

    public static Paciente Criar(string nome, string cpf, DateOnly dataNascimento, string? telefone, string? endereco,
        string? alergias, string? historicoClinico, string? tipoSanguineo, DateOnly hoje)
    {
        var p = new Paciente(0, string.Empty, string.Empty, default, null, null, null, null, null);
        p.Atualizar(nome, cpf, dataNascimento, telefone, endereco, alergias, historicoClinico, tipoSanguineo, hoje);
        return p;
    }

    public void Atualizar(string nome, string cpf, DateOnly dataNascimento, string? telefone, string? endereco,
        string? alergias, string? historicoClinico, string? tipoSanguineo, DateOnly hoje)
    {
        var tel = Texto.SomenteDigitos(telefone);
        var tipo = Texto.Limpar(tipoSanguineo)?.ToUpperInvariant();

        new Validacao()
            .Se(string.IsNullOrWhiteSpace(nome) || nome.Trim().Length < 3 || nome.Trim().Length > 100,
                "RN-PAC-01", "Nome é obrigatório e deve ter entre 3 e 100 caracteres.")
            .Se(!ValueObjects.Cpf.Valido(cpf), "RN-PAC-02", "CPF inválido.")
            .Se(dataNascimento == default || dataNascimento > hoje,
                "RN-PAC-04", "Data de nascimento é obrigatória e não pode ser futura.")
            .Se(dataNascimento != default && dataNascimento < hoje.AddYears(-130),
                "RN-PAC-04", "Data de nascimento inválida (idade acima de 130 anos).")
            .Se(tipo is not null && !TiposSanguineos.Todos.Contains(tipo),
                "RN-PAC-05", $"Tipo sanguíneo inválido. Use: {string.Join(", ", TiposSanguineos.Todos)}.")
            .Se(tel.Length > 0 && tel.Length is not (10 or 11),
                "RN-PAC-06", "Telefone deve ter 10 ou 11 dígitos (com DDD).")
            .LancarSeInvalido();

        Nome = nome.Trim();
        Cpf = Texto.SomenteDigitos(cpf);
        DataNascimento = dataNascimento;
        Telefone = tel.Length == 0 ? null : tel;
        Endereco = Texto.Limpar(endereco);
        Alergias = Texto.Limpar(alergias);
        HistoricoClinico = Texto.Limpar(historicoClinico);
        TipoSanguineo = tipo;
    }

    public void DefinirId(int id) => Id = id;

    public int Idade(DateOnly hoje)
    {
        var idade = hoje.Year - DataNascimento.Year;
        if (DataNascimento > hoje.AddYears(-idade))
        {
            idade--;
        }
        return idade;
    }

    /// <summary>RN-PRE-03: as alergias registradas mencionam o medicamento?</summary>
    public bool EAlergicoA(string nomeMedicamento) => Texto.Menciona(Alergias, nomeMedicamento);
}
