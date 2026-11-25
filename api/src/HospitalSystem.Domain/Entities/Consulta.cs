using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.Services;
using HospitalSystem.Domain.ValueObjects;

namespace HospitalSystem.Domain.Entities;

public class Consulta
{
    /// <summary>RN-CNS-03: tolerância para horário no passado.</summary>
    public static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(10);

    /// <summary>RN-CNS-07: transições de status permitidas.</summary>
    private static readonly Dictionary<string, string[]> Transicoes = new()
    {
        [StatusConsulta.Agendada] = new[] { StatusConsulta.AguardandoTriagem, StatusConsulta.Cancelada, StatusConsulta.NaoCompareceu },
        [StatusConsulta.AguardandoTriagem] = new[] { StatusConsulta.EmAtendimento, StatusConsulta.Cancelada },
        [StatusConsulta.EmAtendimento] = new[] { StatusConsulta.Liberado },
    };

    public int Id { get; private set; }
    public DateOnly Data { get; private set; }
    public TimeOnly? Horario { get; private set; }
    public string Status { get; private set; } = StatusConsulta.Agendada;
    public int IdPaciente { get; private set; }
    public int? IdMedico { get; private set; }
    public int? IdSala { get; private set; }
    public int? IdConvenio { get; private set; }

    public Consulta(int id, DateOnly data, TimeOnly? horario, string status, int idPaciente, int? idMedico, int? idSala, int? idConvenio)
    {
        Id = id; Data = data; Horario = horario; Status = status; IdPaciente = idPaciente;
        IdMedico = idMedico; IdSala = idSala; IdConvenio = idConvenio;
    }

    public bool Ativa => StatusConsulta.Ativos.Contains(Status);

    /// <summary>Agendamento feito pela recepção (RN-CNS-01..03, RN-SAL-05, RN-CON-03).</summary>
    public static Consulta Agendar(Paciente paciente, Medico? medico, Sala? sala, Convenio? convenio,
        DateOnly data, TimeOnly horario, bool enviarParaTriagem, DateTime agora)
    {
        var quando = data.ToDateTime(horario);
        new Validacao()
            .Se(medico is null, "RN-CNS-02", "O agendamento exige um médico.")
            .Se(quando < agora - Tolerancia, "RN-CNS-03", "A data e o horário da consulta não podem estar no passado.")
            .Se(sala is not null && !sala.PodeReceberConsultas, "RN-SAL-05", "A sala está em manutenção.")
            .Se(convenio is not null && !convenio.ValidoEm(data), "RN-CON-03",
                $"O convênio {convenio?.Nome} está vencido para a data da consulta.")
            .LancarSeInvalido();

        var status = enviarParaTriagem ? StatusConsulta.AguardandoTriagem : StatusConsulta.Agendada;
        return new Consulta(0, data, horario, status, paciente.Id, medico!.Id, sala?.Id, convenio?.Id);
    }

    /// <summary>RN-PAC-07: entrada direta na fila de triagem, sem médico definido.</summary>
    public static Consulta EntradaNaFilaDeTriagem(int idPaciente, DateTime agora) =>
        new(0, DateOnly.FromDateTime(agora), TimeOnly.FromDateTime(agora), StatusConsulta.AguardandoTriagem, idPaciente, null, null, null);

    public void DefinirId(int id) => Id = id;

    public void EnviarParaTriagem() => MudarStatus(StatusConsulta.AguardandoTriagem, "RN-CNS-07");

    public void Cancelar() => MudarStatus(StatusConsulta.Cancelada, "RN-CNS-08");

    public void MarcarNaoCompareceu() => MudarStatus(StatusConsulta.NaoCompareceu, "RN-CNS-09");

    public void Liberar() => MudarStatus(StatusConsulta.Liberado, "RN-CNS-10");

    /// <summary>
    /// RN-TRI-01..05: registra a triagem desta consulta e coloca o paciente em atendimento.
    /// </summary>
    public Triagem RegistrarTriagem(decimal temperatura, string? pressaoArterial, int frequenciaCardiaca,
        int saturacaoOxigenio, int escalaDor, string? classificacaoInformada, string? queixa, DateTime agora)
    {
        Validacao.Garantir(Status == StatusConsulta.AguardandoTriagem, "RN-TRI-01",
            $"Só é possível triar consulta 'Aguardando Triagem' (status atual: {Status}).");

        PressaoArterial? pa = null;
        var informada = Texto.Limpar(classificacaoInformada);
        new Validacao()
            .Se(temperatura is < 30 or > 45, "RN-TRI-02", "Temperatura deve estar entre 30,0 e 45,0 °C.")
            .Se(frequenciaCardiaca is < 20 or > 250, "RN-TRI-02", "Frequência cardíaca deve estar entre 20 e 250 bpm.")
            .Se(saturacaoOxigenio is < 50 or > 100, "RN-TRI-02", "Saturação deve estar entre 50 e 100%.")
            .Se(escalaDor is < 0 or > 10, "RN-TRI-02", "Escala de dor deve estar entre 0 e 10.")
            .Se(!PressaoArterial.TentarCriar(pressaoArterial, out pa), "RN-TRI-03",
                "Pressão arterial inválida. Use sistólica/diastólica, ex.: 120/80.")
            .Se(informada is not null && !ClassificacaoRisco.PorGravidade.Contains(informada), "RN-TRI-04",
                $"Classificação inválida. Use: {string.Join(", ", ClassificacaoRisco.PorGravidade)}.")
            .LancarSeInvalido();

        var calculada = ClassificadorManchester.Classificar(
            new SinaisVitais(temperatura, frequenciaCardiaca, saturacaoOxigenio, escalaDor));

        Validacao.Garantir(informada is null || !ClassificadorManchester.EMenosGrave(informada, calculada), "RN-TRI-05",
            $"Pelos sinais vitais o risco mínimo é {calculada}; a classificação pode ser elevada, mas não reduzida.");

        var triagem = new Triagem(0, Id, IdPaciente, temperatura, pa!.ToString(), frequenciaCardiaca, saturacaoOxigenio,
            escalaDor, informada ?? calculada, calculada, "Manchester", Texto.Limpar(queixa), agora);

        MudarStatus(StatusConsulta.EmAtendimento, "RN-TRI-01");
        return triagem;
    }

    private void MudarStatus(string novo, string codigoRegra)
    {
        var permitido = Transicoes.TryGetValue(Status, out var destinos) && destinos.Contains(novo);
        Validacao.Garantir(permitido, codigoRegra, $"Não é possível mudar a consulta de '{Status}' para '{novo}'.");
        Status = novo;
    }
}
