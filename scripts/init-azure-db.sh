#!/usr/bin/env bash
# Inicializa el esquema Hidrtb* en una Azure SQL Database ya creada (p.ej. "hidrix").
#
# Los scripts en database/init/*.sql están escritos para SQL Server local con
# `USE [dbHidrix];` al inicio. Azure SQL Database:
#   - No soporta el statement USE para cambiar/fijar la base de datos.
#   - Ya tiene el nombre de base que tú le hayas puesto (ej. "hidrix"),
#     no "dbHidrix".
# Por eso este script omite 001_create_database.sql y 006_rename_to_dbhidrix.sql
# (crean la BD / ajustan collation; ya existe) y a los demás les quita el bloque
# `USE [dbHidrix]; / GO` antes de ejecutarlos, conectando directo a la BD destino.
#
# Orden:
#   002..010, 012  -> geo, logs, catálogo, prepara Identity
#   EF Identity    -> `dotnet ef migrations script --idempotent` (crea HidrtbUsuario, roles, ...)
#   011            -> HidrtbMetodoCC (después de 012, que la elimina)
#   013..017       -> tablas con FK a HidrtbUsuario y metadatos de sensor
#
# Requiere: sqlcmd (mssql-tools18) y dotnet + dotnet-ef instalados localmente,
# y una regla de firewall en el SQL server de Azure para tu IP.
#
# Uso:
#   ./scripts/init-azure-db.sh                    # lee .env.azure en la raíz del repo
#   ./scripts/init-azure-db.sh ruta/otro.env      # o un env file distinto
#   ./scripts/init-azure-db.sh --reset [env]      # BORRA todos los objetos de la BD antes de migrar

set -euo pipefail

RESET=0
if [ "${1:-}" = "--reset" ]; then
  RESET=1
  shift
fi

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="${1:-$ROOT/.env.azure}"

if [ ! -f "$ENV_FILE" ]; then
  echo "No se encontró $ENV_FILE. Copia .env.azure.example a .env.azure y complétalo." >&2
  exit 1
fi

for cmd in sqlcmd dotnet; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "$cmd no está instalado." >&2
    exit 1
  fi
done

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

run_sql() {
  sqlcmd -S "$FQDN" -d "$AZ_SQL_DB_NAME" -U "$AZ_SQL_ADMIN_USER" -P "$AZ_SQL_ADMIN_PASSWORD" -N -C -I -l 60 -b "$@"
}

run_init_file() {
  local f="$1"
  echo "==> $f"
  sed -e '/^USE \[dbHidrix\];$/{N;/\nGO$/d}' "$ROOT/database/init/$f" > "$TMP_DIR/$f"
  run_sql -i "$TMP_DIR/$f"
}

echo "==> Conectando a ${FQDN} / ${AZ_SQL_DB_NAME}"
run_sql -Q "SELECT 1" > /dev/null

if [ "$RESET" -eq 1 ]; then
  echo "==> --reset: eliminando todos los objetos de usuario en ${AZ_SQL_DB_NAME}"
  run_sql -Q "
    SET NOCOUNT ON;
    DECLARE @sql NVARCHAR(MAX) = N'';
    SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.'
        + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
    FROM sys.foreign_keys;
    EXEC sp_executesql @sql;

    SET @sql = N'';
    SELECT @sql += N'DROP ' + CASE o.type
            WHEN 'V'  THEN N'VIEW'
            WHEN 'P'  THEN N'PROCEDURE'
            WHEN 'FN' THEN N'FUNCTION' WHEN 'IF' THEN N'FUNCTION' WHEN 'TF' THEN N'FUNCTION'
            WHEN 'SO' THEN N'SEQUENCE'
            WHEN 'U'  THEN N'TABLE'
        END + N' ' + QUOTENAME(s.name) + N'.' + QUOTENAME(o.name) + N';'
    FROM sys.objects o
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE o.is_ms_shipped = 0 AND o.type IN ('V','P','FN','IF','TF','SO','U')
    ORDER BY CASE WHEN o.type = 'U' THEN 1 ELSE 0 END;
    EXEC sp_executesql @sql;
  "
fi

for f in \
  002_tables.sql \
  003_seed_geo.sql \
  004_seed_colombia_geo_full.sql \
  005_drop_bug_add_serilog_log.sql \
  007_tables_catalog.sql \
  008_seed_catalog.sql \
  009_normalize_geo_fk.sql \
  010_seed_ecuador_honduras.sql \
  012_prepare_identity.sql; do
  run_init_file "$f"
done

echo "==> Migraciones EF (Identity)"
dotnet ef migrations script --idempotent \
  --project "$ROOT/src/Hidrix.Infrastructure" \
  --startup-project "$ROOT/src/Hidrix.Api" \
  -o "$TMP_DIR/ef_migrations.sql"
run_sql -i "$TMP_DIR/ef_migrations.sql"

for f in \
  011_tables_metodo_cc.sql \
  013_tables_calculo_riego.sql \
  014_tables_nota_evento_riego.sql \
  015_nota_evento_riego_ciudad.sql \
  016_sensor_cc_estimation.sql \
  017_sensor_meta.sql; do
  run_init_file "$f"
done

echo "==> Verificación"
run_sql -W -Q "
  SET NOCOUNT ON;
  SELECT 'Pais' t, COUNT(*) c FROM HidrtbPais
  UNION ALL SELECT 'Depo', COUNT(*) FROM HidrtbDepartamento
  UNION ALL SELECT 'Ciu', COUNT(*) FROM HidrtbCiudad
  UNION ALL SELECT 'Red', COUNT(*) FROM HidrtbRed
  UNION ALL SELECT 'Cultivo', COUNT(*) FROM HidrtbCultivo
  UNION ALL SELECT 'MetodoCC', COUNT(*) FROM HidrtbMetodoCC
  UNION ALL SELECT 'SensorMeta', COUNT(*) FROM HidrtbSensorMeta
  UNION ALL SELECT 'Usuario', COUNT(*) FROM HidrtbUsuario
  UNION ALL SELECT 'EFMigrations', COUNT(*) FROM __EFMigrationsHistory;"

echo "OK: esquema listo en ${AZ_SQL_DB_NAME}."
