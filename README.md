# Yarqua Backend (ws-yarqua)

API .NET 9 Clean Architecture + CQRS para el MVP Ionic de Yarqua.  
Incluye **SQL Server** (scripts + Docker) en este mismo repositorio.

## Contenido del repo

| Ruta | Rol |
|------|-----|
| `src/` | Domain, Application, Infrastructure, Api |
| `tests/` | Pruebas unitarias |
| `database/` | Scripts T-SQL `Yarqtb*` + `install.sh` |
| `docker-compose.yml` | SQL Server **2019 CU25 (15.0.4355.3)** en puerto **1433** |
| `.env.example` | Plantilla de secretos (no versionar `.env`) |

## Capas

| Proyecto | Rol |
|----------|-----|
| **Yarqua.Domain** | Entidades `Yarqtb*` (sin dependencias) |
| **Yarqua.Application** | Casos de uso MediatR, FluentValidation, DTOs, repositorios (contratos) |
| **Yarqua.Infrastructure** | EF Core SQL Server, repositorios, JWT HS256, Visualiti, geo |
| **Yarqua.Api** | Controllers `/api/v1`, Swagger, CORS, Serilog → `YarqtbLog` |
| **Yarqua.Application.Tests** | Pruebas unitarias |

## Base de datos (SQL Server)

```bash
cd ws-yarqua
docker compose up -d
./database/install.sh
```

- Contenedor: `yarqua_mssql`
- Puerto host: `1433`
- SA (dev, vía Docker): ver `docker-compose.yml` / `.env`
- Base: **`dbYarqua`** · collation **`Modern_Spanish_CI_AS`**
- Imagen: `local/dragro-mssql:2019-cu25-15.0.4355.3`

> Si otro contenedor ya usa el 1433, deténgalo o cambie el mapeo.

## Contrato API

Base: `/api/v1` · JSON **camelCase** · Sobre uniforme:

```json
{ "success": true, "message": "OK", "data": { } }
```

| Método | Ruta |
|--------|------|
| POST | `/api/v1/auth/register` |
| POST | `/api/v1/auth/refresh` |
| GET | `/api/v1/geo/countries` |
| GET | `/api/v1/geo/countries/{paisId}/departments` |
| GET | `/api/v1/geo/departments/{depoId}/cities?q&limit` |
| GET | `/api/v1/stations?lat&lng&radius=50&includeSensors=false` |
| GET | `/api/v1/stations/{stationId}/sensors` |
| GET | `/api/v1/sensors/{sensorId}` |
| GET | `/api/v1/sensors/{sensorId}/history?range=today\|7d\|30d\|6m` |
| GET | `/health` → `{ status, database }` (sin sobre) |

## Configuración (secretos)

`appsettings*.json` **no** guarda contraseñas ni JWT. Los secretos van por:

1. **User Secrets** (recomendado en Development)
2. **Variables de entorno** / archivo **`.env`** (gitignored)
3. Plantilla versionada: [`.env.example`](.env.example)

```bash
cd ws-yarqua
cp .env.example .env   # editar valores reales

# o con User Secrets:
cd src/Yarqua.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=dbYarqua;User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=False"
dotnet user-secrets set "Jwt:Secret" "***"
dotnet user-secrets set "Visualiti:Usuario" "***"
dotnet user-secrets set "Visualiti:Password" "***"
```

Variables clave (formato env):

- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret`, `Jwt__AccessMinutes`, `Jwt__RefreshDays`
- `Visualiti__Enabled`, `Visualiti__Usuario`, `Visualiti__Password`, …

## Cómo ejecutar

```bash
export PATH="$HOME/.dotnet:$PATH"
cd ws-yarqua
dotnet restore
dotnet build
dotnet run --project src/Yarqua.Api --launch-profile http
# http://localhost:5080  · Swagger (Development): /swagger  · Health: /health
```

## Pruebas

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test
```

## Notas

- Sensores: catálogo estático; lecturas en vivo vía Visualiti.
- Estaciones: `fin-{slug}` (finca) o `sn-M###` (sensor sin finca).
- Humedad: `Cont Vol1` → depth10cm, `Cont Vol2` → depth30cm; valor ≤ 1.5 → ×100.
- Irrigation **no** forma parte de este backend (lógica en la app).

## Despliegue en Azure (Container Apps)

Recursos objetivo: **Azure SQL Database** + **Azure Container Registry** + **Container App**.

1. Construir la imagen localmente (opcional, para probar):

   ```bash
   docker build -t yarqua-api:local .
   docker run -p 8080:8080 --env-file .env yarqua-api:local
   ```

   El `Dockerfile` es multi-stage (.NET 9 SDK → runtime `aspnet:9.0`), corre como
   usuario no-root y expone el puerto **8080** (`ASPNETCORE_URLS=http://+:8080`).
   El contenedor **falla rápido al iniciar** si no puede conectar a la base de
   datos (el sink de Serilog hacia `YarqtbLog` se inicializa de forma eager),
   así que la BD debe estar accesible y creada antes de arrancar la app.

2. Aprovisionar Azure SQL, ACR, Container Apps Environment y el Container App:

   ```bash
   cp .env.azure.example .env.azure   # completar con los valores reales
   az login
   ./scripts/deploy-azure.sh
   ```

   El script es idempotente (re-ejecutarlo actualiza imagen y variables) y usa
   `az acr build` para compilar la imagen dentro de Azure (no requiere Docker
   local). Ver comentarios en `scripts/deploy-azure.sh` y `.env.azure.example`
   para el detalle de cada recurso.

3. Ejecutar los scripts de `database/init/` contra la Azure SQL Database creada
   (mismo orden que usa `database/install.sh`, pero apuntando al server
   `*.database.windows.net` en vez de al contenedor local).

4. Variables de entorno en producción (`ConnectionStrings__DefaultConnection`,
   `Jwt__Secret`, `Visualiti__*`) se inyectan como **secrets/env vars del
   Container App**, nunca dentro de la imagen: `.env` y `.env.azure` están en
   `.gitignore` y excluidos vía `.dockerignore`. Para Azure SQL la cadena de
   conexión debe usar `Encrypt=True;TrustServerCertificate=False` (a diferencia
   del `Encrypt=False` usado con el SQL Server local en `docker-compose.yml`).
