# Hidrix Backend (ws-hidrix)

API .NET 9 Clean Architecture + CQRS para la app Ionic de Hidrix.  
Incluye **SQL Server** (scripts + Docker) en este mismo repositorio.

## Contenido del repo

| Ruta | Rol |
|------|-----|
| `src/` | Domain, Application, Infrastructure, Api |
| `tests/` | Pruebas unitarias |
| `database/` | Scripts T-SQL `Hidrtb*` + `install.sh` |
| `docker-compose.yml` | SQL Server **2019 CU25 (15.0.4355.3)** en puerto **1433** |
| `.env.example` | Plantilla de secretos (no versionar `.env`) |

## Capas

| Proyecto | Rol |
|----------|-----|
| **Hidrix.Domain** | Entidades `Hidrtb*` (sin dependencias) |
| **Hidrix.Application** | Casos de uso MediatR, FluentValidation, DTOs, repositorios (contratos) |
| **Hidrix.Infrastructure** | EF Core SQL Server, repositorios, JWT HS256, Visualiti, geo |
| **Hidrix.Api** | Controllers `/api/v1`, Swagger, CORS, Serilog → `HidrtbLog` |
| **Hidrix.Application.Tests** | Pruebas unitarias |

## Base de datos (SQL Server)

```bash
cd ws-hidrix
docker compose up -d
./database/install.sh
```

- Contenedor: `hidrix_mssql`
- Puerto host: `1433`
- SA (dev, vía Docker): ver `docker-compose.yml` / `.env`
- Base: **`dbHidrix`** · collation **`Modern_Spanish_CI_AS`**
- Imagen: `local/mssql:2019-cu25-15.0.4355.3`

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
cd ws-hidrix
cp .env.example .env   # editar valores reales

# o con User Secrets:
cd src/Hidrix.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=dbHidrix;User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=False"
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
cd ws-hidrix
dotnet restore
dotnet build
dotnet run --project src/Hidrix.Api --launch-profile http
```

| Servicio | URL / puerto |
|----------|----------------|
| API HTTP | `http://localhost:5080` (escucha `0.0.0.0:5080`) |
| Swagger | `http://localhost:5080/swagger` (Development) |
| Health | `http://localhost:5080/health` |
| SQL Server | `localhost:1433` · base `dbHidrix` |

## App móvil (app-hidrix) — el otro lado

La UI Ionic corre en el puerto **8100** y apunta a esta API vía `apiBaseUrl`.

| Servicio | URL / puerto |
|----------|----------------|
| App web (Ionic) | `http://localhost:8100` |
| API que usa la app | `http://127.0.0.1:5080/api/v1` |

```bash
cd ../app-hidrix   # o la ruta del repo app-hidrix
npm install
npx ionic serve
# → http://localhost:8100
```

Configuración de la URL en `app-hidrix/src/environments/environment.ts`.  
Detalle (Capacitor, pantallas): ver `app-hidrix/README.md`.

## Pruebas

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test
```

## Notas

- Inventario de estaciones/sensores: API Visualiti (`/api/devices`, `/sensor`, `/hardware-status`).
- Metadatos Hidrix: `HidrtbSensorMeta` (cultivo, finca, CC estimada).
- Estaciones: `fin-{slug}` (finca) o `sn-M###` (sensor sin finca).
- Humedad: `Cont Vol1` → sensor_1, `Cont Vol2` → sensor_2; valor ≤ 1.5 → ×100.
- Irrigation **no** forma parte de este backend (lógica en la app).
