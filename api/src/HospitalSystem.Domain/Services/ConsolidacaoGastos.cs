namespace HospitalSystem.Domain.Services;

/// <summary>RN-FAT-02: consolidação dos gastos de um paciente.</summary>
public sealed record ConsolidacaoGastos(decimal TotalInsumos, int QuantidadeExames, int QuantidadePrescricoes)
{
    public const decimal ValorReferenciaExame = 100m;
    public const decimal ValorReferenciaPrescricao = 50m;

    public decimal TotalExames => QuantidadeExames * ValorReferenciaExame;
    public decimal TotalPrescricoes => QuantidadePrescricoes * ValorReferenciaPrescricao;
    public decimal ValorTotal => TotalInsumos + TotalExames + TotalPrescricoes;
}
