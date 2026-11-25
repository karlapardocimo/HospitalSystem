# Decisões Arquiteturais

Registro curto das principais decisões do projeto e do motivo de cada uma.
Útil para responder às perguntas da apresentação.

## 1. Clean Architecture em 4 projetos

```
HospitalSystem.Api ──────────────► HospitalSystem.Application ──► HospitalSystem.Domain
        │                                   ▲
        └──► HospitalSystem.Infrastructure ─┘ (implementa as interfaces do Domain e da Application)
```

| Camada | Contém | Depende de |
|---|---|---|
| **Domain** | Entidades com as regras (`Paciente.Criar`, `Consulta.RegistrarTriagem`, `Leito.Ocupar`...), value objects (`Cpf`, `PressaoArterial`), serviços de domínio (`ClassificadorManchester`, `AnalisadorPrescricao`), interfaces dos repositórios | nada |
| **Application** | Casos de uso (`*Service`), DTOs, `ResultWrapper`, chaves/tempos de cache, interfaces de infraestrutura (`ICacheService`, `ITokenService`, `IPasswordHasher`) | Domain |
| **Infrastructure** | MySQL (ADO.NET + MySqlConnector), repositórios, consultas de leitura, PBKDF2, geração do JWT, `IMemoryCache`, inicialização do banco | Application, Domain |
| **Api** | Controllers, `Program.cs`, validação do JWT, middlewares de erro e de cache, Swagger | Application, Infrastructure |

**Por quê:** o domínio não conhece banco, HTTP nem bibliotecas externas. Dá para trocar MySQL por
outro banco ou `IMemoryCache` por Redis mexendo só na Infrastructure. O `.csproj` do Domain não tem
nenhuma referência, o que comprova a regra de dependência.

Controllers não têm regra de negócio: recebem a requisição, chamam um serviço e convertem o
`ResultWrapper` em status HTTP (`ApiControllerBase.Responder`).

## 2. Regras de negócio dentro das entidades

As invariantes ficam nos métodos das entidades (`Criar`, `Atualizar`, `Cancelar`, `Ocupar`...).
As propriedades têm `private set`, então não é possível colocar uma entidade em estado inválido
sem passar pela regra. Exemplos:

- `Leito.AlterarStatusManualmente` impede "Ocupado" manual (RN-LEI-03);
- `Consulta` tem uma tabela de transições de status permitidas (RN-CNS-07);
- `Faturamento.Emitir` decide sozinho se a guia vai para auditoria (RN-FAT-04).

Regras que precisam consultar o banco (unicidade de CPF, conflito de horário, internação ativa)
ficam nos serviços da Application, porque a entidade não acessa repositório.

Cada violação gera uma mensagem com o código da regra (`RN-PAC-02: CPF inválido.`), documentado em
[REGRAS_DE_NEGOCIO.md](REGRAS_DE_NEGOCIO.md).

## 3. ADO.NET + MySqlConnector (sem Entity Framework)

**Por quê:** o banco já existia (sistema PHP) com nomes de tabelas/colunas próprios e dados em
formato antigo. Com SQL explícito o mapeamento fica visível, o schema original foi mantido e a
migração é só "acrescentar colunas". O SQL é parecido com o PDO usado no PHP, então a equipe
reconhece o que está acontecendo. Todas as consultas usam parâmetros (`@nome`), sem concatenação
de valores, o que evita SQL Injection.

## 4. Repositórios (escrita) separados de Queries (leitura)

- **Repositórios** (`IPacienteRepository`...) ficam no Domain e trabalham com **entidades**.
- **Queries** (`IConsultaQueries`, `IDashboardQueries`...) ficam na Application e devolvem **DTOs**
  prontos para a tela, com `JOIN` entre várias tabelas.

**Por quê:** as listagens precisam de dados de várias tabelas (nome do paciente, do médico...) que
não pertencem a uma única entidade. Montar isso com entidades geraria muitas consultas.

## 5. Unit of Work (transações)

`DbSession` é registrado como *Scoped*: uma conexão por requisição, compartilhada por todos os
repositórios. `IUnitOfWork.ExecutarEmTransacaoAsync` envolve operações que precisam acontecer juntas:

- internar paciente + ocupar leito (RN-INT-03);
- transferir (internação + 2 leitos);
- cadastrar paciente + colocar na fila de triagem;
- triagem + mudança de status da consulta;
- emitir guia + registro de auditoria.

Se qualquer passo falhar, nada é gravado (rollback).

## 6. ResultWrapper único + tratamento global de erros

Todas as respostas, inclusive 400, 401, 403, 404 e 500, têm o formato:

```json
{ "success": bool, "message": "texto", "data": { }, "errors": [ "..." ] }
```

| Situação | Quem gera | Status |
|---|---|---|
| Sucesso | serviço → `ResultWrapper.Ok` | 200 / 201 |
| JSON mal formado | `InvalidModelStateResponseFactory` | 400 |
| Sem token / token inválido / expirado | `JwtBearerEvents.OnChallenge` | 401 |
| Perfil sem permissão | `JwtBearerEvents.OnForbidden` | 403 |
| Registro inexistente | serviço → `ResultWrapper.NotFound` | 404 |
| Duplicidade (CPF, CRM, e-mail) | serviço → `ResultWrapper.Conflict` | 409 |
| Regra de negócio violada | entidade lança `BusinessRuleException` → `ExceptionMiddleware` | 422 |
| Rota inexistente, método não permitido, conteúdo não JSON | `UseStatusCodePages` no `Program.cs` | 404 / 405 / 415 |
| Erro inesperado | `ExceptionMiddleware` (detalhe só no log) | 500 |

A propriedade `ErrorType` do wrapper tem `[JsonIgnore]`: serve só para a API escolher o status HTTP.

## 7. Autenticação JWT (HS256) e senha com PBKDF2

- `POST /api/auth/login` confere a senha com `Pbkdf2PasswordHasher` (100.000 iterações, salt de 16
  bytes, comparação em tempo constante) e gera o token em `JwtTokenService`.
- O token leva `sub`, `name`, `email`, `role` e `jti`, expira em 60 min e é assinado com HMAC-SHA256.
- A API valida emissor, audiência, assinatura e validade em toda requisição (`AddJwtBearer`).
- **Seguro por padrão:** `FallbackPolicy = RequireAuthenticatedUser`. Todo endpoint exige token, a
  menos que tenha `[AllowAnonymous]` (só `login` e `status`).
- Perfis com `[Authorize(Roles = ...)]`, conforme a matriz em REGRAS_DE_NEGOCIO.md.
- Bloqueio após 5 tentativas (RN-USU-08) contra força bruta.

**Por que PBKDF2 e não SHA-256 simples:** hash rápido permite testar bilhões de senhas por segundo.
PBKDF2 com muitas iterações deixa cada tentativa cara; o salt impede tabelas pré-calculadas.
Já vem no .NET (`Rfc2898DeriveBytes.Pbkdf2`), sem dependência externa.

## 8. Cache em memória (IMemoryCache) com cache-aside

| Dado | Chave | Expiração | Invalidado quando |
|---|---|---|---|
| Médicos | `catalogo:medicos` | 10 min | criar/editar/excluir médico |
| Convênios | `catalogo:convenios` | 10 min | criar/editar/excluir convênio |
| Salas | `catalogo:salas` | 10 min | criar/editar/excluir sala |
| Medicamentos | `catalogo:medicamentos` | 10 min | criar/editar/excluir medicamento |
| Mapa de leitos | `leitos:{status}` | 1 min | qualquer mudança em leito ou internação (por prefixo `leitos:`) |
| Dashboard | `dashboard` | 30 s | paciente, consulta, triagem, leito ou internação alterados |

**Por quê esses dados:** são lidos o tempo todo (combos de agendamento, página inicial) e mudam pouco.
Dados que mudam a cada segundo e precisam estar exatos (fila de triagem, estoque, faturamento) **não**
usam cache.

**Expiração absoluta + invalidação na escrita:** a expiração garante que, mesmo que algo seja alterado
fora da API (direto no banco), o dado se corrige sozinho. A invalidação garante que quem altera pela
API vê o dado novo na hora.

**Como observar:** logs `[CACHE MISS]`, `[CACHE HIT]`, `[CACHE SET]`, `[CACHE INVALIDADO]`,
`[CACHE EXPIROU]` no console e o cabeçalho HTTP `X-Cache: HIT|MISS` em cada resposta.

**Limitações conhecidas:** cache em memória vale para uma instância da API. Com várias instâncias
seria preciso um cache distribuído (Redis), trocando só `MemoryCacheService` por outra implementação
de `ICacheService`.

## 9. Configurações sensíveis fora do código

`appsettings.json` não tem senha nem chave (campos vazios). Os valores vêm de:

- **local:** `dotnet user-secrets` (gravado fora da pasta do projeto);
- **Docker:** variáveis de ambiente lidas do arquivo `.env` (que está no `.gitignore`).

A API recusa iniciar se `Jwt:Key` tiver menos de 32 caracteres.

## 10. Cliente PHP: token só no servidor

O aplicativo PHP chama a API pelo servidor (cURL), não pelo navegador. O JWT fica na sessão PHP
(`$_SESSION['token']`) e nunca chega ao JavaScript do navegador, o que protege contra roubo de
token por XSS. Toda resposta 401 com token na sessão encerra a sessão e volta para o login.

O aplicativo não tem mais acesso ao banco: `models/` e `config/Database.php` foram removidos.

## 11. `TimeProvider` em vez de `DateTime.Now` espalhado

As regras que dependem de horário (consulta no passado, bloqueio de login, expiração) recebem a data
de fora (`agora`, `hoje`). Isso deixa o domínio determinístico e testável.
