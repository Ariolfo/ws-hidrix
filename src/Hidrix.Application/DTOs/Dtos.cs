namespace Hidrix.Application.DTOs;

using Hidrix.Application.Services;

/// <summary>Usuario expuesto en autenticación.</summary>
public class UserDto
{
    /// <summary>Identificador (GUID de Identity).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre para mostrar.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Correo electrónico.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Roles asignados.</summary>
    public string[] Roles { get; set; } = [];

    /// <summary>País canónico (al registrar).</summary>
    public string? Country { get; set; }

    /// <summary>Departamento canónico (al registrar).</summary>
    public string? Department { get; set; }

    /// <summary>Ciudad canónica (al registrar).</summary>
    public string? City { get; set; }

    /// <summary>Id del país (catálogo geo).</summary>
    public int? CountryId { get; set; }

    /// <summary>Id del departamento (catálogo geo).</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Id de la ciudad (catálogo geo).</summary>
    public int? CityId { get; set; }
}

/// <summary>Ubicación geográfica del usuario autenticado (ids del catálogo).</summary>
public class UserLocationDto
{
    /// <summary>Id del país.</summary>
    public int? CountryId { get; set; }

    /// <summary>Id del departamento.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Id de la ciudad.</summary>
    public int? CityId { get; set; }

    /// <summary>Nombre del país.</summary>
    public string? CountryName { get; set; }

    /// <summary>Nombre del departamento.</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Nombre de la ciudad.</summary>
    public string? CityName { get; set; }
}

/// <summary>Respuesta de registro / login.</summary>
public class AuthDto
{
    /// <summary>JWT de acceso.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>JWT de refresco.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Datos del usuario.</summary>
    public UserDto User { get; set; } = new();

    /// <summary>Indica que debe confirmar el correo antes de iniciar sesión.</summary>
    public bool EmailConfirmationRequired { get; set; }
}

/// <summary>Respuesta de refresh de tokens.</summary>
public class RefreshDto
{
    /// <summary>Nuevo JWT de acceso.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Nuevo JWT de refresco.</summary>
    public string? RefreshToken { get; set; }
}

/// <summary>País del catálogo geo.</summary>
public class GeoCountryDto
{
    /// <summary>Identificador.</summary>
    public int Id { get; set; }

    /// <summary>Nombre.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Departamento del catálogo geo.</summary>
public class GeoDepartmentDto
{
    /// <summary>Identificador.</summary>
    public int Id { get; set; }

    /// <summary>Código.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Id del país.</summary>
    public int PaisId { get; set; }
}

/// <summary>Ciudad del catálogo geo.</summary>
public class GeoCityDto
{
    /// <summary>Identificador.</summary>
    public int Id { get; set; }

    /// <summary>Código.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Id del departamento.</summary>
    public int DepoId { get; set; }
}

/// <summary>Lectura de humedad por canal (sensor_1 / sensor_2).</summary>
public class ReadingDto
{
    /// <summary>Canal lógico como código de profundidad (10 = sensor_1, 30 = sensor_2).</summary>
    public int DepthCm { get; set; }

    /// <summary>Valor en porcentaje.</summary>
    public double Value { get; set; }

    /// <summary>Marca de tiempo.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Unidad (siempre %).</summary>
    public string Unit { get; set; } = "%";
}

/// <summary>Sensor físico o lógico.</summary>
public class SensorDto
{
    /// <summary>Id (M316 o M316-1).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Id de estación (fin-... o sn-...).</summary>
    public string StationId { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ubicación textual.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Estado de humedad: normal, drain, irrigate_deficit, no_data.</summary>
    public string Status { get; set; } = "no_data";

    /// <summary>Conectividad de estación Visualiti (online/offline).</summary>
    public string? Connectivity { get; set; }

    /// <summary>Estación online según hardware-status.</summary>
    public bool? Online { get; set; }

    /// <summary>Estado de hardware de sensores Visualiti (bueno, desconocido…).</summary>
    public string? HardwareStatus { get; set; }

    /// <summary>Última lectura.</summary>
    public DateTimeOffset LastReadingAt { get; set; }

    /// <summary>Lecturas por canal (sensor_1 / sensor_2).</summary>
    public List<ReadingDto> Readings { get; set; } = new();

    /// <summary>Mensaje de alerta opcional.</summary>
    public string? AlertMessage { get; set; }

    /// <summary>Latitud (Visualiti hardware-status).</summary>
    public double? Latitude { get; set; }

    /// <summary>Longitud (Visualiti hardware-status).</summary>
    public double? Longitude { get; set; }

    /// <summary>Zona horaria IANA del país de la red (p. ej. America/Bogota).</summary>
    public string TimeZoneId { get; set; } = CountryTimeZoneResolver.DefaultTimeZoneId;
}

/// <summary>Estación (finca o sensor suelto).</summary>
public class StationDto
{
    /// <summary>Id de estación.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Latitud media (desde hardware-status).</summary>
    public double Latitude { get; set; }

    /// <summary>Longitud media (desde hardware-status).</summary>
    public double Longitude { get; set; }

    /// <summary>Cantidad de sensores lógicos.</summary>
    public int SensorCount { get; set; }

    /// <summary>Distancia en km al punto de consulta.</summary>
    public double? DistanceKm { get; set; }

    /// <summary>True si alguna estación del grupo está online.</summary>
    public bool? Online { get; set; }

    /// <summary>Conectividad agregada (online si alguna online; si no offline).</summary>
    public string? Connectivity { get; set; }

    /// <summary>Estado de hardware agregado (peor estado del grupo o desconocido).</summary>
    public string? HardwareStatus { get; set; }

    /// <summary>Sensores anidados (opcional).</summary>
    public List<SensorDto> Sensors { get; set; } = new();
}

/// <summary>Punto de histórico de humedad.</summary>
public class HistoryPointDto
{
    /// <summary>Marca de tiempo.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Humedad Cont Vol1 (sensor_1).</summary>
    public double Depth10cm { get; set; }

    /// <summary>Humedad Cont Vol2 (sensor_2).</summary>
    public double Depth30cm { get; set; }
}

/// <summary>
/// Detalle de sensor más histórico en una sola respuesta (optimización de carga de gráfica).
/// </summary>
public class SensorWithHistoryDto
{
    /// <summary>Sensor con última lectura derivada del histórico.</summary>
    public SensorDto Sensor { get; set; } = new();

    /// <summary>Serie histórica del rango solicitado.</summary>
    public List<HistoryPointDto> History { get; set; } = new();

    /// <summary>Rango aplicado (today|7d|30d|6m).</summary>
    public string Range { get; set; } = "7d";
}

/// <summary>Respuesta de salud del servicio.</summary>
public class HealthDto
{
    /// <summary>Estado general (ok|degraded).</summary>
    public string Status { get; set; } = "ok";

    /// <summary>Estado de la base (connected|unavailable). Null si no se expone públicamente.</summary>
    public string? Database { get; set; }
}
