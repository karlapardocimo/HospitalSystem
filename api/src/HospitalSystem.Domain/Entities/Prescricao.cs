using HospitalSystem.Domain.Common;

namespace HospitalSystem.Domain.Entities;

public class Prescricao
{
    public int Id { get; private set; }
    public int IdProntuario { get; private set; }
    public int IdMedicamento { get; private set; }
    public string Dosagem { get; private set; }
    public string Frequencia { get; private set; }
    public string DuracaoTratamento { get; private set; }
    public DateTime DataPrescricao { get; private set; }
    /// <summary>Alerta de interação confirmado conscientemente pelo médico (RN-PRE-04).</summary>
    public string? AlertaConfirmado { get; private set; }

    public Prescricao(int id, int idProntuario, int idMedicamento, string dosagem, string frequencia, string duracao,
        DateTime dataPrescricao, string? alertaConfirmado)
    {
        Id = id; IdProntuario = idProntuario; IdMedicamento = idMedicamento; Dosagem = dosagem; Frequencia = frequencia;
        DuracaoTratamento = duracao; DataPrescricao = dataPrescricao; AlertaConfirmado = alertaConfirmado;
    }

    public static Prescricao Criar(int idProntuario, Medicamento medicamento, string dosagem, string frequencia,
        string duracao, string? alertaConfirmado, DateTime agora)
    {
        new Validacao()
            .Se(string.IsNullOrWhiteSpace(dosagem), "RN-PRE-02", "Informe a dosagem.")
            .Se(string.IsNullOrWhiteSpace(frequencia), "RN-PRE-02", "Informe a frequência.")
            .Se(string.IsNullOrWhiteSpace(duracao), "RN-PRE-02", "Informe a duração do tratamento.")
            .LancarSeInvalido();

        return new Prescricao(0, idProntuario, medicamento.Id, dosagem.Trim(), frequencia.Trim(), duracao.Trim(), agora, alertaConfirmado);
    }

    public void DefinirId(int id) => Id = id;
}
