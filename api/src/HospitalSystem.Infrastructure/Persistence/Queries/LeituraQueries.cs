using HospitalSystem.Application.Abstractions;
using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.Services;
using MySqlConnector;
using static HospitalSystem.Infrastructure.Persistence.DbSession;

namespace HospitalSystem.Infrastructure.Persistence.Queries;

public class ConsultaQueries(DbSession db) : IConsultaQueries
{
    internal const string SelectConsulta = @"
        SELECT c.id_consulta, c.data_consulta, c.horario, c.status_consulta, c.id_paciente, p.nome AS paciente_nome,
               c.id_medico, m.nome AS medico_nome, m.especialidade, c.id_sala, s.numero_sala, c.id_convenio, cv.nome_convenio
        FROM consultas c
        JOIN pacientes p ON p.id_paciente = c.id_paciente
        LEFT JOIN medicos m ON m.id_medico = c.id_medico
        LEFT JOIN salas s ON s.id_sala = c.id_sala
        LEFT JOIN convenios cv ON cv.id_convenio = c.id_convenio";

    internal static ConsultaDto Mapear(MySqlDataReader r) => new(r.Int("id_consulta"), r.Data("data_consulta"), r.HoraN("horario"),
        r.Str("status_consulta"), r.Int("id_paciente"), r.Str("paciente_nome"), r.IntN("id_medico"), r.StrN("medico_nome"),
        r.StrN("especialidade"), r.IntN("id_sala"), r.StrN("numero_sala"), r.IntN("id_convenio"), r.StrN("nome_convenio"));

    internal static TriagemDto MapearTriagem(MySqlDataReader r) => new(r.Int("id_triagem"), r.IntN("id_consulta"), r.Int("id_paciente"),
        r.Dec("temperatura"), r.Str("pressao_arterial"), r.Int("frequencia_cardiaca"), r.Int("saturacao_oxigenio"), r.Int("escala_dor"),
        r.Str("classificacao_risco"), r.StrN("classificacao_calculada"), r.StrN("queixa_principal"), r.DataHoraN("data_triagem"));

    public Task<ConsultaDto?> ObterAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync($"{SelectConsulta} WHERE c.id_consulta=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<ConsultaDto>> ListarAsync(DateOnly? data, string? status, CancellationToken ct = default) =>
        await db.ListarAsync($@"{SelectConsulta}
            WHERE (@data IS NULL OR c.data_consulta = @data) AND (@status IS NULL OR c.status_consulta = @status)
            ORDER BY c.data_consulta DESC, c.horario ASC, c.id_consulta DESC LIMIT 500", Mapear, ct, P("@data", data), P("@status", status));

    public async Task<IReadOnlyList<FilaTriagemItemDto>> FilaTriagemAsync(CancellationToken ct = default) =>
        await db.ListarAsync($@"{SelectConsulta} WHERE c.status_consulta = @s ORDER BY c.data_consulta, c.horario, c.id_consulta",
            r => new FilaTriagemItemDto(r.Int("id_consulta"), r.Int("id_paciente"), r.Str("paciente_nome"), r.Data("data_consulta"),
                r.HoraN("horario"), r.StrN("medico_nome")), ct, P("@s", StatusConsulta.AguardandoTriagem));

    /// <summary>
    /// Consultas em atendimento com a triagem mais recente de cada uma.
    /// Triagens antigas (do sistema PHP) não têm id_consulta: nesse caso usa a última triagem do paciente.
    /// A ORDENAÇÃO por gravidade é feita no serviço, com a regra do domínio (RN-TRI-06).
    /// </summary>
    public async Task<IReadOnlyList<FilaAtendimentoItemDto>> FilaAtendimentoAsync(CancellationToken ct = default) =>
        await db.ListarAsync(@"
            SELECT c.id_consulta, c.id_paciente, p.nome AS paciente_nome, t.id_triagem, t.classificacao_risco, t.temperatura,
                   t.pressao_arterial, t.frequencia_cardiaca, t.saturacao_oxigenio, t.escala_dor, t.queixa_principal, t.data_triagem
            FROM consultas c
            JOIN pacientes p ON p.id_paciente = c.id_paciente
            LEFT JOIN triagem t ON t.id_triagem = (
                SELECT MAX(t2.id_triagem) FROM triagem t2
                WHERE t2.id_consulta = c.id_consulta OR (t2.id_consulta IS NULL AND t2.id_paciente = c.id_paciente))
            WHERE c.status_consulta = @s",
            r => new FilaAtendimentoItemDto
            {
                IdConsulta = r.Int("id_consulta"),
                IdPaciente = r.Int("id_paciente"),
                PacienteNome = r.Str("paciente_nome"),
                IdTriagem = r.Int("id_triagem"),
                ClassificacaoRisco = r.StrN("classificacao_risco") ?? "Sem triagem",
                Temperatura = r.Dec("temperatura"),
                PressaoArterial = r.StrN("pressao_arterial"),
                FrequenciaCardiaca = r.Int("frequencia_cardiaca"),
                SaturacaoOxigenio = r.Int("saturacao_oxigenio"),
                EscalaDor = r.Int("escala_dor"),
                QueixaPrincipal = r.StrN("queixa_principal"),
                DataTriagem = r.DataHoraN("data_triagem")
            }, ct, P("@s", StatusConsulta.EmAtendimento));

    public Task<TriagemDto?> ObterTriagemAsync(int idTriagem, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM triagem WHERE id_triagem=@id", MapearTriagem, ct, P("@id", idTriagem));
}

public class ProntuarioQueries(DbSession db) : IProntuarioQueries
{
    private const string SelectPrescricao = @"
        SELECT pr.*, m.nome_medicamento FROM prescricoes pr
        JOIN medicamentos m ON m.id_medicamento = pr.id_medicamento";

    private static PrescricaoDto MapearPrescricao(MySqlDataReader r) => new(r.Int("id_prescricao"), r.Int("id_prontuario"),
        r.Int("id_medicamento"), r.Str("nome_medicamento"), r.Str("dosagem"), r.Str("frequencia"), r.Str("duracao_tratamento"),
        r.DataHoraN("data_prescricao"), r.StrN("alerta_confirmado"));

    public async Task<IReadOnlyList<TriagemDto>> TriagensDoPacienteAsync(int idPaciente, CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM triagem WHERE id_paciente=@p ORDER BY data_triagem DESC, id_triagem DESC",
            ConsultaQueries.MapearTriagem, ct, P("@p", idPaciente));

    public async Task<IReadOnlyList<RegistroProntuarioDto>> RegistrosDoPacienteAsync(int idPaciente, CancellationToken ct = default)
    {
        var registros = await db.ListarAsync(@"
            SELECT pr.* FROM prontuario pr JOIN consultas c ON c.id_consulta = pr.id_consulta
            WHERE c.id_paciente = @p ORDER BY pr.data_registro DESC, pr.id_prontuario DESC",
            r => new RegistroProntuarioDto
            {
                Id = r.Int("id_prontuario"),
                IdConsulta = r.Int("id_consulta"),
                EvolucaoClinica = r.Str("evolucao_clinica"),
                Observacoes = r.StrN("observacoes"),
                DataRegistro = r.DataHoraN("data_registro"),
                Profissional = r.StrN("profissional")
            }, ct, P("@p", idPaciente));

        if (registros.Count == 0)
        {
            return registros;
        }

        var prescricoes = await db.ListarAsync($@"{SelectPrescricao}
            JOIN prontuario p ON p.id_prontuario = pr.id_prontuario JOIN consultas c ON c.id_consulta = p.id_consulta
            WHERE c.id_paciente = @p ORDER BY pr.id_prescricao", MapearPrescricao, ct, P("@p", idPaciente));

        var exames = await db.ListarAsync(@"
            SELECT e.* FROM exames e JOIN prontuario p ON p.id_prontuario = e.id_prontuario JOIN consultas c ON c.id_consulta = p.id_consulta
            WHERE c.id_paciente = @p ORDER BY e.id_exame",
            r => new ExameDto(r.Int("id_exame"), r.Int("id_prontuario"), r.Str("nome_exame"), r.StrN("resultado"), r.DataN("data_exame")),
            ct, P("@p", idPaciente));

        foreach (var reg in registros)
        {
            reg.Prescricoes.AddRange(prescricoes.Where(x => x.IdProntuario == reg.Id));
            reg.Exames.AddRange(exames.Where(x => x.IdProntuario == reg.Id));
        }
        return registros;
    }

    public async Task<int?> IdPacienteDoProntuarioAsync(int idProntuario, CancellationToken ct = default) =>
        await db.EscalarAsync<int?>(@"SELECT c.id_paciente FROM prontuario p JOIN consultas c ON c.id_consulta = p.id_consulta
                                      WHERE p.id_prontuario = @id", ct, P("@id", idProntuario));

    public Task<PrescricaoDto?> ObterPrescricaoAsync(int idPrescricao, CancellationToken ct = default) =>
        db.PrimeiroAsync($"{SelectPrescricao} WHERE pr.id_prescricao=@id", MapearPrescricao, ct, P("@id", idPrescricao));
}

public class LeitoQueries(DbSession db) : ILeitoQueries
{
    public async Task<IReadOnlyList<LeitoDto>> MapaAsync(string? status, CancellationToken ct = default) =>
        await db.ListarAsync(@"
            SELECT l.*, i.id_internacao, p.nome AS paciente_nome
            FROM leitos l
            LEFT JOIN internacoes i ON i.id_leito = l.id_leito AND i.status_internacao = 'Ativa'
            LEFT JOIN pacientes p ON p.id_paciente = i.id_paciente
            WHERE (@s IS NULL OR l.status_leito = @s)
            ORDER BY l.ala, l.numero_leito",
            r => new LeitoDto(r.Int("id_leito"), r.Str("numero_leito"), r.Str("ala"), r.Str("status_leito"), r.IntN("tempo_higienizacao"),
                r.IntN("id_internacao"), r.StrN("paciente_nome")), ct, P("@s", status));
}

public class InternacaoQueries(DbSession db) : IInternacaoQueries
{
    private const string Select = @"
        SELECT i.*, p.nome AS paciente_nome, l.numero_leito, l.ala
        FROM internacoes i
        JOIN pacientes p ON p.id_paciente = i.id_paciente
        JOIN leitos l ON l.id_leito = i.id_leito";

    private static InternacaoDto Mapear(MySqlDataReader r) => new(r.Int("id_internacao"), r.Int("id_paciente"), r.Str("paciente_nome"),
        r.Int("id_leito"), r.Str("numero_leito"), r.Str("ala"), r.DataHora("data_entrada"), r.DataHoraN("data_alta"), r.Str("status_internacao"));

    public Task<InternacaoDto?> ObterAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync($"{Select} WHERE i.id_internacao=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<InternacaoDto>> ListarAsync(bool somenteAtivas, CancellationToken ct = default) =>
        await db.ListarAsync($"{Select} WHERE (@ativas = 0 OR i.status_internacao = 'Ativa') ORDER BY i.data_entrada DESC LIMIT 500",
            Mapear, ct, P("@ativas", somenteAtivas));
}

public class FaturamentoQueries(DbSession db) : IFaturamentoQueries
{
    private const string Select = @"
        SELECT f.*, p.nome AS paciente_nome FROM faturamento f JOIN pacientes p ON p.id_paciente = f.id_paciente";

    private static FaturamentoDto Mapear(MySqlDataReader r) => new(r.Int("id_faturamento"), r.Int("id_paciente"), r.Str("paciente_nome"),
        r.Dec("valor_total"), r.Data("data_faturamento"), r.Str("status_pagamento"));

    public Task<FaturamentoDto?> ObterAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync($"{Select} WHERE f.id_faturamento=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<FaturamentoDto>> ListarAsync(string? status, CancellationToken ct = default) =>
        await db.ListarAsync($"{Select} WHERE (@s IS NULL OR f.status_pagamento=@s) ORDER BY f.data_faturamento DESC, f.id_faturamento DESC",
            Mapear, ct, P("@s", status));

    public async Task<ResumoFaturamentoDto> ResumoAsync(DateOnly inicioMes, DateOnly fimMes, CancellationToken ct = default) =>
        (await db.ListarAsync(@"
            SELECT
              COALESCE(SUM(CASE WHEN status_pagamento='Pendente' THEN valor_total END),0) AS pendente,
              COALESCE(SUM(CASE WHEN status_pagamento='Pago'
                                 AND COALESCE(data_pagamento, data_faturamento) BETWEEN @ini AND @fim THEN valor_total END),0) AS recebido,
              COALESCE(SUM(CASE WHEN status_pagamento='Auditoria' THEN 1 ELSE 0 END),0) AS auditoria
            FROM faturamento",
            r => new ResumoFaturamentoDto(r.Dec("pendente"), r.Dec("recebido"), r.Int("auditoria")), ct,
            P("@ini", inicioMes), P("@fim", fimMes))).Single();

    /// <summary>
    /// RN-FAT-02: insumos lançados nas internações do paciente. Registros antigos (sem id_internacao)
    /// são atribuídos pelo leito e pelo período da internação.
    /// </summary>
    public async Task<(decimal TotalInsumos, int Exames, int Prescricoes)> DadosConsolidacaoAsync(int idPaciente, DateTime agora, CancellationToken ct = default)
    {
        var insumos = await db.EscalarAsync<decimal>(@"
            SELECT COALESCE(SUM(u.quantidade_utilizada * ins.valor_unitario), 0)
            FROM uso_insumos u
            JOIN insumos ins ON ins.id_insumo = u.id_insumo
            WHERE u.id_internacao IN (SELECT id_internacao FROM internacoes WHERE id_paciente = @p)
               OR (u.id_internacao IS NULL AND EXISTS (
                    SELECT 1 FROM internacoes i
                    WHERE i.id_paciente = @p AND i.id_leito = u.id_leito
                      AND u.data_uso >= i.data_entrada AND u.data_uso <= COALESCE(i.data_alta, @agora)))",
            ct, P("@p", idPaciente), P("@agora", agora));

        var exames = await db.EscalarAsync<long>(@"
            SELECT COUNT(*) FROM exames e JOIN prontuario pr ON pr.id_prontuario = e.id_prontuario
            JOIN consultas c ON c.id_consulta = pr.id_consulta WHERE c.id_paciente = @p", ct, P("@p", idPaciente));

        var prescricoes = await db.EscalarAsync<long>(@"
            SELECT COUNT(*) FROM prescricoes ps JOIN prontuario pr ON pr.id_prontuario = ps.id_prontuario
            JOIN consultas c ON c.id_consulta = pr.id_consulta WHERE c.id_paciente = @p", ct, P("@p", idPaciente));

        return (insumos, (int)exames, (int)prescricoes);
    }
}

public class DashboardQueries(DbSession db, IConsultaQueries consultas, TimeProvider relogio) : IDashboardQueries
{
    public async Task<DashboardDto> ObterAsync(DateOnly hoje, CancellationToken ct = default)
    {
        var contagens = (await db.ListarAsync(@"
            SELECT
              (SELECT COUNT(*) FROM internacoes WHERE status_internacao = 'Ativa') AS internados,
              (SELECT COUNT(*) FROM leitos WHERE status_leito = 'Disponível') AS leitos,
              (SELECT COUNT(*) FROM consultas WHERE data_consulta = @hoje AND status_consulta NOT IN ('Cancelada')) AS consultas_hoje,
              (SELECT COUNT(*) FROM consultas WHERE status_consulta = 'Aguardando Triagem') AS pendentes",
            r => (Internados: r.Int("internados"), Leitos: r.Int("leitos"), Hoje: r.Int("consultas_hoje"), Pendentes: r.Int("pendentes")),
            ct, P("@hoje", hoje))).Single();

        var alertas = (await consultas.FilaAtendimentoAsync(ct))
            .Where(f => f.ClassificacaoRisco is ClassificacaoRisco.Vermelho or ClassificacaoRisco.Laranja or ClassificacaoRisco.Amarelo)
            .OrderBy(f => ClassificadorManchester.Prioridade(f.ClassificacaoRisco))   // RN-TRI-06
            .ThenBy(f => f.DataTriagem)
            .Take(5)
            .Select(f => new AlertaTriagemDto(f.IdPaciente, f.PacienteNome, f.ClassificacaoRisco, f.PressaoArterial, f.Temperatura))
            .ToList();

        var proximas = await db.ListarAsync($@"{ConsultaQueries.SelectConsulta}
            WHERE c.data_consulta = @hoje AND c.status_consulta IN ('Agendada','Aguardando Triagem')
            ORDER BY c.horario LIMIT 5", ConsultaQueries.Mapear, ct, P("@hoje", hoje));

        return new DashboardDto(contagens.Internados, contagens.Leitos, contagens.Hoje, contagens.Pendentes, alertas, proximas, relogio.GetLocalNow().DateTime);
    }
}
