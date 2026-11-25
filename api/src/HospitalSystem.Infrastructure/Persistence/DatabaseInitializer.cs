using System.Reflection;
using HospitalSystem.Application.Abstractions;
using HospitalSystem.Domain.Constants;
using HospitalSystem.Domain.Entities;
using HospitalSystem.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace HospitalSystem.Infrastructure.Persistence;

/// <summary>
/// Prepara o banco na inicialização da API:
///  1. cria o banco se não existir;
///  2. cria as tabelas (schema.sql, idempotente);
///  3. adiciona as colunas novas em bancos criados pelo sistema PHP (migração);
///  4. cria o administrador inicial (RN-USU-12) e, opcionalmente, dados de demonstração.
/// </summary>
public sealed class DatabaseInitializer(
    IConfiguration config,
    DbSession db,
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    TimeProvider relogio,
    ILogger<DatabaseInitializer> logger)
{
    /// <summary>Colunas acrescentadas na versão da API (tabela, coluna, definição).</summary>
    private static readonly (string Tabela, string Coluna, string Definicao)[] ColunasNovas =
    {
        ("triagem", "classificacao_calculada", "VARCHAR(20) NULL"),
        ("triagem", "queixa_principal", "TEXT NULL"),
        ("triagem", "data_triagem", "DATETIME NULL"),
        ("triagem", "id_consulta", "INT NULL"),
        ("prontuario", "profissional", "VARCHAR(150) NULL"),
        ("prescricoes", "data_prescricao", "DATETIME NULL"),
        ("prescricoes", "alerta_confirmado", "TEXT NULL"),
        ("faturamento", "data_pagamento", "DATE NULL"),
        ("uso_insumos", "id_internacao", "INT NULL"),
    };

    public async Task InicializarAsync(CancellationToken ct = default)
    {
        var cs = config.GetConnectionString("HospitalDb")!;
        await CriarBancoSeNecessarioAsync(cs, ct);

        await db.ExecutarAsync(LerScript("schema.sql"), ct);
        await AplicarMigracoesAsync(ct);
        await CriarAdministradorAsync(ct);

        if (config.GetValue<bool>("Database:CriarDadosDemo"))
        {
            await CriarDadosDemoAsync(ct);
        }
        logger.LogInformation("Banco de dados pronto.");
    }

    private async Task CriarBancoSeNecessarioAsync(string cs, CancellationToken ct)
    {
        var builder = new MySqlConnectionStringBuilder(cs);
        var nomeBanco = builder.Database;
        builder.Database = string.Empty;
        try
        {
            await using var conn = new MySqlConnection(builder.ConnectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{nomeBanco.Replace("`", "")}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (MySqlException ex)
        {
            logger.LogWarning("Não foi possível criar o banco automaticamente ({Mensagem}). Supondo que já exista.", ex.Message);
        }
    }

    private async Task AplicarMigracoesAsync(CancellationToken ct)
    {
        foreach (var (tabela, coluna, definicao) in ColunasNovas)
        {
            var existe = await db.EscalarAsync<long>(@"SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t AND COLUMN_NAME = @c", ct,
                DbSession.P("@t", tabela), DbSession.P("@c", coluna));
            if (existe == 0)
            {
                await db.ExecutarAsync($"ALTER TABLE {tabela} ADD COLUMN {coluna} {definicao}", ct);
                logger.LogInformation("Migração: coluna {Tabela}.{Coluna} adicionada.", tabela, coluna);
            }
        }
    }

    private async Task CriarAdministradorAsync(CancellationToken ct)
    {
        if (await usuarios.ContarAsync(ct) > 0)
        {
            return;
        }

        var email = config["Seed:AdminEmail"];
        var senha = config["Seed:AdminSenha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning("Nenhum usuário cadastrado e Seed:AdminEmail/Seed:AdminSenha não configurados. " +
                              "Configure-os para criar o administrador inicial.");
            return;
        }

        Usuario.ValidarForcaSenha(senha);
        var admin = Usuario.Criar(config["Seed:AdminNome"] ?? "Administrador", email, hasher.Hash(senha), Perfis.Administrador, relogio.GetLocalNow().DateTime);
        await usuarios.AdicionarAsync(admin, ct);
        logger.LogInformation("Administrador inicial criado: {Email}", admin.Email);

        var senhaDemo = config["Seed:SenhaUsuariosDemo"];
        if (config.GetValue<bool>("Database:CriarDadosDemo") && !string.IsNullOrWhiteSpace(senhaDemo))
        {
            Usuario.ValidarForcaSenha(senhaDemo);
            var dominio = email.Split('@')[1];
            foreach (var (nome, perfil, login) in new[]
                     {
                         ("Dra. Helena Souza", Perfis.Medico, "medico"),
                         ("Enf. Carlos Lima", Perfis.Enfermagem, "enfermagem"),
                         ("Ana Recepção", Perfis.Recepcao, "recepcao")
                     })
            {
                await usuarios.AdicionarAsync(Usuario.Criar(nome, $"{login}@{dominio}", hasher.Hash(senhaDemo), perfil, relogio.GetLocalNow().DateTime), ct);
            }
            logger.LogInformation("Usuários de demonstração criados (medico@, enfermagem@, recepcao@{Dominio}).", dominio);
        }
    }

    private async Task CriarDadosDemoAsync(CancellationToken ct)
    {
        if (await db.EscalarAsync<long>("SELECT COUNT(*) FROM medicos", ct) > 0)
        {
            return; // já existe base: não mexe em dados reais
        }

        await db.ExecutarAsync(LerScript("dados_demo.sql"), ct);
        logger.LogInformation("Dados de demonstração inseridos.");
    }

    private static string LerScript(string nome)
    {
        var asm = Assembly.GetExecutingAssembly();
        var recurso = asm.GetManifestResourceNames().Single(n => n.EndsWith(nome, StringComparison.Ordinal));
        using var stream = asm.GetManifestResourceStream(recurso)!;
        using var leitor = new StreamReader(stream);
        return leitor.ReadToEnd();
    }
}
