namespace HospitalSystem.Domain.Entities;

/// <summary>Triagem criada exclusivamente por <see cref="Consulta.RegistrarTriagem"/>.</summary>
public class Triagem
{
    public int Id { get; private set; }
    public int? IdConsulta { get; private set; }
    public int IdPaciente { get; private set; }
    public decimal Temperatura { get; private set; }
    public string PressaoArterial { get; private set; }
    public int FrequenciaCardiaca { get; private set; }
    public int SaturacaoOxigenio { get; private set; }
    public int EscalaDor { get; private set; }
    /// <summary>Classificação final (pode ter sido elevada pelo profissional).</summary>
    public string ClassificacaoRisco { get; private set; }
    /// <summary>Classificação calculada pelos sinais vitais.</summary>
    public string ClassificacaoCalculada { get; private set; }
    public string Protocolo { get; private set; }
    public string? QueixaPrincipal { get; private set; }
    public DateTime DataTriagem { get; private set; }

    public Triagem(int id, int? idConsulta, int idPaciente, decimal temperatura, string pressaoArterial, int frequenciaCardiaca,
        int saturacaoOxigenio, int escalaDor, string classificacaoRisco, string classificacaoCalculada, string protocolo,
        string? queixaPrincipal, DateTime dataTriagem)
    {
        Id = id; IdConsulta = idConsulta; IdPaciente = idPaciente; Temperatura = temperatura; PressaoArterial = pressaoArterial;
        FrequenciaCardiaca = frequenciaCardiaca; SaturacaoOxigenio = saturacaoOxigenio; EscalaDor = escalaDor;
        ClassificacaoRisco = classificacaoRisco; ClassificacaoCalculada = classificacaoCalculada; Protocolo = protocolo;
        QueixaPrincipal = queixaPrincipal; DataTriagem = dataTriagem;
    }

    public void DefinirId(int id) => Id = id;
}
