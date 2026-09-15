#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONTAINER="${HIDRIX_MSSQL_CONTAINER:-hidrix_mssql}"
SA_PASSWORD="${MSSQL_SA_PASSWORD:-Hidrix_Str0ng!Passw0rd}"

echo "==> Esperando SQL Server en $CONTAINER..."
for i in $(seq 1 60); do
  if docker exec "$CONTAINER" /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" &>/dev/null; then
    break
  fi
  sleep 2
done

for f in 001_create_database.sql 006_rename_to_dbhidrix.sql 002_tables.sql 003_seed_geo.sql 004_seed_colombia_geo_full.sql 005_drop_bug_add_serilog_log.sql 007_tables_catalog.sql 008_seed_catalog.sql 009_normalize_geo_fk.sql 010_seed_ecuador_honduras.sql 011_tables_metodo_cc.sql 012_prepare_identity.sql 013_tables_calculo_riego.sql 014_tables_nota_evento_riego.sql 015_nota_evento_riego_ciudad.sql 016_sensor_cc_estimation.sql 017_sensor_meta.sql; do
  echo "==> $f"
  docker cp "$ROOT/database/init/$f" "$CONTAINER:/tmp/$f"
  docker exec "$CONTAINER" /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -b -i "/tmp/$f"
done

echo "==> Migración EF Identity (dotnet ef database update)"
echo "    Ejecutar desde la raíz de ws-hidrix:"
echo "    export DOTNET_ROOT=\"\$HOME/.dotnet\" PATH=\"\$HOME/.dotnet:\$HOME/.dotnet/tools:\$PATH\""
echo "    # Cargar ConnectionStrings__DefaultConnection desde .env"
echo "    dotnet ef database update --project src/Hidrix.Infrastructure --startup-project src/Hidrix.Api"

echo "==> Verificación"
docker exec "$CONTAINER" /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -d dbHidrix -Q \
  "SELECT 'Pais' t, COUNT(*) c FROM HidrtbPais
   UNION ALL SELECT 'Depo', COUNT(*) FROM HidrtbDepartamento
   UNION ALL SELECT 'Ciu', COUNT(*) FROM HidrtbCiudad
   UNION ALL SELECT 'Depo_CO', COUNT(*) FROM HidrtbDepartamento WHERE Pais_Id=170
   UNION ALL SELECT 'Ciu_CO', COUNT(*) FROM HidrtbCiudad c INNER JOIN HidrtbDepartamento d ON d.Depo_Id=c.Depo_Id WHERE d.Pais_Id=170
   UNION ALL SELECT 'Depo_EC', COUNT(*) FROM HidrtbDepartamento WHERE Pais_Id=218
   UNION ALL SELECT 'Ciu_EC', COUNT(*) FROM HidrtbCiudad c INNER JOIN HidrtbDepartamento d ON d.Depo_Id=c.Depo_Id WHERE d.Pais_Id=218
   UNION ALL SELECT 'Depo_HN', COUNT(*) FROM HidrtbDepartamento WHERE Pais_Id=340
   UNION ALL SELECT 'Ciu_HN', COUNT(*) FROM HidrtbCiudad c INNER JOIN HidrtbDepartamento d ON d.Depo_Id=c.Depo_Id WHERE d.Pais_Id=340
   UNION ALL SELECT 'Red', COUNT(*) FROM HidrtbRed
   UNION ALL SELECT 'Cultivo', COUNT(*) FROM HidrtbCultivo
   UNION ALL SELECT 'SensorMeta', COUNT(*) FROM HidrtbSensorMeta;"
echo "OK: base dbHidrix lista."
