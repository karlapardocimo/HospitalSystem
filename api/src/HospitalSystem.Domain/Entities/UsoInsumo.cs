using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

public class UsoInsumo
{
    public int Id { get; private set; }
    public int QuantidadeUtilizada { get; private set; }
    public DateTime DataUso { get; private set; }
    public int IdLeito { get; private set; }
    public int IdInsumo { get; private set; }
    /// <summary>Internação que consumiu o insumo (mantém o gasto mesmo após transferência de leito).</summary>
    public int? IdInternacao { get; private set; }

    public UsoInsumo(int id, int quantidadeUtilizada, DateTime dataUso, int idLeito, int idInsumo, int? idInternacao)
    {
        Id = id; QuantidadeUtilizada = quantidadeUtilizada; DataUso = dataUso; IdLeito = idLeito; IdInsumo = idInsumo;
        IdInternacao = idInternacao;
    }

    /// <summary>RN-EST-04: lança o uso em um leito ocupado (com internação ativa) e dá baixa no estoque.</summary>
    public static UsoInsumo Registrar(Insumo insumo, Leito leito, Internacao? internacaoAtiva, int quantidade, DateTime agora)
    {
        Validacao.Garantir(leito.Status == StatusLeito.Ocupado && internacaoAtiva is { Ativa: true } && internacaoAtiva.IdLeito == leito.Id,
            "RN-EST-04", $"O uso de insumo só pode ser lançado em leito ocupado (leito {leito.Numero} está {leito.Status}).");
        insumo.Saida(quantidade);
        return new UsoInsumo(0, quantidade, agora, leito.Id, insumo.Id, internacaoAtiva!.Id);
    }

    public void DefinirId(int id) => Id = id;
}
