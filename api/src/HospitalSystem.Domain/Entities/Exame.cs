using HospitalSystem.Domain.Common;

namespace HospitalSystem.Domain.Entities;

public class Exame
{
    public int Id { get; private set; }
    public int IdProntuario { get; private set; }
    public string Nome { get; private set; }
    public string? Resultado { get; private set; }
    public DateOnly DataExame { get; private set; }

    public Exame(int id, int idProntuario, string nome, string? resultado, DateOnly dataExame)
    {
        Id = id; IdProntuario = idProntuario; Nome = nome; Resultado = resultado; DataExame = dataExame;
    }

    public static Exame Solicitar(int idProntuario, string nome, DateOnly? data, string? resultado, DateOnly hoje)
    {
        Validacao.Garantir(!string.IsNullOrWhiteSpace(nome) && nome.Trim().Length <= 100, "RN-EXA-01",
            "O nome do exame é obrigatório (até 100 caracteres).");
        return new Exame(0, idProntuario, nome.Trim(), Texto.Limpar(resultado), data ?? hoje);
    }

    /// <summary>RN-EXA-02: o resultado é registrado uma única vez.</summary>
    public void RegistrarResultado(string resultado)
    {
        new Validacao()
            .Se(!string.IsNullOrWhiteSpace(Resultado), "RN-EXA-02", "Este exame já possui resultado registrado.")
            .Se(string.IsNullOrWhiteSpace(resultado), "RN-EXA-02", "Informe o resultado do exame.")
            .LancarSeInvalido();
        Resultado = resultado.Trim();
    }

    public void DefinirId(int id) => Id = id;
}
