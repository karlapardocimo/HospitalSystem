# Como abrir o sistema — guia rápido

O trabalho roda em **Docker** (forma oficial). Detalhes e explicações estão no `README.md`.

## 1. Instale o Docker Desktop (só na primeira vez)

- Link: https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe
- Na instalação, deixe marcado **Per-user installation** + **WSL 2** e clique em OK.
- Se o instalador der erro de ".NET Framework", instale pela Microsoft Store: https://apps.microsoft.com/detail/xp8cbj40xlbwkx (ou rode `winget install Docker.DockerDesktop`).
- Abra o Docker Desktop e espere aparecer **Engine running**.

## 2. Suba o sistema

Desligue o MySQL do XAMPP, se estiver ligado. Depois, no Prompt de Comando, na pasta do projeto:

```
cd "C:\caminho\para\HospitalSystem"
copy .env.example .env
docker compose up --build
```

A primeira vez demora alguns minutos. Está pronto quando aparecer `Banco de dados pronto.`

## 3. Acesse

| O quê | Endereço |
|---|---|
| Aplicativo | http://localhost:8080/login.php |
| API (Swagger) | http://localhost:5080/swagger |

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | admin@hospital.local | Admin@2026 |
| Médico | medico@hospital.local | Demo@2026 |
| Enfermagem | enfermagem@hospital.local | Demo@2026 |
| Recepção | recepcao@hospital.local | Demo@2026 |

**No Swagger:** faça `POST /api/auth/login`, copie o `token` da resposta, clique em **Authorize** e cole o token.

## Comandos úteis

| Para | Comando |
|---|---|
| Ver os containers | `docker compose ps` |
| Ver o log da API (cache HIT/MISS) | `docker compose logs -f api` |
| Abrir o banco | `docker exec -it hospital-db mariadb -u hospital -p hospital` (senha = `DB_PASSWORD` do `.env`) |
| Parar | Ctrl + C, ou `docker compose down` |
| Apagar o banco e recomeçar | `docker compose down -v` |

## Se der erro

| Erro | Solução |
|---|---|
| "Docker daemon is not running" | Abra o Docker Desktop e espere "Engine running" |
| Porta 3307, 5080 ou 8080 ocupada | Desligue o MySQL do XAMPP e feche outros terminais da API |
| Login com 401 | Confira e-mail e senha da tabela acima |

## Alternativa sem Docker (desenvolvimento)

Precisa do XAMPP (MySQL ligado) e do .NET 10 SDK. Siga a "Opção B" do `README.md`: configurar os `user-secrets`, rodar `dotnet run --launch-profile http` na pasta `api\src\HospitalSystem.Api` e `C:\xampp\php\php.exe -S localhost:8000` na pasta `web`.
