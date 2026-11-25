namespace HospitalSystem.Application.DTOs;

// ---------- Pacientes ----------
public sealed record PacienteRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public DateOnly DataNascimento { get; init; }
    public string? Telefone { get; init; }
    public string? Endereco { get; init; }
    public string? Alergias { get; init; }
    public string? HistoricoClinico { get; init; }
    public string? TipoSanguineo { get; init; }
    /// <summary>RN-PAC-07: só é usado no cadastro. Padrão: true.</summary>
    public bool EnviarParaTriagem { get; init; } = true;
}

public sealed record PacienteDto(int Id, string Nome, string Cpf, string CpfFormatado, DateOnly DataNascimento, int Idade,
    string? Telefone, string? Endereco, string? Alergias, string? HistoricoClinico, string? TipoSanguineo);

// ---------- Médicos ----------
public sealed record MedicoRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Crm { get; init; } = string.Empty;
    public string Especialidade { get; init; } = string.Empty;
    public string? Telefone { get; init; }
    public string? Email { get; init; }
}

public sealed record MedicoDto(int Id, string Nome, string Crm, string Especialidade, string? Telefone, string? Email);

// ---------- Convênios ----------
public sealed record ConvenioRequest
{
    public string Nome { get; init; } = string.Empty;
    public DateOnly? ValidadeCarteira { get; init; }
    public string? Telefone { get; init; }
}

public sealed record ConvenioDto(int Id, string Nome, DateOnly? ValidadeCarteira, string? Telefone, bool Vencido);

// ---------- Salas ----------
public sealed record CriarSalaRequest
{
    public string Numero { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    /// <summary>RN-SAL-04: cria também um leito com o mesmo número.</summary>
    public bool VincularLeito { get; init; }
    public string? Ala { get; init; }
}

public sealed record AtualizarSalaRequest
{
    public string Numero { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed record SalaDto(int Id, string Numero, string Tipo, string Status, int? IdLeitoCriado = null);

// ---------- Medicamentos ----------
public sealed record MedicamentoRequest
{
    public string Nome { get; init; } = string.Empty;
    public string? Contraindicacoes { get; init; }
    public string? InteracoesMedicamentosas { get; init; }
}

public sealed record MedicamentoDto(int Id, string Nome, string? Contraindicacoes, string? InteracoesMedicamentosas);
