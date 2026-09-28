# Diagrama ER — Hidrix (`dbHidrix`)

Modelo actual de la base de datos (nomenclatura `Hidrtb*`).
Desde la migración a ASP.NET Core Identity, `HidrtbUsuario` usa `nvarchar(450)` como PK (GUID de Identity).

## Diagrama

```mermaid
erDiagram
    HidrtbPais ||--o{ HidrtbDepartamento : "tiene"
    HidrtbDepartamento ||--o{ HidrtbCiudad : "tiene"
    HidrtbCiudad ||--o{ HidrtbUsuario : "reside_en"
    HidrtbPais ||--o{ HidrtbRed : "opera_en"
    HidrtbCultivo ||--o{ HidrtbSensorMeta : "asocia"
    HidrtbUsuario ||--o{ HidrtbUsuarioRol : "tiene"
    HidrtbRol ||--o{ HidrtbUsuarioRol : "agrupa"

    HidrtbPais {
        int Pais_Id PK
        nvarchar Pais_Nombre
        int Pais_Estado
    }

    HidrtbDepartamento {
        int Depo_Id PK
        int Pais_Id FK
        nvarchar Depo_Code
        nvarchar Depo_Nombre
    }

    HidrtbCiudad {
        int Ciu_Id PK
        nvarchar Ciu_Nombre
        int Depo_Id FK
        nvarchar Ciu_Cod
    }

    HidrtbUsuario {
        nvarchar Usua_Id PK "GUID Identity (450)"
        nvarchar Usua_Nombre
        nvarchar UserName
        nvarchar NormalizedUserName
        nvarchar Email
        nvarchar NormalizedEmail
        nvarchar PasswordHash
        int Ciu_Id FK
        datetime2 Usua_FechaRegistro
        datetime2 Usua_FechaCreacion
        datetime2 Usua_FechaActualizacion
        bit Usua_Activo
    }

    HidrtbRol {
        nvarchar Id PK
        nvarchar Name
        nvarchar NormalizedName
    }

    HidrtbUsuarioRol {
        nvarchar UserId PK "FK → HidrtbUsuario"
        nvarchar RoleId PK "FK → HidrtbRol"
    }

    HidrtbUsuarioClaim {
        int Id PK
        nvarchar UserId FK
        nvarchar ClaimType
        nvarchar ClaimValue
    }

    HidrtbUsuarioLogin {
        nvarchar LoginProvider PK
        nvarchar ProviderKey PK
        nvarchar UserId FK
    }

    HidrtbUsuarioToken {
        nvarchar UserId PK "FK"
        nvarchar LoginProvider PK
        nvarchar Name PK
        nvarchar Value
    }

    HidrtbRolClaim {
        int Id PK
        nvarchar RoleId FK
        nvarchar ClaimType
        nvarchar ClaimValue
    }

    HidrtbRed {
        int Red_Id PK
        nvarchar Red_Nombre
        int Pais_Id FK
    }

    HidrtbCultivo {
        int Cult_Id PK
        nvarchar Cult_Nombre
        decimal Cult_CapacidadCampo
        decimal Cult_PorcentajeMaximo
        decimal Cult_DecisionRiego
        bit Cult_Activo
    }

    HidrtbSensorMeta {
        int Meta_Id PK
        nvarchar Sens_Nombre UK "serial Visualiti M###"
        int Cult_Id FK
        nvarchar Sens_Finca
        decimal Sens_CCEstimado
        nvarchar Sens_MetodoCC
        datetime2 Sens_FechaEstimacionCC
        bit Meta_Activo
    }

    HidrtbLog {
        int Id PK
        nvarchar Message
        nvarchar Level
        datetime TimeStamp
    }
```

## Explicación

### Geografía
- **HidrtbPais**: catálogo de países (Colombia, Ecuador, Honduras).
- **HidrtbDepartamento**: provincias/departamentos; pertenece a un país (`Pais_Id`).
- **HidrtbCiudad**: municipios/cantones; pertenece solo al departamento (`Depo_Id`). El país se obtiene vía Departamento → País.

### Usuarios e Identity
- **HidrtbUsuario**: usuario de la app; mapeado a `ApplicationUser : IdentityUser`. La PK `Usua_Id` es un GUID `nvarchar(450)` generado por Identity. Su ubicación es la ciudad (`Ciu_Id`).
- **HidrtbRol / HidrtbUsuarioRol**: roles Identity (`Admin`, `User`). Sembrados al inicio en `Program.cs`.
- **HidrtbUsuarioClaim / HidrtbUsuarioLogin / HidrtbUsuarioToken / HidrtbRolClaim**: tablas auxiliares de Identity.

### Sensores y riego
- **HidrtbRed**: red de referencia ligada a un país (p. ej. RED ASORUT, RED ECUADOR, RED HONDURA). El inventario vivo de estaciones está en Visualiti.
- **HidrtbCultivo**: parámetros de riego (capacidad de campo, % máximo, % decisión).
- **HidrtbSensorMeta**: metadatos Hidrix por serial Visualiti (cultivo, finca, CC estimada). Coordenadas, canales y estado operativo vienen de la API Visualiti.

### Soporte
- **HidrtbLog**: logs de aplicación (Serilog).

### Relaciones (FK)
| Desde | Hacia | Columna |
|-------|-------|---------|
| Departamento | País | `Pais_Id` |
| Ciudad | Departamento | `Depo_Id` |
| Usuario | Ciudad | `Ciu_Id` |
| UsuarioRol | Usuario | `UserId` |
| UsuarioRol | Rol | `RoleId` |
| Red | País | `Pais_Id` |
| SensorMeta | Cultivo | `Cult_Id` (nullable) |
