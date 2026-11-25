using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;
using MySqlConnector;
using static HospitalSystem.Infrastructure.Persistence.DbSession;

namespace HospitalSystem.Infrastructure.Persistence.Repositories;

public class ConsultaRepository(DbSession db) : IConsultaRepository
{
    private const string Ativos = "('Agendada','Aguardando Triagem','Em Atendimento')"; // RN-CNS-06

    private static Consulta Mapear(MySqlDataReader r) => new(r.Int("id_consulta"), r.Data("data_consulta"), r.HoraN("horario"),
        r.Str("status_consulta"), r.Int("id_paciente"), r.IntN("id_medico"), r.IntN("id_sala"), r.IntN("id_convenio"));

    public Task<Consulta?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM consultas WHERE id_consulta=@id", Mapear, ct, P("@id", id));

    public async Task<bool> ExisteConflitoMedicoAsync(int idMedico, DateOnly data, TimeOnly horario, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>($@"SELECT COUNT(*) FROM consultas WHERE id_medico=@m AND data_consulta=@d AND horario=@h
                                       AND status_consulta IN {Ativos} AND id_consulta<>@ign", ct,
            P("@m", idMedico), P("@d", data), P("@h", horario), P("@ign", ignorarId ?? 0)) > 0;

    public async Task<bool> ExisteConflitoPacienteAsync(int idPaciente, DateOnly data, TimeOnly horario, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>($@"SELECT COUNT(*) FROM consultas WHERE id_paciente=@p AND data_consulta=@d AND horario=@h
                                       AND status_consulta IN {Ativos} AND id_consulta<>@ign", ct,
            P("@p", idPaciente), P("@d", data), P("@h", horario), P("@ign", ignorarId ?? 0)) > 0;

    public Task<int> AdicionarAsync(Consulta c, CancellationToken ct = default) =>
        db.InserirAsync(@"INSERT INTO consultas (data_consulta, horario, status_consulta, id_paciente, id_medico, id_sala, id_convenio)
                          VALUES (@d,@h,@s,@p,@m,@sala,@conv)", ct,
            P("@d", c.Data), P("@h", c.Horario), P("@s", c.Status), P("@p", c.IdPaciente), P("@m", c.IdMedico),
            P("@sala", c.IdSala), P("@conv", c.IdConvenio));

    public Task AtualizarStatusAsync(Consulta c, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE consultas SET status_consulta=@s WHERE id_consulta=@id", ct, P("@s", c.Status), P("@id", c.Id));

    public Task<int> AdicionarTriagemAsync(Triagem t, CancellationToken ct = default) =>
        db.InserirAsync(@"INSERT INTO triagem (temperatura, pressao_arterial, frequencia_cardiaca, saturacao_oxigenio, escala_dor,
                              classificacao_risco, classificacao_calculada, protocolo_manchester, queixa_principal, data_triagem, id_paciente, id_consulta)
                          VALUES (@temp,@pa,@fc,@sat,@dor,@risco,@calc,@prot,@queixa,@data,@pac,@cons)", ct,
            P("@temp", t.Temperatura), P("@pa", t.PressaoArterial), P("@fc", t.FrequenciaCardiaca), P("@sat", t.SaturacaoOxigenio),
            P("@dor", t.EscalaDor), P("@risco", t.ClassificacaoRisco), P("@calc", t.ClassificacaoCalculada), P("@prot", t.Protocolo),
            P("@queixa", t.QueixaPrincipal), P("@data", t.DataTriagem), P("@pac", t.IdPaciente), P("@cons", t.IdConsulta));
}

public class ProntuarioRepository(DbSession db) : IProntuarioRepository
{
    private static RegistroProntuario MapearRegistro(MySqlDataReader r) => new(r.Int("id_prontuario"), r.Int("id_consulta"),
        r.Str("evolucao_clinica"), r.StrN("observacoes"), r.DataHora("data_registro"), r.StrN("profissional"));

    private static Exame MapearExame(MySqlDataReader r) =>
        new(r.Int("id_exame"), r.Int("id_prontuario"), r.Str("nome_exame"), r.StrN("resultado"), r.Data("data_exame"));

    public Task<RegistroProntuario?> ObterRegistroAsync(int idProntuario, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM prontuario WHERE id_prontuario=@id", MapearRegistro, ct, P("@id", idProntuario));

    public Task<int> AdicionarRegistroAsync(RegistroProntuario r, CancellationToken ct = default) =>
        db.InserirAsync(@"INSERT INTO prontuario (evolucao_clinica, observacoes, data_registro, profissional, id_consulta)
                          VALUES (@ev,@obs,@data,@prof,@cons)", ct,
            P("@ev", r.EvolucaoClinica), P("@obs", r.Observacoes), P("@data", r.DataRegistro), P("@prof", r.Profissional), P("@cons", r.IdConsulta));

    public Task<Exame?> ObterExameAsync(int idExame, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM exames WHERE id_exame=@id", MapearExame, ct, P("@id", idExame));

    public Task<int> AdicionarExameAsync(Exame e, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO exames (nome_exame, resultado, data_exame, id_prontuario) VALUES (@n,@r,@d,@p)", ct,
            P("@n", e.Nome), P("@r", e.Resultado), P("@d", e.DataExame), P("@p", e.IdProntuario));

    public Task AtualizarExameAsync(Exame e, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE exames SET resultado=@r WHERE id_exame=@id", ct, P("@r", e.Resultado), P("@id", e.Id));

    public Task<int> AdicionarPrescricaoAsync(Prescricao p, CancellationToken ct = default) =>
        db.InserirAsync(@"INSERT INTO prescricoes (dosagem, frequencia, duracao_tratamento, data_prescricao, alerta_confirmado, id_prontuario, id_medicamento)
                          VALUES (@dos,@freq,@dur,@data,@alerta,@pront,@med)", ct,
            P("@dos", p.Dosagem), P("@freq", p.Frequencia), P("@dur", p.DuracaoTratamento), P("@data", p.DataPrescricao),
            P("@alerta", p.AlertaConfirmado), P("@pront", p.IdProntuario), P("@med", p.IdMedicamento));

    public async Task<IReadOnlyList<Medicamento>> MedicamentosPrescritosDesdeAsync(int idPaciente, DateTime desde, CancellationToken ct = default) =>
        await db.ListarAsync(@"SELECT DISTINCT m.* FROM prescricoes pr
                               JOIN prontuario p ON p.id_prontuario = pr.id_prontuario
                               JOIN consultas c ON c.id_consulta = p.id_consulta
                               JOIN medicamentos m ON m.id_medicamento = pr.id_medicamento
                               WHERE c.id_paciente = @pac AND COALESCE(pr.data_prescricao, p.data_registro) >= @desde",
            MedicamentoRepository.Mapear, ct, P("@pac", idPaciente), P("@desde", desde));
}

public class LeitoRepository(DbSession db) : ILeitoRepository
{
    private static Leito Mapear(MySqlDataReader r) =>
        new(r.Int("id_leito"), r.Str("numero_leito"), r.Str("ala"), r.Str("status_leito"), r.IntN("tempo_higienizacao"));

    public Task<Leito?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM leitos WHERE id_leito=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<Leito>> ListarAsync(string? status, CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM leitos WHERE (@s IS NULL OR status_leito=@s) ORDER BY ala, numero_leito", Mapear, ct, P("@s", status));

    public async Task<bool> NumeroExisteNaAlaAsync(string numero, string ala, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM leitos WHERE numero_leito=@n AND ala=@a AND id_leito<>@ign", ct,
            P("@n", numero), P("@a", ala), P("@ign", ignorarId ?? 0)) > 0;

    public async Task<bool> PossuiInternacoesAsync(int id, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM internacoes WHERE id_leito=@id", ct, P("@id", id)) > 0;

    public Task<int> AdicionarAsync(Leito l, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO leitos (numero_leito, ala, status_leito, tempo_higienizacao) VALUES (@n,@a,@s,@t)", ct,
            P("@n", l.Numero), P("@a", l.Ala), P("@s", l.Status), P("@t", l.TempoHigienizacao));

    public Task AtualizarAsync(Leito l, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE leitos SET numero_leito=@n, ala=@a, status_leito=@s WHERE id_leito=@id", ct,
            P("@n", l.Numero), P("@a", l.Ala), P("@s", l.Status), P("@id", l.Id));

    public Task RemoverAsync(int id, CancellationToken ct = default) =>
        db.ExecutarAsync("DELETE FROM leitos WHERE id_leito=@id", ct, P("@id", id));
}

public class InternacaoRepository(DbSession db) : IInternacaoRepository
{
    private static Internacao Mapear(MySqlDataReader r) => new(r.Int("id_internacao"), r.DataHora("data_entrada"), r.DataHoraN("data_alta"),
        r.Str("status_internacao"), r.Int("id_paciente"), r.Int("id_leito"));

    public Task<Internacao?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM internacoes WHERE id_internacao=@id", Mapear, ct, P("@id", id));

    public Task<Internacao?> ObterAtivaPorPacienteAsync(int idPaciente, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM internacoes WHERE id_paciente=@p AND status_internacao=@s LIMIT 1", Mapear, ct,
            P("@p", idPaciente), P("@s", StatusInternacao.Ativa));

    public Task<Internacao?> ObterAtivaPorLeitoAsync(int idLeito, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM internacoes WHERE id_leito=@l AND status_internacao=@s LIMIT 1", Mapear, ct,
            P("@l", idLeito), P("@s", StatusInternacao.Ativa));

    public Task<int> AdicionarAsync(Internacao i, CancellationToken ct = default) =>
        db.InserirAsync(@"INSERT INTO internacoes (data_entrada, data_alta, status_internacao, id_paciente, id_leito)
                          VALUES (@e,@a,@s,@p,@l)", ct,
            P("@e", i.DataEntrada), P("@a", i.DataAlta), P("@s", i.Status), P("@p", i.IdPaciente), P("@l", i.IdLeito));

    public Task AtualizarAsync(Internacao i, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE internacoes SET data_alta=@a, status_internacao=@s, id_leito=@l WHERE id_internacao=@id", ct,
            P("@a", i.DataAlta), P("@s", i.Status), P("@l", i.IdLeito), P("@id", i.Id));
}

public class InsumoRepository(DbSession db) : IInsumoRepository
{
    private static Insumo Mapear(MySqlDataReader r) =>
        new(r.Int("id_insumo"), r.Str("nome_insumo"), r.Int("quantidade_estoque"), r.Dec("valor_unitario"));

    public Task<Insumo?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM insumos WHERE id_insumo=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<Insumo>> ListarAsync(CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM insumos ORDER BY nome_insumo", Mapear, ct);

    public async Task<bool> NomeExisteAsync(string nome, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM insumos WHERE nome_insumo=@n AND id_insumo<>@ign", ct, P("@n", nome), P("@ign", ignorarId ?? 0)) > 0;

    public Task<int> AdicionarAsync(Insumo i, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO insumos (nome_insumo, quantidade_estoque, valor_unitario) VALUES (@n,@q,@v)", ct,
            P("@n", i.Nome), P("@q", i.QuantidadeEstoque), P("@v", i.ValorUnitario));

    public Task AtualizarAsync(Insumo i, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE insumos SET nome_insumo=@n, quantidade_estoque=@q, valor_unitario=@v WHERE id_insumo=@id", ct,
            P("@n", i.Nome), P("@q", i.QuantidadeEstoque), P("@v", i.ValorUnitario), P("@id", i.Id));

    public Task<int> AdicionarUsoAsync(UsoInsumo u, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO uso_insumos (quantidade_utilizada, data_uso, id_leito, id_insumo, id_internacao) VALUES (@q,@d,@l,@i,@int)", ct,
            P("@q", u.QuantidadeUtilizada), P("@d", u.DataUso), P("@l", u.IdLeito), P("@i", u.IdInsumo), P("@int", u.IdInternacao));
}

public class FaturamentoRepository(DbSession db) : IFaturamentoRepository
{
    private static Faturamento Mapear(MySqlDataReader r) => new(r.Int("id_faturamento"), r.Dec("valor_total"), r.Data("data_faturamento"),
        r.Str("status_pagamento"), r.Int("id_paciente"), r.DataN("data_pagamento"));

    public Task<Faturamento?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM faturamento WHERE id_faturamento=@id", Mapear, ct, P("@id", id));

    public Task<int> AdicionarAsync(Faturamento f, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO faturamento (valor_total, data_faturamento, status_pagamento, data_pagamento, id_paciente) VALUES (@v,@d,@s,@dp,@p)", ct,
            P("@v", f.ValorTotal), P("@d", f.DataFaturamento), P("@s", f.Status), P("@dp", f.DataPagamento), P("@p", f.IdPaciente));

    public Task AtualizarAsync(Faturamento f, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE faturamento SET status_pagamento=@s, data_pagamento=@dp WHERE id_faturamento=@id", ct,
            P("@s", f.Status), P("@dp", f.DataPagamento), P("@id", f.Id));

    public Task AdicionarAuditoriaAsync(Auditoria a, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO auditoria (descricao, data_auditoria, status_auditoria, id_faturamento) VALUES (@d,@dt,@s,@f)", ct,
            P("@d", a.Descricao), P("@dt", a.DataAuditoria), P("@s", a.Status), P("@f", a.IdFaturamento));
}
