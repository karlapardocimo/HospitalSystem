namespace HospitalSystem.Domain.Common;

/// <summary>
/// Exceção lançada quando uma regra de negócio é violada.
/// Cada erro vem prefixado com o código da regra (ex.: "RN-PAC-02: CPF inválido."),
/// conforme docs/REGRAS_DE_NEGOCIO.md.
/// </summary>
public class BusinessRuleException : Exception
{
    public IReadOnlyList<string> Erros { get; }

    public BusinessRuleException(string codigo, string mensagem)
        : this(new[] { $"{codigo}: {mensagem}" })
    {
    }

    public BusinessRuleException(IEnumerable<string> erros)
        : base("A operação viola regras de negócio.")
    {
        Erros = erros.ToList();
    }
}
