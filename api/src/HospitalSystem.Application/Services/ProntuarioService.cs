using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.Common;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;
using HospitalSystem.Domain.Services;

namespace HospitalSystem.Application.Services;

public interface IProntuarioService
{
    Task<ResultWrapper<PrescricaoCriadaDto>> PrescreverAsync(int idProntuario, PrescricaoRequest r, CancellationToken ct = default);
    Task<ResultWrapper<ExameDto>> SolicitarExameAsync(int idProntuario, ExameRequest r, CancellationToken ct = default);
    Task<ResultWrapper<ExameDto>> RegistrarResultadoExameAsync(int idExame, ResultadoExameRequest r, CancellationToken ct = default);
}

public class ProntuarioService(
    IProntuarioRepository prontuarios,
    IProntuarioQueries queries,
    IPacienteRepository pacientes,
    IMedicamentoRepository medicamentos,
    IUsuarioAtual usuarioAtual,
    TimeProvider relogio) : IProntuarioService
{
    private static readonly TimeSpan JanelaInteracao = TimeSpan.FromDays(30); // RN-PRE-04

    public async Task<ResultWrapper<PrescricaoCriadaDto>> PrescreverAsync(int idProntuario, PrescricaoRequest r, CancellationToken ct = default)
    {
        // RN-PRE-01 (também garantido pelo [Authorize(Roles = "Medico")] no controller)
        if (usuarioAtual.Perfil != Perfis.Medico)
            return ResultWrapper<PrescricaoCriadaDto>.Fail(ErrorType.Forbidden, "Acesso negado.",
                new[] { "RN-PRE-01: Somente médicos podem prescrever." });

        if (await prontuarios.ObterRegistroAsync(idProntuario, ct) is null) return ResultWrapper<PrescricaoCriadaDto>.NotFound("Registro de prontuário");
        var idPaciente = await queries.IdPacienteDoProntuarioAsync(idProntuario, ct);
        var paciente = idPaciente is int id ? await pacientes.ObterPorIdAsync(id, ct) : null;
        if (paciente is null) return ResultWrapper<PrescricaoCriadaDto>.NotFound("Paciente");

        var medicamento = await medicamentos.ObterPorIdAsync(r.IdMedicamento, ct);
        if (medicamento is null) return ResultWrapper<PrescricaoCriadaDto>.RegraViolada("RN-PRE-02", "Medicamento não encontrado.");

        var agora = relogio.Agora();
        var recentes = await prontuarios.MedicamentosPrescritosDesdeAsync(paciente.Id, agora - JanelaInteracao, ct);

        // RN-PRE-03 (alergia: lança exceção) e RN-PRE-04/05 (alertas)
        var analise = AnalisadorPrescricao.Analisar(paciente, medicamento, recentes);

        if (analise.PossuiInteracoes && !r.ConfirmarInteracao)
        {
            return ResultWrapper<PrescricaoCriadaDto>.Fail(ErrorType.BusinessRule,
                "Interação medicamentosa detectada. Reenvie com confirmarInteracao = true para prescrever mesmo assim.",
                analise.Interacoes.Select(i => $"RN-PRE-04: {i}"));
        }

        var alertaConfirmado = analise.PossuiInteracoes ? string.Join(" | ", analise.Interacoes) : null;
        var prescricao = Prescricao.Criar(idProntuario, medicamento, r.Dosagem, r.Frequencia, r.DuracaoTratamento, alertaConfirmado, agora);
        prescricao.DefinirId(await prontuarios.AdicionarPrescricaoAsync(prescricao, ct));

        var alertas = new List<string>(analise.Interacoes.Select(i => $"Interação confirmada pelo médico: {i}"));
        if (!string.IsNullOrWhiteSpace(analise.Contraindicacoes))
            alertas.Add($"RN-PRE-05: Contraindicações de {medicamento.Nome}: {analise.Contraindicacoes}");

        var dto = (await queries.ObterPrescricaoAsync(prescricao.Id, ct))!;
        return ResultWrapper<PrescricaoCriadaDto>.Ok(new PrescricaoCriadaDto(dto, alertas), "Prescrição emitida com sucesso.");
    }

    public async Task<ResultWrapper<ExameDto>> SolicitarExameAsync(int idProntuario, ExameRequest r, CancellationToken ct = default)
    {
        if (await prontuarios.ObterRegistroAsync(idProntuario, ct) is null) return ResultWrapper<ExameDto>.NotFound("Registro de prontuário");

        var exame = Exame.Solicitar(idProntuario, r.NomeExame, r.DataExame, r.Resultado, relogio.Hoje());
        exame.DefinirId(await prontuarios.AdicionarExameAsync(exame, ct));
        return ResultWrapper<ExameDto>.Ok(exame.ToDto(), "Exame registrado no prontuário.");
    }

    public async Task<ResultWrapper<ExameDto>> RegistrarResultadoExameAsync(int idExame, ResultadoExameRequest r, CancellationToken ct = default)
    {
        var exame = await prontuarios.ObterExameAsync(idExame, ct);
        if (exame is null) return ResultWrapper<ExameDto>.NotFound("Exame");

        exame.RegistrarResultado(r.Resultado);
        await prontuarios.AtualizarExameAsync(exame, ct);
        return ResultWrapper<ExameDto>.Ok(exame.ToDto(), "Resultado do exame registrado.");
    }
}
