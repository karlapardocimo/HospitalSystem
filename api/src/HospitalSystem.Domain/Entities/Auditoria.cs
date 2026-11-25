namespace HospitalSystem.Domain.Entities;

public class Auditoria
{
    public int Id { get; private set; }
    public string Descricao { get; private set; }
    public DateTime DataAuditoria { get; private set; }
    public string Status { get; private set; }
    public int IdFaturamento { get; private set; }

    public Auditoria(int id, string descricao, DateTime dataAuditoria, string status, int idFaturamento)
    {
        Id = id; Descricao = descricao; DataAuditoria = dataAuditoria; Status = status; IdFaturamento = idFaturamento;
    }

    public static Auditoria Registrar(int idFaturamento, string descricao, string status, DateTime agora) =>
        new(0, descricao, agora, status, idFaturamento);
}
