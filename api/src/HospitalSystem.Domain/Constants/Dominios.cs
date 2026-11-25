namespace HospitalSystem.Domain.Constants;

// Os valores são gravados no banco exatamente como no sistema PHP (com acentos),
// por isso são constantes de texto e não enums.

public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string Medico = "Medico";
    public const string Enfermagem = "Enfermagem";
    public const string Recepcao = "Recepcao";

    public static readonly IReadOnlyList<string> Todos = new[] { Administrador, Medico, Enfermagem, Recepcao };
}

public static class StatusConsulta
{
    public const string Agendada = "Agendada";
    public const string AguardandoTriagem = "Aguardando Triagem";
    public const string EmAtendimento = "Em Atendimento";
    public const string Liberado = "Liberado";
    public const string Cancelada = "Cancelada";
    public const string NaoCompareceu = "Não Compareceu";

    /// <summary>RN-CNS-06: status que ocupam a agenda do médico/paciente.</summary>
    public static readonly IReadOnlyList<string> Ativos = new[] { Agendada, AguardandoTriagem, EmAtendimento };
}

public static class StatusLeito
{
    public const string Disponivel = "Disponível";
    public const string Ocupado = "Ocupado";
    public const string Higienizacao = "Higienização";
    public const string Manutencao = "Manutenção";

    public static readonly IReadOnlyList<string> Todos = new[] { Disponivel, Ocupado, Higienizacao, Manutencao };
}

public static class StatusInternacao
{
    public const string Ativa = "Ativa";
    public const string Alta = "Alta";
}

public static class StatusSala
{
    public const string Disponivel = "Disponível";
    public const string Ocupada = "Ocupada";
    public const string Manutencao = "Manutenção";

    public static readonly IReadOnlyList<string> Todos = new[] { Disponivel, Ocupada, Manutencao };
}

public static class TiposSala
{
    public static readonly IReadOnlyList<string> Todos = new[] { "Consultório", "Cirurgia", "Exame", "Triagem", "Internação" };
}

public static class StatusPagamento
{
    public const string Pendente = "Pendente";
    public const string Auditoria = "Auditoria";
    public const string Pago = "Pago";
    public const string Cancelado = "Cancelado";
}

public static class ClassificacaoRisco
{
    public const string Vermelho = "Vermelho";
    public const string Laranja = "Laranja";
    public const string Amarelo = "Amarelo";
    public const string Verde = "Verde";
    public const string Azul = "Azul";

    /// <summary>Da mais grave para a menos grave.</summary>
    public static readonly IReadOnlyList<string> PorGravidade = new[] { Vermelho, Laranja, Amarelo, Verde, Azul };
}

public static class TiposSanguineos
{
    public static readonly IReadOnlyList<string> Todos = new[] { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
}

public static class Especialidades
{
    public static readonly IReadOnlyList<string> Todas = new[]
    {
        "Cardiologia", "Neurologia", "Ortopedia", "Pediatria", "Ginecologia", "Oftalmologia",
        "Otorrinolaringologia", "Dermatologia", "Psiquiatria", "Cirurgia Geral", "Clínica Geral"
    };
}
