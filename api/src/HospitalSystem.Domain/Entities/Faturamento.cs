using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

public class Faturamento
{
    public const decimal LimiteAuditoria = 10_000m; // RN-FAT-04

    private static readonly Dictionary<string, string[]> Transicoes = new() // RN-FAT-05
    {
        [StatusPagamento.Pendente] = new[] { StatusPagamento.Pago, StatusPagamento.Auditoria, StatusPagamento.Cancelado },
        [StatusPagamento.Auditoria] = new[] { StatusPagamento.Pendente, StatusPagamento.Cancelado },
    };

    public int Id { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateOnly DataFaturamento { get; private set; }
    public string Status { get; private set; } = StatusPagamento.Pendente;
    public int IdPaciente { get; private set; }
    /// <summary>Data em que a guia foi paga (usada no indicador "Recebido no mês", RN-FAT-07).</summary>
    public DateOnly? DataPagamento { get; private set; }

    public Faturamento(int id, decimal valorTotal, DateOnly dataFaturamento, string status, int idPaciente, DateOnly? dataPagamento)
    {
        Id = id; ValorTotal = valorTotal; DataFaturamento = dataFaturamento; Status = status; IdPaciente = idPaciente;
        DataPagamento = dataPagamento;
    }

    /// <summary>RN-FAT-01 e RN-FAT-04: guia nasce Pendente, ou em Auditoria se o valor for alto.</summary>
    public static Faturamento Emitir(int idPaciente, decimal valor, DateOnly hoje)
    {
        Validacao.Garantir(valor > 0, "RN-FAT-01", "O valor total da guia deve ser maior que zero.");
        var status = valor >= LimiteAuditoria ? StatusPagamento.Auditoria : StatusPagamento.Pendente;
        return new Faturamento(0, decimal.Round(valor, 2), hoje, status, idPaciente, null);
    }

    public bool EmAuditoria => Status == StatusPagamento.Auditoria;

    public void DefinirId(int id) => Id = id;

    public void Pagar(DateOnly hoje)
    {
        Mudar(StatusPagamento.Pago);
        DataPagamento = hoje;
    }
    public void EnviarParaAuditoria() => Mudar(StatusPagamento.Auditoria);
    public void AprovarAuditoria() => Mudar(StatusPagamento.Pendente);

    /// <summary>RN-FAT-06</summary>
    public void Cancelar(string? motivo)
    {
        Validacao.Garantir(!string.IsNullOrWhiteSpace(motivo), "RN-FAT-06", "Informe o motivo do cancelamento.");
        Mudar(StatusPagamento.Cancelado);
    }

    private void Mudar(string novo)
    {
        var ok = Transicoes.TryGetValue(Status, out var destinos) && destinos.Contains(novo);
        Validacao.Garantir(ok, "RN-FAT-05", $"Não é possível mudar a guia de '{Status}' para '{novo}'.");
        Status = novo;
    }
}
