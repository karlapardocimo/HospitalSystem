namespace HospitalSystem.Application.DTOs;

// ---------- Consultas ----------
public sealed record AgendarConsultaRequest
{
    public int IdPaciente { get; init; }
    public int? IdMedico { get; init; }
    public int? IdSala { get; init; }
    public int? IdConvenio { get; init; }
    public DateOnly Data { get; init; }
    public TimeOnly Horario { get; init; }
    public bool EnviarParaTriagem { get; init; }
}

public sealed record ConsultaDto(int Id, DateOnly Data, TimeOnly? Horario, string Status,
    int IdPaciente, string PacienteNome, int? IdMedico, string? MedicoNome, string? Especialidade,
    int? IdSala, string? SalaNumero, int? IdConvenio, string? ConvenioNome);

public sealed record FilaTriagemItemDto(int IdConsulta, int IdPaciente, string PacienteNome, DateOnly Data, TimeOnly? Horario, string? MedicoNome);

public sealed record FilaAtendimentoItemDto
{
    public int IdConsulta { get; init; }
    public int IdPaciente { get; init; }
    public string PacienteNome { get; init; } = string.Empty;
    public int IdTriagem { get; init; }
    public string ClassificacaoRisco { get; init; } = string.Empty;
    public decimal Temperatura { get; init; }
    public string? PressaoArterial { get; init; }
    public int FrequenciaCardiaca { get; init; }
    public int SaturacaoOxigenio { get; init; }
    public int EscalaDor { get; init; }
    public string? QueixaPrincipal { get; init; }
    public DateTime? DataTriagem { get; init; }
    // Calculados pelo serviço (RN-TRI-06 / RN-TRI-07)
    public int Prioridade { get; init; }
    public int TempoAlvoMinutos { get; init; }
    public int MinutosAguardando { get; init; }
    public bool Atrasado { get; init; }
}

// ---------- Triagem ----------
public sealed record TriagemRequest
{
    public decimal Temperatura { get; init; }
    public string PressaoArterial { get; init; } = string.Empty;
    public int FrequenciaCardiaca { get; init; }
    public int SaturacaoOxigenio { get; init; } = 98;
    public int EscalaDor { get; init; }
    /// <summary>Opcional: o profissional pode elevar a classificação calculada (RN-TRI-05).</summary>
    public string? ClassificacaoRisco { get; init; }
    public string? QueixaPrincipal { get; init; }
}

public sealed record TriagemDto(int Id, int? IdConsulta, int IdPaciente, decimal Temperatura, string PressaoArterial,
    int FrequenciaCardiaca, int SaturacaoOxigenio, int EscalaDor, string ClassificacaoRisco, string? ClassificacaoCalculada,
    string? QueixaPrincipal, DateTime? DataTriagem);

// ---------- Prontuário ----------
public sealed record RegistroProntuarioRequest
{
    public string EvolucaoClinica { get; init; } = string.Empty;
    public string? Observacoes { get; init; }
}

public sealed record PrescricaoRequest
{
    public int IdMedicamento { get; init; }
    public string Dosagem { get; init; } = string.Empty;
    public string Frequencia { get; init; } = string.Empty;
    public string DuracaoTratamento { get; init; } = string.Empty;
    /// <summary>RN-PRE-04: confirma ciência de interação medicamentosa.</summary>
    public bool ConfirmarInteracao { get; init; }
}

public sealed record ExameRequest
{
    public string NomeExame { get; init; } = string.Empty;
    public DateOnly? DataExame { get; init; }
    public string? Resultado { get; init; }
}

public sealed record ResultadoExameRequest
{
    public string Resultado { get; init; } = string.Empty;
}

public sealed record PrescricaoDto(int Id, int IdProntuario, int IdMedicamento, string NomeMedicamento, string Dosagem,
    string Frequencia, string DuracaoTratamento, DateTime? DataPrescricao, string? AlertaConfirmado);

public sealed record PrescricaoCriadaDto(PrescricaoDto Prescricao, IReadOnlyList<string> Alertas);

public sealed record ExameDto(int Id, int IdProntuario, string NomeExame, string? Resultado, DateOnly? DataExame);

public sealed record RegistroProntuarioDto
{
    public int Id { get; init; }
    public int IdConsulta { get; init; }
    public string EvolucaoClinica { get; init; } = string.Empty;
    public string? Observacoes { get; init; }
    public DateTime? DataRegistro { get; init; }
    public string? Profissional { get; init; }
    public List<PrescricaoDto> Prescricoes { get; init; } = new();
    public List<ExameDto> Exames { get; init; } = new();
}

public sealed record ProntuarioPacienteDto(PacienteDto Paciente, IReadOnlyList<ConsultaDto> ConsultaEmAtendimento,
    IReadOnlyList<TriagemDto> Triagens, IReadOnlyList<RegistroProntuarioDto> Registros);
