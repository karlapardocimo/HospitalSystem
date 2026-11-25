using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

public class Sala
{
    public int Id { get; private set; }
    public string Numero { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public string Status { get; private set; } = StatusSala.Disponivel;

    public Sala(int id, string numero, string tipo, string status)
    {
        Id = id; Numero = numero; Tipo = tipo; Status = status;
    }

    /// <summary>RN-SAL-03: toda sala nova começa Disponível.</summary>
    public static Sala Criar(string numero, string tipo)
    {
        var s = new Sala(0, string.Empty, string.Empty, StatusSala.Disponivel);
        s.Atualizar(numero, tipo, StatusSala.Disponivel);
        return s;
    }

    public void Atualizar(string numero, string tipo, string status)
    {
        new Validacao()
            .Se(string.IsNullOrWhiteSpace(numero) || numero.Trim().Length > 10,
                "RN-SAL-01", "Número da sala é obrigatório (até 10 caracteres).")
            .Se(!TiposSala.Todos.Contains(tipo?.Trim() ?? string.Empty),
                "RN-SAL-02", $"Tipo de sala inválido. Use: {string.Join(", ", TiposSala.Todos)}.")
            .Se(!StatusSala.Todos.Contains(status?.Trim() ?? string.Empty),
                "RN-SAL-03", $"Status de sala inválido. Use: {string.Join(", ", StatusSala.Todos)}.")
            .LancarSeInvalido();

        Numero = numero!.Trim();
        Tipo = tipo!.Trim();
        Status = status!.Trim();
    }

    public void DefinirId(int id) => Id = id;

    /// <summary>RN-SAL-05</summary>
    public bool PodeReceberConsultas => Status != StatusSala.Manutencao;
}
