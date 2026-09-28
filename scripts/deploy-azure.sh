#!/usr/bin/env bash
# Aprovisiona (si no existen) y despliega ws-yarqua en Azure:
#   Resource Group -> Azure SQL (Server + DB) -> Container Registry -> Container Apps Env -> Container App
#
# Requisitos: az CLI logueado (`az login`) y con la suscripción correcta
# seleccionada (`az account set --subscription <id>`). Docker no es necesario:
# la imagen se construye remotamente con `az acr build`.
#
# Uso:
#   cp .env.azure.example .env.azure   # completar valores reales
#   ./scripts/deploy-azure.sh
#
# El script es idempotente: puede re-ejecutarse para actualizar la imagen o
# los env vars del Container App.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="${1:-$ROOT/.env.azure}"

if [ ! -f "$ENV_FILE" ]; then
  echo "No se encontró $ENV_FILE. Copia .env.azure.example a .env.azure y complétalo." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

required=(
  AZ_RESOURCE_GROUP AZ_LOCATION
  AZ_SQL_SERVER_NAME AZ_SQL_DB_NAME AZ_SQL_ADMIN_USER AZ_SQL_ADMIN_PASSWORD
  AZ_ACR_NAME AZ_CONTAINERAPPS_ENV AZ_CONTAINERAPP_NAME
  ConnectionStrings__DefaultConnection Jwt__Secret Visualiti__Usuario Visualiti__Password
)
missing=()
for var in "${required[@]}"; do
  if [ -z "${!var:-}" ] || [[ "${!var:-}" == CAMBIAR_* ]]; then
    missing+=("$var")
  fi
done
if [ ${#missing[@]} -gt 0 ]; then
  echo "Faltan valores en $ENV_FILE: ${missing[*]}" >&2
  exit 1
fi

echo "==> Resource group: $AZ_RESOURCE_GROUP ($AZ_LOCATION)"
az group create \
  --name "$AZ_RESOURCE_GROUP" \
  --location "$AZ_LOCATION" \
  --output none

echo "==> Azure SQL: server $AZ_SQL_SERVER_NAME"
if ! az sql server show --name "$AZ_SQL_SERVER_NAME" --resource-group "$AZ_RESOURCE_GROUP" &>/dev/null; then
  az sql server create \
    --name "$AZ_SQL_SERVER_NAME" \
    --resource-group "$AZ_RESOURCE_GROUP" \
    --location "$AZ_LOCATION" \
    --admin-user "$AZ_SQL_ADMIN_USER" \
    --admin-password "$AZ_SQL_ADMIN_PASSWORD" \
    --output none
else
  echo "    ya existe, se omite"
fi

echo "==> Regla de firewall: permitir servicios de Azure (Container Apps)"
az sql server firewall-rule create \
  --resource-group "$AZ_RESOURCE_GROUP" \
  --server "$AZ_SQL_SERVER_NAME" \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  --output none

echo "==> Base de datos: $AZ_SQL_DB_NAME"
if ! az sql db show --name "$AZ_SQL_DB_NAME" --server "$AZ_SQL_SERVER_NAME" --resource-group "$AZ_RESOURCE_GROUP" &>/dev/null; then
  az sql db create \
    --name "$AZ_SQL_DB_NAME" \
    --server "$AZ_SQL_SERVER_NAME" \
    --resource-group "$AZ_RESOURCE_GROUP" \
    --edition GeneralPurpose \
    --family Gen5 \
    --capacity 1 \
    --compute-model Serverless \
    --collation Modern_Spanish_CI_AS \
    --output none
else
  echo "    ya existe, se omite"
fi

echo ""
echo "!! La base de datos está vacía. Ejecuta los scripts de database/init contra:"
echo "   Server=tcp:${AZ_SQL_SERVER_NAME}.database.windows.net,1433;Database=${AZ_SQL_DB_NAME};..."
echo "   (por ejemplo con sqlcmd, Azure Data Studio o 'az sql db import')."
echo ""

echo "==> Container Registry: $AZ_ACR_NAME"
if ! az acr show --name "$AZ_ACR_NAME" --resource-group "$AZ_RESOURCE_GROUP" &>/dev/null; then
  az acr create \
    --name "$AZ_ACR_NAME" \
    --resource-group "$AZ_RESOURCE_GROUP" \
    --sku Basic \
    --admin-enabled true \
    --output none
else
  echo "    ya existe, se omite"
fi

IMAGE_NAME="${AZ_IMAGE_NAME:-$AZ_CONTAINERAPP_NAME}"
IMAGE_TAG="$(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null || date +%Y%m%d%H%M%S)"
IMAGE="${IMAGE_NAME}:${IMAGE_TAG}"
echo "==> Build remoto de imagen en ACR: $IMAGE"
az acr build \
  --registry "$AZ_ACR_NAME" \
  --image "$IMAGE" \
  --image "${IMAGE_NAME}:latest" \
  --file "$ROOT/Dockerfile" \
  "$ROOT"

ACR_SERVER=$(az acr show --name "$AZ_ACR_NAME" --resource-group "$AZ_RESOURCE_GROUP" --query loginServer -o tsv)

if ! az extension show --name containerapp &>/dev/null; then
  echo "==> Instalando extensión az containerapp"
  az extension add --name containerapp --upgrade --output none
fi
az provider register --namespace Microsoft.App --wait --output none
az provider register --namespace Microsoft.OperationalInsights --wait --output none

echo "==> Container Apps environment: $AZ_CONTAINERAPPS_ENV"
if ! az containerapp env show --name "$AZ_CONTAINERAPPS_ENV" --resource-group "$AZ_RESOURCE_GROUP" &>/dev/null; then
  az containerapp env create \
    --name "$AZ_CONTAINERAPPS_ENV" \
    --resource-group "$AZ_RESOURCE_GROUP" \
    --location "$AZ_LOCATION" \
    --output none
else
  echo "    ya existe, se omite"
fi

CONNECTION_STRING="${ConnectionStrings__DefaultConnection:-Server=tcp:${AZ_SQL_SERVER_NAME}.database.windows.net,1433;Database=${AZ_SQL_DB_NAME};User Id=${AZ_SQL_ADMIN_USER};Password=${AZ_SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30}"

echo "==> Container App: $AZ_CONTAINERAPP_NAME"
if ! az containerapp show --name "$AZ_CONTAINERAPP_NAME" --resource-group "$AZ_RESOURCE_GROUP" &>/dev/null; then
  az containerapp create \
    --name "$AZ_CONTAINERAPP_NAME" \
    --resource-group "$AZ_RESOURCE_GROUP" \
    --environment "$AZ_CONTAINERAPPS_ENV" \
    --image "${ACR_SERVER}/${IMAGE}" \
    --registry-server "$ACR_SERVER" \
    --target-port 8080 \
    --ingress external \
    --min-replicas 1 \
    --max-replicas 3 \
    --secrets \
      "connection-string=${CONNECTION_STRING}" \
      "jwt-secret=${Jwt__Secret}" \
      "visualiti-usuario=${Visualiti__Usuario}" \
      "visualiti-password=${Visualiti__Password}" \
    --env-vars \
      "ASPNETCORE_ENVIRONMENT=Production" \
      "ConnectionStrings__DefaultConnection=secretref:connection-string" \
      "Jwt__Secret=secretref:jwt-secret" \
      "Jwt__AccessMinutes=${Jwt__AccessMinutes:-60}" \
      "Jwt__RefreshDays=${Jwt__RefreshDays:-30}" \
      "Visualiti__Enabled=${Visualiti__Enabled:-true}" \
      "Visualiti__LoginUrl=${Visualiti__LoginUrl:-http://appgricultor.com/api/login}" \
      "Visualiti__ApiUrl=${Visualiti__ApiUrl:-https://api.appgricultor.com}" \
      "Visualiti__Cliente=${Visualiti__Cliente:-AGROSAVIA}" \
      "Visualiti__Usuario=secretref:visualiti-usuario" \
      "Visualiti__Password=secretref:visualiti-password" \
      "Visualiti__SslVerify=${Visualiti__SslVerify:-false}" \
    --output none
else
  echo "    ya existe: actualizando imagen y variables"
  az containerapp secret set \
    --name "$AZ_CONTAINERAPP_NAME" \
    --resource-group "$AZ_RESOURCE_GROUP" \
    --secrets \
      "connection-string=${CONNECTION_STRING}" \
      "jwt-secret=${Jwt__Secret}" \
      "visualiti-usuario=${Visualiti__Usuario}" \
      "visualiti-password=${Visualiti__Password}" \
    --output none
  az containerapp update \
    --name "$AZ_CONTAINERAPP_NAME" \
    --resource-group "$AZ_RESOURCE_GROUP" \
    --image "${ACR_SERVER}/${IMAGE}" \
    --set-env-vars \
      "ASPNETCORE_ENVIRONMENT=Production" \
      "ConnectionStrings__DefaultConnection=secretref:connection-string" \
      "Jwt__Secret=secretref:jwt-secret" \
      "Jwt__AccessMinutes=${Jwt__AccessMinutes:-60}" \
      "Jwt__RefreshDays=${Jwt__RefreshDays:-30}" \
      "Visualiti__Enabled=${Visualiti__Enabled:-true}" \
      "Visualiti__LoginUrl=${Visualiti__LoginUrl:-http://appgricultor.com/api/login}" \
      "Visualiti__ApiUrl=${Visualiti__ApiUrl:-https://api.appgricultor.com}" \
      "Visualiti__Cliente=${Visualiti__Cliente:-AGROSAVIA}" \
      "Visualiti__Usuario=secretref:visualiti-usuario" \
      "Visualiti__Password=secretref:visualiti-password" \
      "Visualiti__SslVerify=${Visualiti__SslVerify:-false}" \
    --output none
fi

FQDN=$(az containerapp show --name "$AZ_CONTAINERAPP_NAME" --resource-group "$AZ_RESOURCE_GROUP" --query properties.configuration.ingress.fqdn -o tsv)
echo ""
echo "==> Listo: https://${FQDN}"
echo "    Health: https://${FQDN}/health"
