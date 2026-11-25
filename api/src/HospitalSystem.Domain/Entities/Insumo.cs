using HospitalSystem.Domain.Common;

namespace HospitalSystem.Domain.Entities;

public class Insumo
{
    public const int LimiteEstoqueCritico = 20; // RN-EST-03

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int QuantidadeEstoque { get; private set; }
    public decimal ValorUnitario { get; private set; }

    public Insumo(int id, string nome, int quantidadeEstoque, decimal valorUnitario)
    {
        Id = id; Nome = nome; QuantidadeEstoque = quantidadeEstoque; ValorUnitario = valorUnitario;
    }

    public bool EstoqueCritico => QuantidadeEstoque < LimiteEstoqueCritico;

    public static Insumo Criar(string nome, int quantidadeInicial, decimal valorUnitario)
    {
        Validacao.Garantir(quantidadeInicial >= 0, "RN-EST-01", "A quantidade inicial não pode ser negativa.");
        var i = new Insumo(0, string.Empty, quantidadeInicial, 0);
        i.AtualizarDados(nome, valorUnitario);
        return i;
    }

    /// <summary>RN-EST-05: a quantidade não é alterada aqui, só por movimentação.</summary>
    public void AtualizarDados(string nome, decimal valorUnitario)
    {
        new Validacao()
            .Se(string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 100, "RN-EST-01", "Nome do insumo é obrigatório (até 100 caracteres).")
            .Se(valorUnitario <= 0, "RN-EST-01", "O valor unitário deve ser maior que zero.")
            .LancarSeInvalido();
        Nome = nome.Trim();
        ValorUnitario = decimal.Round(valorUnitario, 2);
    }

    public void DefinirId(int id) => Id = id;

    public void Entrada(int quantidade)
    {
        Validacao.Garantir(quantidade > 0, "RN-EST-02", "A quantidade da movimentação deve ser maior que zero.");
        QuantidadeEstoque += quantidade;
    }

    public void Saida(int quantidade)
    {
        Validacao.Garantir(quantidade > 0, "RN-EST-02", "A quantidade da movimentação deve ser maior que zero.");
        Validacao.Garantir(quantidade <= QuantidadeEstoque, "RN-EST-02",
            $"Estoque insuficiente de {Nome}: disponível {QuantidadeEstoque}, solicitado {quantidade}.");
        QuantidadeEstoque -= quantidade;
    }
}
