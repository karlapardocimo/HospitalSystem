using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;
using MySqlConnector;
using static HospitalSystem.Infrastructure.Persistence.DbSession;

namespace HospitalSystem.Infrastructure.Persistence.Repositories;

public class UsuarioRepository(DbSession db) : IUsuarioRepository
{
    private static Usuario Mapear(MySqlDataReader r) => new(r.Int("id_usuario"), r.Str("nome"), r.Str("email"), r.Str("senha_hash"),
        r.Str("perfil"), r.Bool("ativo"), r.Int("tentativas_falhas"), r.DataHoraN("bloqueado_ate"), r.DataHora("criado_em"));

    public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM usuarios WHERE id_usuario = @id", Mapear, ct, P("@id", id));

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM usuarios WHERE email = @email", Mapear, ct, P("@email", email));

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM usuarios ORDER BY nome", Mapear, ct);

    public async Task<bool> EmailExisteAsync(string email, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM usuarios WHERE email = @email AND id_usuario <> @ignorar", ct,
            P("@email", email), P("@ignorar", ignorarId ?? 0)) > 0;

    public async Task<int> ContarAsync(CancellationToken ct = default) =>
        (int)await db.EscalarAsync<long>("SELECT COUNT(*) FROM usuarios", ct);

    public Task<int> AdicionarAsync(Usuario u, CancellationToken ct = default) =>
        db.InserirAsync(@"INSERT INTO usuarios (nome, email, senha_hash, perfil, ativo, tentativas_falhas, bloqueado_ate, criado_em)
                          VALUES (@nome, @email, @hash, @perfil, @ativo, @tent, @bloq, @criado)", ct,
            P("@nome", u.Nome), P("@email", u.Email), P("@hash", u.SenhaHash), P("@perfil", u.Perfil), P("@ativo", u.Ativo),
            P("@tent", u.TentativasFalhas), P("@bloq", u.BloqueadoAte), P("@criado", u.CriadoEm));

    public Task AtualizarAsync(Usuario u, CancellationToken ct = default) =>
        db.ExecutarAsync(@"UPDATE usuarios SET nome=@nome, senha_hash=@hash, perfil=@perfil, ativo=@ativo,
                           tentativas_falhas=@tent, bloqueado_ate=@bloq WHERE id_usuario=@id", ct,
            P("@nome", u.Nome), P("@hash", u.SenhaHash), P("@perfil", u.Perfil), P("@ativo", u.Ativo),
            P("@tent", u.TentativasFalhas), P("@bloq", u.BloqueadoAte), P("@id", u.Id));

    public Task RemoverAsync(int id, CancellationToken ct = default) =>
        db.ExecutarAsync("DELETE FROM usuarios WHERE id_usuario = @id", ct, P("@id", id));
}

public class PacienteRepository(DbSession db) : IPacienteRepository
{
    private static Paciente Mapear(MySqlDataReader r) => new(r.Int("id_paciente"), r.Str("nome"), r.Str("cpf"), r.Data("data_nascimento"),
        r.StrN("telefone"), r.StrN("endereco"), r.StrN("alergias"), r.StrN("historico_clinico"), r.StrN("tipo_sanguineo"));

    public Task<Paciente?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM pacientes WHERE id_paciente = @id", Mapear, ct, P("@id", id));

    /// <summary>Busca por nome (parcial) ou CPF (com ou sem pontuação).</summary>
    public async Task<IReadOnlyList<Paciente>> ListarAsync(string? busca, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(busca))
        {
            return await db.ListarAsync("SELECT * FROM pacientes ORDER BY nome", Mapear, ct);
        }

        var digitos = new string(busca.Where(char.IsDigit).ToArray());
        return await db.ListarAsync(@"SELECT * FROM pacientes
                                      WHERE nome LIKE @nome OR (@cpf <> '' AND REPLACE(REPLACE(cpf,'.',''),'-','') LIKE @cpfLike)
                                      ORDER BY nome", Mapear, ct,
            P("@nome", $"%{busca.Trim()}%"), P("@cpf", digitos), P("@cpfLike", $"%{digitos}%"));
    }

    /// <summary>Compara só os dígitos, pois registros antigos do PHP podem ter o CPF com pontuação.</summary>
    public async Task<bool> CpfExisteAsync(string cpf, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>(@"SELECT COUNT(*) FROM pacientes
                                      WHERE REPLACE(REPLACE(cpf,'.',''),'-','') = @cpf AND id_paciente <> @ignorar", ct,
            P("@cpf", cpf), P("@ignorar", ignorarId ?? 0)) > 0;

    public async Task<bool> PossuiHistoricoAsync(int id, CancellationToken ct = default) =>
        await db.EscalarAsync<long>(@"SELECT (SELECT COUNT(*) FROM consultas WHERE id_paciente=@id)
                                           + (SELECT COUNT(*) FROM internacoes WHERE id_paciente=@id)
                                           + (SELECT COUNT(*) FROM faturamento WHERE id_paciente=@id)
                                           + (SELECT COUNT(*) FROM triagem WHERE id_paciente=@id)", ct, P("@id", id)) > 0;

    public Task<int> AdicionarAsync(Paciente p, CancellationToken ct = default) =>
        db.InserirAsync(@"INSERT INTO pacientes (nome, cpf, data_nascimento, telefone, endereco, alergias, historico_clinico, tipo_sanguineo)
                          VALUES (@nome, @cpf, @nasc, @tel, @end, @alerg, @hist, @tipo)", ct, Parametros(p));

    public Task AtualizarAsync(Paciente p, CancellationToken ct = default) =>
        db.ExecutarAsync(@"UPDATE pacientes SET nome=@nome, cpf=@cpf, data_nascimento=@nasc, telefone=@tel, endereco=@end,
                           alergias=@alerg, historico_clinico=@hist, tipo_sanguineo=@tipo WHERE id_paciente=@id", ct,
            Parametros(p).Append(P("@id", p.Id)).ToArray());

    public Task RemoverAsync(int id, CancellationToken ct = default) =>
        db.ExecutarAsync("DELETE FROM pacientes WHERE id_paciente = @id", ct, P("@id", id));

    private static object?[] Parametros(Paciente p) => new object?[]
    {
        P("@nome", p.Nome), P("@cpf", p.Cpf), P("@nasc", p.DataNascimento), P("@tel", p.Telefone), P("@end", p.Endereco),
        P("@alerg", p.Alergias), P("@hist", p.HistoricoClinico), P("@tipo", p.TipoSanguineo)
    };
}

public class MedicoRepository(DbSession db) : IMedicoRepository
{
    private static Medico Mapear(MySqlDataReader r) =>
        new(r.Int("id_medico"), r.Str("nome"), r.Str("crm"), r.Str("especialidade"), r.StrN("telefone"), r.StrN("email"));

    public Task<Medico?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM medicos WHERE id_medico=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<Medico>> ListarAsync(CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM medicos ORDER BY nome", Mapear, ct);

    public async Task<bool> CrmExisteAsync(string crm, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM medicos WHERE crm=@crm AND id_medico<>@ign", ct, P("@crm", crm), P("@ign", ignorarId ?? 0)) > 0;

    public async Task<bool> PossuiConsultasAsync(int id, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM consultas WHERE id_medico=@id", ct, P("@id", id)) > 0;

    public Task<int> AdicionarAsync(Medico m, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO medicos (nome, crm, especialidade, telefone, email) VALUES (@nome,@crm,@esp,@tel,@email)", ct,
            P("@nome", m.Nome), P("@crm", m.Crm), P("@esp", m.Especialidade), P("@tel", m.Telefone), P("@email", m.Email));

    public Task AtualizarAsync(Medico m, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE medicos SET nome=@nome, crm=@crm, especialidade=@esp, telefone=@tel, email=@email WHERE id_medico=@id", ct,
            P("@nome", m.Nome), P("@crm", m.Crm), P("@esp", m.Especialidade), P("@tel", m.Telefone), P("@email", m.Email), P("@id", m.Id));

    public Task RemoverAsync(int id, CancellationToken ct = default) =>
        db.ExecutarAsync("DELETE FROM medicos WHERE id_medico=@id", ct, P("@id", id));
}

public class ConvenioRepository(DbSession db) : IConvenioRepository
{
    private static Convenio Mapear(MySqlDataReader r) =>
        new(r.Int("id_convenio"), r.Str("nome_convenio"), r.DataN("validade_carteira"), r.StrN("telefone"));

    public Task<Convenio?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM convenios WHERE id_convenio=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<Convenio>> ListarAsync(CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM convenios ORDER BY nome_convenio", Mapear, ct);

    public async Task<bool> NomeExisteAsync(string nome, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM convenios WHERE nome_convenio=@n AND id_convenio<>@ign", ct, P("@n", nome), P("@ign", ignorarId ?? 0)) > 0;

    public async Task<bool> PossuiConsultasAsync(int id, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM consultas WHERE id_convenio=@id", ct, P("@id", id)) > 0;

    public Task<int> AdicionarAsync(Convenio c, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO convenios (nome_convenio, validade_carteira, telefone) VALUES (@n,@v,@t)", ct,
            P("@n", c.Nome), P("@v", c.ValidadeCarteira), P("@t", c.Telefone));

    public Task AtualizarAsync(Convenio c, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE convenios SET nome_convenio=@n, validade_carteira=@v, telefone=@t WHERE id_convenio=@id", ct,
            P("@n", c.Nome), P("@v", c.ValidadeCarteira), P("@t", c.Telefone), P("@id", c.Id));

    public Task RemoverAsync(int id, CancellationToken ct = default) =>
        db.ExecutarAsync("DELETE FROM convenios WHERE id_convenio=@id", ct, P("@id", id));
}

public class SalaRepository(DbSession db) : ISalaRepository
{
    private static Sala Mapear(MySqlDataReader r) => new(r.Int("id_sala"), r.Str("numero_sala"), r.Str("tipo_sala"), r.Str("status_sala"));

    public Task<Sala?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM salas WHERE id_sala=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<Sala>> ListarAsync(CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM salas ORDER BY numero_sala", Mapear, ct);

    public async Task<bool> NumeroExisteAsync(string numero, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM salas WHERE numero_sala=@n AND id_sala<>@ign", ct, P("@n", numero), P("@ign", ignorarId ?? 0)) > 0;

    public async Task<bool> PossuiConsultasAsync(int id, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM consultas WHERE id_sala=@id", ct, P("@id", id)) > 0;

    public Task<int> AdicionarAsync(Sala s, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO salas (numero_sala, tipo_sala, status_sala) VALUES (@n,@t,@s)", ct,
            P("@n", s.Numero), P("@t", s.Tipo), P("@s", s.Status));

    public Task AtualizarAsync(Sala s, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE salas SET numero_sala=@n, tipo_sala=@t, status_sala=@s WHERE id_sala=@id", ct,
            P("@n", s.Numero), P("@t", s.Tipo), P("@s", s.Status), P("@id", s.Id));

    public Task RemoverAsync(int id, CancellationToken ct = default) =>
        db.ExecutarAsync("DELETE FROM salas WHERE id_sala=@id", ct, P("@id", id));
}

public class MedicamentoRepository(DbSession db) : IMedicamentoRepository
{
    internal static Medicamento Mapear(MySqlDataReader r) =>
        new(r.Int("id_medicamento"), r.Str("nome_medicamento"), r.StrN("contraindicacoes"), r.StrN("interacoes_medicamentosas"));

    public Task<Medicamento?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.PrimeiroAsync("SELECT * FROM medicamentos WHERE id_medicamento=@id", Mapear, ct, P("@id", id));

    public async Task<IReadOnlyList<Medicamento>> ListarAsync(CancellationToken ct = default) =>
        await db.ListarAsync("SELECT * FROM medicamentos ORDER BY nome_medicamento", Mapear, ct);

    public async Task<bool> NomeExisteAsync(string nome, int? ignorarId = null, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM medicamentos WHERE nome_medicamento=@n AND id_medicamento<>@ign", ct,
            P("@n", nome), P("@ign", ignorarId ?? 0)) > 0;

    public async Task<bool> PossuiPrescricoesAsync(int id, CancellationToken ct = default) =>
        await db.EscalarAsync<long>("SELECT COUNT(*) FROM prescricoes WHERE id_medicamento=@id", ct, P("@id", id)) > 0;

    public Task<int> AdicionarAsync(Medicamento m, CancellationToken ct = default) =>
        db.InserirAsync("INSERT INTO medicamentos (nome_medicamento, contraindicacoes, interacoes_medicamentosas) VALUES (@n,@c,@i)", ct,
            P("@n", m.Nome), P("@c", m.Contraindicacoes), P("@i", m.InteracoesMedicamentosas));

    public Task AtualizarAsync(Medicamento m, CancellationToken ct = default) =>
        db.ExecutarAsync("UPDATE medicamentos SET nome_medicamento=@n, contraindicacoes=@c, interacoes_medicamentosas=@i WHERE id_medicamento=@id", ct,
            P("@n", m.Nome), P("@c", m.Contraindicacoes), P("@i", m.InteracoesMedicamentosas), P("@id", m.Id));

    public Task RemoverAsync(int id, CancellationToken ct = default) =>
        db.ExecutarAsync("DELETE FROM medicamentos WHERE id_medicamento=@id", ct, P("@id", id));
}
