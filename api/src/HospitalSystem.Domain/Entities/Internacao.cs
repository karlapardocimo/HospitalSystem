using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

public class Internacao
{
    public int Id { get; private set; }
    public DateTime DataEntrada { get; private set; }
    public DateTime? DataAlta { get; private set; }
    public string Status { get; private set; } = StatusInternacao.Ativa;
    public int IdPaciente { get; private set; }
    public int IdLeito { get; private set; }

    public Internacao(int id, DateTime dataEntrada, DateTime? dataAlta, string status, int idPaciente, int idLeito)
    {
        Id = id; DataEntrada = dataEntrada; DataAlta = dataAlta; Status = status; IdPaciente = idPaciente; IdLeito = idLeito;
    }

    public bool Ativa => Status == StatusInternacao.Ativa;

    /// <summary>RN-INT-02 / RN-INT-03 (a verificação de RN-INT-01 depende do banco e fica no serviço).</summary>
    public static Internacao Internar(Paciente paciente, Leito leito, DateTime agora)
    {
        leito.Ocupar();
        return new Internacao(0, agora, null, StatusInternacao.Ativa, paciente.Id, leito.Id);
    }

    public void DefinirId(int id) => Id = id;

    /// <summary>RN-INT-04</summary>
    public void DarAlta(Leito leito, DateTime agora)
    {
        Validacao.Garantir(Ativa, "RN-INT-04", $"Só é possível dar alta em internação ativa (status atual: {Status}).");
        Status = StatusInternacao.Alta;
        DataAlta = agora;
        leito.Desocupar();
    }

    /// <summary>RN-INT-05</summary>
    public void Transferir(Leito leitoAtual, Leito novoLeito)
    {
        new Validacao()
            .Se(!Ativa, "RN-INT-05", "Só é possível transferir internação ativa.")
            .Se(novoLeito.Id == leitoAtual.Id, "RN-INT-05", "O novo leito deve ser diferente do atual.")
            .LancarSeInvalido();
        novoLeito.Ocupar();
        leitoAtual.Desocupar();
        IdLeito = novoLeito.Id;
    }
}
