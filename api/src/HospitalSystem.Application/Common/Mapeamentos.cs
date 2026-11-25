using HospitalSystem.Application.DTOs;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.ValueObjects;

namespace HospitalSystem.Application.Common;

/// <summary>Conversão entidade → DTO (o JSON nunca expõe a entidade de domínio diretamente).</summary>
public static class Mapeamentos
{
    public static UsuarioDto ToDto(this Usuario u, DateTime agora) =>
        new(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo, u.CriadoEm, u.EstaBloqueado(agora));

    public static PacienteDto ToDto(this Paciente p, DateOnly hoje) =>
        new(p.Id, p.Nome, p.Cpf, Cpf.Formatar(p.Cpf), p.DataNascimento, p.Idade(hoje), p.Telefone, p.Endereco,
            p.Alergias, p.HistoricoClinico, p.TipoSanguineo);

    public static MedicoDto ToDto(this Medico m) => new(m.Id, m.Nome, m.Crm, m.Especialidade, m.Telefone, m.Email);

    public static ConvenioDto ToDto(this Convenio c, DateOnly hoje) =>
        new(c.Id, c.Nome, c.ValidadeCarteira, c.Telefone, !c.ValidoEm(hoje));

    public static SalaDto ToDto(this Sala s, int? idLeitoCriado = null) => new(s.Id, s.Numero, s.Tipo, s.Status, idLeitoCriado);

    public static MedicamentoDto ToDto(this Medicamento m) => new(m.Id, m.Nome, m.Contraindicacoes, m.InteracoesMedicamentosas);

    public static LeitoDto ToDto(this Leito l) => new(l.Id, l.Numero, l.Ala, l.Status, l.TempoHigienizacao);

    public static InsumoDto ToDto(this Insumo i) => new(i.Id, i.Nome, i.QuantidadeEstoque, i.ValorUnitario, i.EstoqueCritico);

    public static ExameDto ToDto(this Exame e) => new(e.Id, e.IdProntuario, e.Nome, e.Resultado, e.DataExame);

    public static TriagemDto ToDto(this Triagem t) =>
        new(t.Id, t.IdConsulta, t.IdPaciente, t.Temperatura, t.PressaoArterial, t.FrequenciaCardiaca, t.SaturacaoOxigenio,
            t.EscalaDor, t.ClassificacaoRisco, t.ClassificacaoCalculada, t.QueixaPrincipal, t.DataTriagem);
}

public static class TimeProviderExtensions
{
    /// <summary>Data/hora local do servidor (o banco grava DATETIME sem fuso).</summary>
    public static DateTime Agora(this TimeProvider tp) => tp.GetLocalNow().DateTime;
    public static DateOnly Hoje(this TimeProvider tp) => DateOnly.FromDateTime(tp.GetLocalNow().DateTime);
}
