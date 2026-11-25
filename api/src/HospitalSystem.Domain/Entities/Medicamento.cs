using HospitalSystem.Domain.Common;

namespace HospitalSystem.Domain.Entities;

public class Medicamento
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Contraindicacoes { get; private set; }
    /// <summary>Texto livre com os nomes dos medicamentos com que este interage.</summary>
    public string? InteracoesMedicamentosas { get; private set; }

    public Medicamento(int id, string nome, string? contraindicacoes, string? interacoes)
    {
        Id = id; Nome = nome; Contraindicacoes = contraindicacoes; InteracoesMedicamentosas = interacoes;
    }

    public static Medicamento Criar(string nome, string? contraindicacoes, string? interacoes)
    {
        var m = new Medicamento(0, string.Empty, null, null);
        m.Atualizar(nome, contraindicacoes, interacoes);
        return m;
    }

    public void Atualizar(string nome, string? contraindicacoes, string? interacoes)
    {
        Validacao.Garantir(!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length <= 100, "RN-PRE-02",
            "O nome do medicamento é obrigatório (até 100 caracteres).");
        Nome = nome.Trim();
        Contraindicacoes = Texto.Limpar(contraindicacoes);
        InteracoesMedicamentosas = Texto.Limpar(interacoes);
    }

    public void DefinirId(int id) => Id = id;

    /// <summary>A interação vale nos dois sentidos: basta um dos cadastros citar o outro.</summary>
    public bool InterageCom(Medicamento outro) =>
        outro.Id != Id &&
        (Texto.Menciona(InteracoesMedicamentosas, outro.Nome) || Texto.Menciona(outro.InteracoesMedicamentosas, Nome));
}
