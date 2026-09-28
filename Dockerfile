# syntax=docker/dockerfile:1

# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia solo los .csproj primero para cachear `dotnet restore`.
COPY src/Yarqua.Domain/Yarqua.Domain.csproj src/Yarqua.Domain/
COPY src/Yarqua.Application/Yarqua.Application.csproj src/Yarqua.Application/
COPY src/Yarqua.Infrastructure/Yarqua.Infrastructure.csproj src/Yarqua.Infrastructure/
COPY src/Yarqua.Api/Yarqua.Api.csproj src/Yarqua.Api/

RUN dotnet restore src/Yarqua.Api/Yarqua.Api.csproj

COPY src/ src/

RUN dotnet publish src/Yarqua.Api/Yarqua.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN useradd --uid 10001 --create-home --shell /usr/sbin/nologin appuser

# Azure Container Apps enruta al puerto de destino configurado en el ingress;
# 8080 evita requerir privilegios y coincide con el target port recomendado.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "Yarqua.Api.dll"]
