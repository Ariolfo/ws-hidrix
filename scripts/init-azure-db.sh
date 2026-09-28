#!/usr/bin/env bash
# Inicializa el esquema Yarqtb* en una Azure SQL Database ya creada (p.ej. "hidrix").
#
# Los scripts en database/init/*.sql están escritos para SQL Server local con
# `USE [dbYarqua];` al inicio. Azure SQL Database:
#   - No soporta el statement USE para cambiar/fijar la base de datos.
#   - Ya tiene el nombre de base que tú le hayas puesto (ej. "hidrix"),
#     no "dbYarqua".
# Por eso este script omite 001_create_database.sql y 006_rename_to_dbyarqua.sql
# (crean/renombran la BD; ya existe) y a los demás les quita el bloque
# `USE [dbYarqua]; / GO` antes de ejecutarlos, conectando directo a la BD destino.
#
# Requiere: sqlcmd (mssql-tools18) instalado localmente.
#   Debian/Ubuntu: https://learn.microsoft.com/sql/linux/sql-server-linux-setup-tools
#
# Uso:
#   ./scripts/init-azure-db.sh                # lee .env.azure en la raíz del repo
#   ./scripts/init-azure-db.sh ruta/otro.env   # o un env file distinto

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="${1:-$ROOT/.env.azure}"

if [ ! -f "$ENV_FILE" ]; then
  echo "No se encontró $ENV_FILE. Copia .env.azure.example a .env.azure y complétalo." >&2
  exit 1
fi

if ! command -v sqlcmd &>/dev/null; then
  echo "sqlcmd no está instalado. Instala mssql-tools18 o usa Azure Data Studio para correr" >&2
  echo "manualmente 002_tables.sql, 003_seed_geo.sql, 004_seed_colombia_geo_full.sql y" >&2
  echo "005_drop_bug_add_serilog_log.sql (sin la línea USE [dbYarqua];/GO) contra tu BD." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

for var in AZ_SQL_SERVER_NAME AZ_SQL_DB_NAME AZ_SQL_ADMIN_USER AZ_SQL_ADMIN_PASSWORD; do
  if [ -z "${!var:-}" ] || [[ "${!var:-}" == CAMBIAR_* ]]; then
    echo "Falta $var en $ENV_FILE" >&2
    exit 1
  fi
done

FQDN="${AZ_SQL_SERVER_NAME}.database.windows.net"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

echo "==> Conectando a ${FQDN} / ${AZ_SQL_DB_NAME}"
sqlcmd -S "$FQDN" -d "$AZ_SQL_DB_NAME" -U "$AZ_SQL_ADMIN_USER" -P "$AZ_SQL_ADMIN_PASSWORD" -N -C -Q "SELECT 1" -b

for f in 002_tables.sql 003_seed_geo.sql 004_seed_colombia_geo_full.sql 005_drop_bug_add_serilog_log.sql; do
  echo "==> $f"
  stripped="$TMP_DIR/$f"
  sed -e '/^USE \[dbYarqua\];$/{N;/\nGO$/d}' "$ROOT/database/init/$f" > "$stripped"
  sqlcmd -S "$FQDN" -d "$AZ_SQL_DB_NAME" -U "$AZ_SQL_ADMIN_USER" -P "$AZ_SQL_ADMIN_PASSWORD" -N -C -b -i "$stripped"
done

echo "==> Verificación"
sqlcmd -S "$FQDN" -d "$AZ_SQL_DB_NAME" -U "$AZ_SQL_ADMIN_USER" -P "$AZ_SQL_ADMIN_PASSWORD" -N -C -Q \
  "SELECT 'Pais' t, COUNT(*) c FROM YarqtbPais
   UNION ALL SELECT 'Depo', COUNT(*) FROM YarqtbDepartamento
   UNION ALL SELECT 'Ciu', COUNT(*) FROM YarqtbCiudad;"

echo "OK: esquema listo en ${AZ_SQL_DB_NAME}."
