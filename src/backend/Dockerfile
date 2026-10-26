# =====================================================
# Partnership Manager - Backend Dockerfile
# .NET 9 Multi-stage Build
# =====================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto e restaurar dependências
COPY ["src/backend/PartnershipManager.API/PartnershipManager.API.csproj", "PartnershipManager.API/"]
COPY ["src/backend/PartnershipManager.Application/PartnershipManager.Application.csproj", "PartnershipManager.Application/"]
COPY ["src/backend/PartnershipManager.Infrastructure/PartnershipManager.Infrastructure.csproj", "PartnershipManager.Infrastructure/"]
COPY ["src/backend/PartnershipManager.Domain/PartnershipManager.Domain.csproj", "PartnershipManager.Domain/"]

RUN dotnet restore "PartnershipManager.API/PartnershipManager.API.csproj"

# Copiar código fonte
COPY src/backend/ .

# Publish
FROM build AS publish
WORKDIR "/src/PartnershipManager.API"
RUN dotnet publish "PartnershipManager.API.csproj" -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Instalar curl para o health check
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Criar usuário não-root
RUN adduser --disabled-password --gecos "" appuser

# Copiar aplicação publicada
COPY --from=publish /app/publish .

# Copiar certificado SSL do MySQL
COPY ca-certificate.pem /app/

# Criar diretórios necessários e ajustar permissões
RUN mkdir -p /app/logs /app/storage && chown -R appuser /app
USER appuser

# Expor porta
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5000/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PartnershipManager.API.dll"]
