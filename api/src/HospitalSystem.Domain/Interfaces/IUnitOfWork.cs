namespace HospitalSystem.Domain.Interfaces;

/// <summary>
/// Garante que várias operações (ex.: gravar internação + ocupar leito) sejam
/// confirmadas juntas ou desfeitas juntas.
/// </summary>
public interface IUnitOfWork
{
    Task ExecutarEmTransacaoAsync(Func<Task> operacao, CancellationToken ct = default);
    Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken ct = default);
}
