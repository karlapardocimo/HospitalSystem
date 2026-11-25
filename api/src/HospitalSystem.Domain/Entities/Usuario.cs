using HospitalSystem.Domain.Common;
using HospitalSystem.Domain.Constants;

namespace HospitalSystem.Domain.Entities;

public class Usuario
{
    public const int MaximoTentativas = 5;                              // RN-USU-08
    public static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public string Perfil { get; private set; } = Perfis.Recepcao;
    public bool Ativo { get; private set; }
    public int TentativasFalhas { get; private set; }
    public DateTime? BloqueadoAte { get; private set; }
    public DateTime CriadoEm { get; private set; }

    /// <summary>Reconstrói o usuário a partir do banco (sem validação).</summary>
    public Usuario(int id, string nome, string email, string senhaHash, string perfil, bool ativo,
        int tentativasFalhas, DateTime? bloqueadoAte, DateTime criadoEm)
    {
        Id = id; Nome = nome; Email = email; SenhaHash = senhaHash; Perfil = perfil; Ativo = ativo;
        TentativasFalhas = tentativasFalhas; BloqueadoAte = bloqueadoAte; CriadoEm = criadoEm;
    }

    public static Usuario Criar(string nome, string email, string senhaHash, string perfil, DateTime agora)
    {
        Validar(nome, email, perfil);
        return new Usuario(0, nome.Trim(), email.Trim().ToLowerInvariant(), senhaHash, perfil, true, 0, null, agora);
    }

    public void Atualizar(string nome, string perfil, bool ativo)
    {
        Validar(nome, Email, perfil);
        Nome = nome.Trim();
        Perfil = perfil;
        Ativo = ativo;
        if (ativo)
        {
            DesbloquearConta();
        }
    }

    public void AlterarSenha(string novoHash)
    {
        SenhaHash = novoHash;
        DesbloquearConta();
    }

    public void DefinirId(int id) => Id = id;

    /// <summary>RN-USU-03: força mínima da senha (verificada antes de gerar o hash).</summary>
    public static void ValidarForcaSenha(string? senha)
    {
        var ok = !string.IsNullOrEmpty(senha) && senha.Length >= 8 && senha.Any(char.IsLetter) && senha.Any(char.IsDigit);
        Validacao.Garantir(ok, "RN-USU-03", "A senha deve ter no mínimo 8 caracteres, com pelo menos uma letra e um número.");
    }

    public bool EstaBloqueado(DateTime agora) => BloqueadoAte.HasValue && BloqueadoAte.Value > agora;

    /// <summary>RN-USU-08: a cada falha incrementa o contador; na 5ª bloqueia por 15 minutos.</summary>
    public void RegistrarFalhaDeLogin(DateTime agora)
    {
        TentativasFalhas++;
        if (TentativasFalhas >= MaximoTentativas)
        {
            BloqueadoAte = agora.Add(TempoBloqueio);
            TentativasFalhas = 0;
        }
    }

    public void RegistrarLoginComSucesso() => DesbloquearConta();

    private void DesbloquearConta()
    {
        TentativasFalhas = 0;
        BloqueadoAte = null;
    }

    private static void Validar(string? nome, string? email, string? perfil)
    {
        new Validacao()
            .Se(string.IsNullOrWhiteSpace(nome) || nome.Trim().Length < 3 || nome.Trim().Length > 100,
                "RN-USU-01", "Nome é obrigatório e deve ter entre 3 e 100 caracteres.")
            .Se(!Texto.EmailValido(email), "RN-USU-02", "E-mail inválido.")
            .Se(perfil is null || !Perfis.Todos.Contains(perfil),
                "RN-USU-05", $"Perfil inválido. Use: {string.Join(", ", Perfis.Todos)}.")
            .LancarSeInvalido();
    }
}
