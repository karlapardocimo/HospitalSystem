# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só os .csproj (camada de cache do Docker)
COPY Directory.Build.props HospitalSystem.sln ./
COPY src/HospitalSystem.Domain/HospitalSystem.Domain.csproj src/HospitalSystem.Domain/
COPY src/HospitalSystem.Application/HospitalSystem.Application.csproj src/HospitalSystem.Application/
COPY src/HospitalSystem.Infrastructure/HospitalSystem.Infrastructure.csproj src/HospitalSystem.Infrastructure/
COPY src/HospitalSystem.Api/HospitalSystem.Api.csproj src/HospitalSystem.Api/
RUN dotnet restore src/HospitalSystem.Api/HospitalSystem.Api.csproj

COPY src/ src/
RUN dotnet publish src/HospitalSystem.Api/HospitalSystem.Api.csproj -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Fuso horário: as regras de data (consulta no passado, bloqueio de login) usam a hora local
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=America/Sao_Paulo
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "HospitalSystem.Api.dll"]
