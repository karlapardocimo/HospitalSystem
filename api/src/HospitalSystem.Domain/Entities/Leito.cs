using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

public class Leito
{
    /// <summary>RN-LEI-04: mudanças de status que podem ser feitas manualmente.</summary>
    private static readonly Dictionary<string, string[]> TransicoesManuais = new()
    {
        [StatusLeito.Disponivel] = new[] { StatusLeito.Manutencao, StatusLeito.Higienizacao },
        [StatusLeito.Manutencao] = new[] { StatusLeito.Disponivel },
        [StatusLeito.Higienizacao] = new[] { StatusLeito.Disponivel, StatusLeito.Manutencao },
    };

    public int Id { get; private set; }
    public string Numero { get; private set; } = string.Empty;
    public string Ala { get; private set; } = string.Empty;
    public string Status { get; private set; } = StatusLeito.Disponivel;
    public int? TempoHigienizacao { get; private set; }

    public Leito(int id, string numero, string ala, string status, int? tempoHigienizacao)
    {
        Id = id; Numero = numero; Ala = ala; Status = status; TempoHigienizacao = tempoHigienizacao;
    }

    /// <summary>RN-LEI-01, RN-LEI-02</summary>
    public static Leito Criar(string numero, string ala)
    {
        var l = new Leito(0, string.Empty, string.Empty, StatusLeito.Disponivel, null);
        l.AtualizarDados(numero, ala);
        return l;
    }

    public void AtualizarDados(string numero, string ala)
    {
        new Validacao()
            .Se(string.IsNullOrWhiteSpace(numero) || numero.Trim().Length > 10, "RN-LEI-01",
                "Número do leito é obrigatório (até 10 caracteres).")
            .Se(string.IsNullOrWhiteSpace(ala) || ala.Trim().Length > 50, "RN-LEI-01",
                "Ala é obrigatória (até 50 caracteres).")
            .LancarSeInvalido();
        Numero = numero.Trim();
        Ala = ala.Trim();
    }

    public void DefinirId(int id) => Id = id;

    /// <summary>RN-LEI-03 e RN-LEI-04: alteração manual feita pela equipe.</summary>
    public void AlterarStatusManualmente(string novoStatus)
    {
        Validacao.Garantir(StatusLeito.Todos.Contains(novoStatus), "RN-LEI-02",
            $"Status inválido. Use: {string.Join(", ", StatusLeito.Todos)}.");
        Validacao.Garantir(novoStatus != StatusLeito.Ocupado && Status != StatusLeito.Ocupado, "RN-LEI-03",
            "Um leito só é ocupado por internação e só é liberado por alta ou transferência.");
        var permitido = TransicoesManuais.TryGetValue(Status, out var destinos) && destinos.Contains(novoStatus);
        Validacao.Garantir(permitido, "RN-LEI-04", $"Não é possível mudar o leito de '{Status}' para '{novoStatus}'.");
        Status = novoStatus;
    }

    /// <summary>Usado somente pela internação (RN-INT-02 / RN-INT-03).</summary>
    public void Ocupar()
    {
        Validacao.Garantir(Status == StatusLeito.Disponivel, "RN-INT-02",
            $"O leito {Numero} ({Ala}) não está disponível (status: {Status}).");
        Status = StatusLeito.Ocupado;
    }

    /// <summary>Usado somente pela alta/transferência (RN-INT-04 / RN-INT-05).</summary>
    public void Desocupar()
    {
        Status = StatusLeito.Higienizacao;
    }
}
