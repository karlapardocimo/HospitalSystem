namespace HospitalSystem.Domain.Common;

/// <summary>
/// Acumula violações de regras para devolver todas de uma vez ao cliente,
/// em vez de parar no primeiro erro.
/// </summary>
public sealed class Validacao
{
    private readonly List<string> _erros = new();

    public bool Valido => _erros.Count == 0;

    /// <summary>Registra a violação <paramref name="codigo"/> quando <paramref name="invalido"/> for verdadeiro.</summary>
    public Validacao Se(bool invalido, string codigo, string mensagem)
    {
        if (invalido)
        {
            _erros.Add($"{codigo}: {mensagem}");
        }
        return this;
    }

    public void LancarSeInvalido()
    {
        if (!Valido)
        {
            throw new BusinessRuleException(_erros);
        }
    }

    /// <summary>Atalho para regras simples: lança a exceção imediatamente se a condição for verdadeira.</summary>
    public static void Garantir(bool condicaoValida, string codigo, string mensagem)
    {
        if (!condicaoValida)
        {
            throw new BusinessRuleException(codigo, mensagem);
        }
    }
}
