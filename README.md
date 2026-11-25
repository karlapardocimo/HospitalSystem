# Manus Hospital — API ASP.NET Core + Aplicativo PHP

Sistema de gestão hospitalar desenvolvido na disciplina de **Engenharia de Software**.

A versão anterior era um aplicativo PHP que acessava o banco MySQL diretamente. Nesta etapa o sistema foi
evoluído para uma arquitetura **cliente/servidor**:

- uma **API REST em C# / ASP.NET Core 10** concentra as regras de negócio, o acesso aos dados, a autenticação JWT e o cache;
- o **aplicativo PHP** passou a ser apenas um cliente dessa API.

```
Aplicativo PHP  ──HTTP + JWT──►  API ASP.NET Core  ──ADO.NET──►  MariaDB
   (web/)                           (api/)                      (database/)
```

## Integrantes da equipe

- Karla Pardócimo
- Amanda Garcia
- Gabriel Pimentel

---

## Sumário

1. [Funcionalidades](#1-funcionalidades)
2. [Tecnologias](#2-tecnologias)
3. [Arquitetura](#3-arquitetura)
4. [Como executar](#4-como-executar)
5. [Banco de dados](#5-banco-de-dados)
6. [Autenticação JWT](#6-autenticação-jwt)
7. [Estratégia de cache](#7-estratégia-de-cache)
8. [Padrão de resposta (ResultWrapper)](#8-padrão-de-resposta-resultwrapper)
9. [Principais endpoints](#9-principais-endpoints)
10. [Exemplos de requisições e respostas](#10-exemplos-de-requisições-e-respostas)
11. [Reprodução do projeto](#11-reprodução-do-projeto)
12. [Documentação complementar](#12-documentação-complementar)

---

## 1. Funcionalidades

| Módulo | O que faz |
|---|---|
| Pacientes | Cadastro com validação de CPF e registro de alergias |
| Consultas | Agendamento, check-in, cancelamento e falta |
| Triagem | Protocolo de Manchester: a API calcula a cor de risco pelos sinais vitais e ordena a fila |
| Prontuário | Evolução clínica, prescrições com bloqueio por alergia e alerta de interação, exames |
| Leitos e internações | Mapa de leitos, internação, transferência e alta |
| Estoque | Entradas, saídas, estoque crítico e uso em paciente internado |
| Faturamento | Consolidação de gastos, emissão de guias, auditoria, pagamento e cancelamento |
| Usuários | Perfis Administrador, Médico, Enfermagem e Recepção, cada um com permissões próprias |

Cada regra de negócio tem um código (`RN-PAC-02`, `RN-TRI-05`...) que aparece nas mensagens de erro da API.
A lista completa está em [docs/REGRAS_DE_NEGOCIO.md](docs/REGRAS_DE_NEGOCIO.md).

---

## 2. Tecnologias

| Parte | Tecnologia |
|---|---|
| API | C#, **.NET 10**, ASP.NET Core Web API |
| Acesso a dados | ADO.NET + **MySqlConnector 3.0** (SQL parametrizado) |
| Banco | **MariaDB 10.11** (Docker) ou MySQL 8 |
| Autenticação | **JWT** (HMAC-SHA256) |
| Senhas | **PBKDF2-SHA256** (100.000 iterações + salt) |
| Cache | **IMemoryCache** (cache-aside) |
| Documentação da API | Swagger / OpenAPI |
| Aplicativo | PHP 8.3 + cURL, Bootstrap 5 |
| Implantação | **Docker** + Docker Compose |

---

## 3. Arquitetura

### 3.1 Clean Architecture

A API é dividida em quatro projetos. As dependências apontam para dentro: o domínio não conhece banco,
HTTP nem bibliotecas externas.

```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure (implementa as interfaces da Application e do Domain)
```

| Camada | Responsabilidade | Depende de |
|---|---|---|
| **Domain** | Entidades com as regras, value objects (`Cpf`), serviços de domínio, interfaces dos repositórios | nada |
| **Application** | Casos de uso (`*Service`), DTOs, `ResultWrapper`, chaves de cache | Domain |
| **Infrastructure** | Repositórios SQL, Unit of Work, PBKDF2, geração do JWT, cache | Application, Domain |
| **Api** | Controllers, middlewares, `Program.cs` (injeção de dependências, JWT, Swagger) | Application, Infrastructure |

O `HospitalSystem.Domain.csproj` não referencia nenhum outro projeto nem pacote.

### 3.2 Estrutura de pastas

```
HospitalSystem/
├── api/                               API ASP.NET Core
│   ├── HospitalSystem.sln
│   ├── HospitalSystem.Api.http        requisições de exemplo
│   ├── Dockerfile
│   └── src/
│       ├── HospitalSystem.Domain/
│       ├── HospitalSystem.Application/
│       ├── HospitalSystem.Infrastructure/
│       └── HospitalSystem.Api/
├── web/                               aplicativo PHP (cliente da API)
│   ├── lib/ApiClient.php              chamadas HTTP + token
│   └── Dockerfile
├── database/                          scripts SQL
├── docs/                              regras de negócio e decisões arquiteturais
├── docker-compose.yml
└── .env.example
```

### 3.3 Fluxo de uma requisição

Exemplo: `POST /api/internacoes`

1. `InternacoesController` recebe o JSON e chama o serviço (o controller não tem regra de negócio);
2. `InternacaoService` busca paciente e leito e verifica se o paciente já está internado;
3. a entidade `Internacao` aplica as regras: o leito precisa estar disponível e passa a ocupado;
4. o Unit of Work grava internação e leito na mesma transação;
5. o cache do mapa de leitos e do painel é invalidado;
6. o serviço devolve um `ResultWrapper`, convertido em `201 Created`.

---

## 4. Como executar

### 4.1 Com Docker (forma oficial)

Pré-requisito: **Docker Desktop** aberto e com o status *Engine running*.

```bash
copy .env.example .env      # Linux/macOS: cp .env.example .env
docker compose up --build
```

Aguarde a mensagem **"Banco de dados pronto."** no log.

| Serviço | Endereço |
|---|---|
| Aplicativo | http://localhost:8080/login.php |
| API (Swagger) | http://localhost:5080/swagger |
| Banco MariaDB | `localhost:3307` (usuário `hospital`, senha definida no `.env`) |

Na primeira execução a API cria as tabelas, o administrador e os dados de demonstração.

| Comando | Para quê |
|---|---|
| `docker compose up` | subir de novo (sem rebuild) |
| `docker compose logs -f api` | acompanhar os logs da API (cache) |
| `docker compose down` | parar |
| `docker compose down -v` | parar e apagar o banco (recomeçar do zero) |

### 4.2 Usuários de demonstração

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | admin@hospital.local | `Admin@2026` |
| Médico | medico@hospital.local | `Demo@2026` |
| Enfermagem | enfermagem@hospital.local | `Demo@2026` |
| Recepção | recepcao@hospital.local | `Demo@2026` |

As senhas vêm do `.env` (`ADMIN_SENHA` e `SENHA_USUARIOS_DEMO`).

### 4.3 Sem Docker (desenvolvimento local)

<details>
<summary>Clique para ver os passos</summary>

Pré-requisitos: **.NET 10 SDK**, MySQL ou MariaDB (por exemplo, do XAMPP) e PHP 8.

**API** — as configurações sensíveis ficam em *user-secrets*, fora do código:

```bash
cd api/src/HospitalSystem.Api
dotnet user-secrets set "ConnectionStrings:HospitalDb" "Server=localhost;Port=3306;Database=hospital_api;User ID=root;Password=;ConvertZeroDateTime=True"
dotnet user-secrets set "Jwt:Key" "uma-chave-aleatoria-com-pelo-menos-32-caracteres"
dotnet user-secrets set "Seed:AdminSenha" "Admin@2026"
dotnet user-secrets set "Seed:SenhaUsuariosDemo" "Demo@2026"
dotnet run --launch-profile http
```

A API sobe em http://localhost:5080.

**Aplicativo** — em outro terminal:

```bash
cd web
php -S localhost:8000
```

Acesse http://localhost:8000/login.php. O endereço da API fica em `web/config/config.php` (`API_BASE_URL`).

</details>

### 4.4 Problemas comuns

<details>
<summary>Clique para ver</summary>

| Erro | Solução |
|---|---|
| `Cannot connect to the Docker daemon` | Abra o Docker Desktop e espere *Engine running* |
| Porta já em uso | `docker compose down` e depois `docker compose up` |
| Swagger responde 401 | Faça login em `/api/auth/login` e cole o token em **Authorize** |
| `NETSDK1045` (execução local) | Instale o .NET 10 SDK |
| Arquivo bloqueado ao compilar (local) | `taskkill /IM HospitalSystem.Api.exe /F` |

</details>

---

## 5. Banco de dados

| Configuração | Descrição |
|---|---|
| `ConnectionStrings:HospitalDb` | conexão com o banco (`.env` no Docker ou *user-secrets* no local) |
| `Database:InicializarAutomaticamente` | cria o banco e as tabelas ao iniciar |
| `Database:CriarDadosDemo` | insere dados de exemplo se o banco estiver vazio |

Scripts em `database/`:

- `hospital.sql` — schema completo;
- `dados_demo.sql` — dados de demonstração;
- `migracao_php_para_api.sql` — migração de um banco do sistema PHP anterior.

Todo acesso usa **SQL parametrizado** (`@cpf`, `@nome`), o que evita SQL Injection.

Para consultar o banco no Docker:

```bash
docker exec -it hospital-db mariadb -u hospital -p hospital
```

---

## 6. Autenticação JWT

**Como funciona**

1. O cliente envia `POST /api/auth/login` com e-mail e senha (endpoint público).
2. A API confere a senha com o hash **PBKDF2** (a senha nunca é guardada em texto puro).
3. A API gera um **JWT assinado com HMAC-SHA256**, válido por **60 minutos**, com as claims `sub`, `name`, `email`, `role` e `jti`.
4. O cliente envia o token em toda requisição: `Authorization: Bearer <token>`.
5. Sem token ou com token inválido → **401**. Perfil sem permissão → **403**.

**Proteção dos endpoints**

- Todo endpoint exige token por padrão, exceto `/api/auth/login` e `/api/status`;
- autorização por perfil com `[Authorize(Roles = ...)]`;
- conta bloqueada por 15 minutos após 5 senhas erradas.

**Chaves e segredos**

- A chave do JWT, a conexão do banco e as senhas iniciais ficam **fora do código**: no `.env` (Docker) ou em *user-secrets* (local).
- O `appsettings.json` versionado tem esses campos vazios e o `.env` está no `.gitignore`.
- A API não inicia se a chave tiver menos de 32 caracteres.

**No aplicativo PHP:** o token fica na sessão do servidor (`$_SESSION`), nunca no navegador. Se a API
responder 401, o usuário volta para o login.

---

## 7. Estratégia de cache

Implementado com **IMemoryCache** na estratégia **cache-aside**, por trás da interface `ICacheService`:

1. o serviço procura a chave no cache;
2. **HIT** → devolve da memória, sem acessar o banco;
3. **MISS** → busca no banco, guarda e devolve;
4. toda escrita **invalida** as chaves afetadas.

| Dado | Chave | Expiração | Invalidado quando |
|---|---|---|---|
| Médicos, convênios, salas, medicamentos | `catalogo:*` | 10 min | o item é criado, editado ou excluído |
| Mapa de leitos | `leitos:{status}` | 1 min | muda um leito ou internação |
| Painel | `dashboard` | 30 s | muda paciente, consulta, triagem, leito ou internação |

Fila de triagem, estoque e faturamento **não** usam cache porque precisam estar sempre exatos.

**Como identificar:** cada resposta traz o cabeçalho `X-Cache: HIT` ou `X-Cache: MISS`, e o log da API mostra:

```
[CACHE MISS] catalogo:medicos → buscando na fonte original (banco de dados)
[CACHE SET]  catalogo:medicos armazenado por 600s
[CACHE HIT]  catalogo:medicos → dado servido da memória (expira em 599s)
[CACHE INVALIDADO] catalogo:medicos → a próxima leitura buscará dados atualizados no banco
```

`DELETE /api/cache` (Administrador) limpa todo o cache.

---

## 8. Padrão de resposta (ResultWrapper)

Todas as respostas, de sucesso ou de erro, têm o mesmo formato:

```json
{ "success": true, "message": "Operação realizada com sucesso.", "data": { }, "errors": [] }
```

| Status | Quando |
|---|---|
| 200 / 201 | sucesso (201 em criação) |
| 400 | JSON mal formado |
| 401 | sem token, token inválido ou expirado |
| 403 | perfil sem permissão |
| 404 | registro ou rota não encontrados |
| 405 | método HTTP não permitido para a rota |
| 409 | duplicidade (CPF, CRM, e-mail) |
| 415 | corpo enviado sem `Content-Type: application/json` |
| 422 | regra de negócio violada (`RN-XXX-NN`) |
| 500 | erro inesperado (detalhe só no log) |

---

## 9. Principais endpoints

Lista completa e testável no Swagger. 🔓 = público; os demais exigem token.

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/status` 🔓 | API no ar |
| POST | `/api/auth/login` 🔓 | login → JWT |
| GET | `/api/auth/me` | usuário do token |
| CRUD | `/api/usuarios` | usuários (Administrador) |
| CRUD | `/api/pacientes` | pacientes |
| GET | `/api/pacientes/{id}/prontuario` | prontuário completo |
| CRUD | `/api/medicos`, `/api/convenios`, `/api/salas`, `/api/medicamentos` | cadastros (com cache) |
| GET / POST | `/api/consultas` | listar e agendar |
| GET | `/api/consultas/fila-atendimento` | fila ordenada por Manchester |
| POST | `/api/consultas/{id}/triagem` | triagem com risco calculado |
| POST | `/api/prontuarios/{id}/prescricoes` | prescrição (Médico) |
| CRUD | `/api/leitos` | leitos (com cache) |
| GET / POST / PATCH | `/api/internacoes` | internação, alta e transferência |
| GET / POST | `/api/insumos` | estoque |
| GET / POST / PATCH | `/api/faturamentos` | guias, pagamento e auditoria |
| GET | `/api/dashboard` | indicadores (com cache) |
| DELETE | `/api/cache` | limpa o cache (Administrador) |

Mais exemplos prontos em `api/HospitalSystem.Api.http`.

---

## 10. Exemplos de requisições e respostas

**Login**

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@hospital.local", "senha": "Admin@2026" }
```

```json
{
  "success": true,
  "message": "Login realizado com sucesso.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tipo": "Bearer",
    "usuario": { "id": 1, "nome": "Administrador do Sistema", "perfil": "Administrador" }
  },
  "errors": []
}
```

**Endpoint protegido sem token** → `401`

```json
{ "success": false, "message": "Token não informado. Faça login para acessar este recurso.",
  "data": null, "errors": ["Token não informado. Faça login para acessar este recurso."] }
```

**Regra de negócio violada** — `POST /api/pacientes` com CPF inválido → `422`

```json
{ "success": false, "message": "A operação viola regras de negócio.", "data": null,
  "errors": ["RN-PAC-02: CPF inválido."] }
```

**Perfil sem permissão** — Recepção tentando excluir paciente → `403`

```json
{ "success": false, "message": "Acesso negado.", "data": null,
  "errors": ["O perfil 'Recepcao' não tem permissão para esta operação."] }
```

**Triagem** — `POST /api/consultas/1/triagem` → `201`

```json
{ "temperatura": 39.5, "pressaoArterial": "13/9", "frequenciaCardiaca": 105, "saturacaoOxigenio": 96, "escalaDor": 3 }
```

```json
{ "success": true, "message": "Triagem registrada. Classificação: Laranja.",
  "data": { "id": 1, "idConsulta": 1, "classificacaoRisco": "Laranja" }, "errors": [] }
```

**Cache** — `GET /api/medicos`

```
1ª chamada   → 200, X-Cache: MISS
2ª chamada   → 200, X-Cache: HIT
POST médico  → invalida catalogo:medicos
3ª chamada   → 200, X-Cache: MISS
```

---

## 11. Reprodução do projeto

Clone este repositório (botão **Code** do GitHub → copiar a URL) e, dentro da pasta clonada:

```bash
copy .env.example .env      # Linux/macOS: cp .env.example .env
docker compose up --build
```

Depois acesse http://localhost:5080/swagger (API) e http://localhost:8080/login.php (aplicativo) com os
usuários da seção 4.2.

---

## 12. Documentação complementar

| Arquivo | Conteúdo |
|---|---|
| [docs/REGRAS_DE_NEGOCIO.md](docs/REGRAS_DE_NEGOCIO.md) | todas as regras de negócio com código e a matriz de permissões |
| [docs/DECISOES_ARQUITETURAIS.md](docs/DECISOES_ARQUITETURAIS.md) | principais decisões de arquitetura e o motivo de cada uma |
| `api/HospitalSystem.Api.http` | requisições prontas para testar |
