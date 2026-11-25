namespace HospitalSystem.Application.DTOs;

// ---------- Leitos ----------
public sealed record LeitoRequest
{
    public string Numero { get; init; } = string.Empty;
    public string Ala { get; init; } = string.Empty;
}

public sealed record AlterarStatusLeitoRequest
{
    public string Status { get; init; } = string.Empty;
}

public sealed record LeitoDto(int Id, string Numero, string Ala, string Status, int? TempoHigienizacao,
    int? IdInternacaoAtiva = null, string? PacienteAtual = null);

// ---------- Internações ----------
public sealed record InternarRequest
{
    public int IdPaciente { get; init; }
    public int IdLeito { get; init; }
}

public sealed record TransferirRequest
{
    public int IdNovoLeito { get; init; }
}

public sealed record InternacaoDto(int Id, int IdPaciente, string PacienteNome, int IdLeito, string LeitoNumero, string Ala,
    DateTime DataEntrada, DateTime? DataAlta, string Status);

// ---------- Estoque ----------
public sealed record CriarInsumoRequest
{
    public string Nome { get; init; } = string.Empty;
    public int QuantidadeInicial { get; init; }
    public decimal ValorUnitario { get; init; }
}

public sealed record AtualizarInsumoRequest
{
    public string Nome { get; init; } = string.Empty;
    public decimal ValorUnitario { get; init; }
}

public sealed record MovimentacaoRequest
{
    /// <summary>"entrada" ou "saida"</summary>
    public string Tipo { get; init; } = string.Empty;
    public int Quantidade { get; init; }
}

public sealed record UsoInsumoRequest
{
    public int IdLeito { get; init; }
    public int Quantidade { get; init; }
}

public sealed record InsumoDto(int Id, string Nome, int QuantidadeEstoque, decimal ValorUnitario, bool EstoqueCritico);

// ---------- Faturamento ----------
public sealed record EmitirGuiaRequest
{
    public int IdPaciente { get; init; }
    /// <summary>Opcional: se omitido usa a consolidação de gastos (RN-FAT-03).</summary>
    public decimal? Valor { get; init; }
}

public sealed record CancelarGuiaRequest
{
    public string Motivo { get; init; } = string.Empty;
}

public sealed record FaturamentoDto(int Id, int IdPaciente, string PacienteNome, decimal ValorTotal, DateOnly DataFaturamento, string Status);

public sealed record ResumoFaturamentoDto(decimal TotalPendente, decimal TotalRecebidoMes, int QuantidadeEmAuditoria);

public sealed record ConsolidacaoGastosDto(int IdPaciente, decimal TotalInsumos, int QuantidadeExames, decimal TotalExames,
    int QuantidadePrescricoes, decimal TotalPrescricoes, decimal ValorTotal);

// ---------- Dashboard ----------
public sealed record AlertaTriagemDto(int IdPaciente, string PacienteNome, string ClassificacaoRisco, string? PressaoArterial, decimal Temperatura);

public sealed record DashboardDto(int PacientesInternados, int LeitosDisponiveis, int ConsultasHoje, int TriagensPendentes,
    IReadOnlyList<AlertaTriagemDto> AlertasTriagem, IReadOnlyList<ConsultaDto> ProximasConsultas, DateTime GeradoEm);
