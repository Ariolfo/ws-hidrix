using System.Text.RegularExpressions;
using Hidrix.Application.Common.Interfaces;

namespace Hidrix.Application.Services;

/// <summary>
/// Sensor físico del inventario Visualiti (M###) con metadatos Hidrix opcionales.
/// </summary>
public sealed class PhysicalSensor
{
    /// <summary>
    /// Crea un sensor físico.
    /// </summary>
    public PhysicalSensor(
        string serial,
        string red,
        string? cultivo = null,
        string? finca = null,
        string? pais = null,
        double? latitud = null,
        double? longitud = null,
        int canales = SensorCatalog.DefaultChannels,
        int? countryId = null,
        string? timeZoneId = null,
        string? displayName = null,
        bool? online = null,
        string? connectivity = null,
        string? hardwareStatus = null,
        bool hasHardwareSnapshot = false)
    {
        Serial = serial;
        Red = red;
        Cultivo = cultivo;
        Finca = finca;
        Pais = pais;
        CountryId = countryId;
        TimeZoneId = timeZoneId
            ?? (countryId is int id
                ? CountryTimeZoneResolver.ResolveForCountryId(id)
                : CountryTimeZoneResolver.ResolveForCountryName(pais));
        Latitud = latitud;
        Longitud = longitud;
        Canales = canales;
        DisplayName = displayName;
        Online = online;
        Connectivity = connectivity;
        HardwareStatus = hardwareStatus;
        HasHardwareSnapshot = hasHardwareSnapshot;
    }

    /// <summary>Serial Visualiti (M###).</summary>
    public string Serial { get; }

    /// <summary>Red de sensores.</summary>
    public string Red { get; }

    /// <summary>Cultivo asociado.</summary>
    public string? Cultivo { get; }

    /// <summary>Nombre de finca.</summary>
    public string? Finca { get; }

    /// <summary>País.</summary>
    public string? Pais { get; }

    /// <summary>Id del país (catálogo geo).</summary>
    public int? CountryId { get; }

    /// <summary>Zona horaria IANA del país de la red.</summary>
    public string TimeZoneId { get; }

    /// <summary>Latitud WGS84 (Visualiti hardware-status).</summary>
    public double? Latitud { get; }

    /// <summary>Longitud WGS84 (Visualiti hardware-status).</summary>
    public double? Longitud { get; }

    /// <summary>Canales lógicos expuestos.</summary>
    public int Canales { get; }

    /// <summary>Nombre Visualiti del dispositivo (name_device).</summary>
    public string? DisplayName { get; }

    /// <summary>Estación online según Visualiti (null si desconocido).</summary>
    public bool? Online { get; }

    /// <summary>Conectividad textual (online/offline).</summary>
    public string? Connectivity { get; }

    /// <summary>Estado de hardware de sensores (bueno, aceptable, desconocido…).</summary>
    public string? HardwareStatus { get; }

    /// <summary>True si hubo snapshot Celery de hardware-status (false = 404 / sin datos).</summary>
    public bool HasHardwareSnapshot { get; }

    /// <summary>
    /// Copia con datos en vivo de Visualiti (canales, coords, estado).
    /// </summary>
    public PhysicalSensor WithLiveData(
        int? canales = null,
        double? latitud = null,
        double? longitud = null,
        bool? online = null,
        string? connectivity = null,
        string? hardwareStatus = null,
        bool? hasHardwareSnapshot = null) =>
        new(
            Serial,
            Red,
            Cultivo,
            Finca,
            Pais,
            latitud ?? Latitud,
            longitud ?? Longitud,
            canales ?? Canales,
            CountryId,
            TimeZoneId,
            DisplayName,
            online ?? Online,
            connectivity ?? Connectivity,
            hardwareStatus ?? HardwareStatus,
            hasHardwareSnapshot ?? HasHardwareSnapshot);
}

/// <summary>
/// Normaliza respuestas de GET …/hardware-status (incluye 404 sin snapshot).
/// </summary>
public static class VisualitiHardwareDefaults
{
    /// <summary>Conectividad cuando no hay snapshot Celery.</summary>
    public const string Offline = "offline";

    /// <summary>Estado de sensores cuando no hay snapshot.</summary>
    public const string Unknown = "desconocido";

    /// <summary>
    /// Aplica valores por defecto si hardware es null (404 / sin snapshot).
    /// </summary>
    public static (
        double? Latitude,
        double? Longitude,
        bool Online,
        string Connectivity,
        string HardwareStatus,
        bool HasSnapshot) From(VisualitiHardwareStatus? hardware)
    {
        if (hardware is null)
        {
            return (null, null, false, Offline, Unknown, false);
        }

        var online = hardware.Online
            ?? string.Equals(hardware.Connectivity, "online", StringComparison.OrdinalIgnoreCase);

        var connectivity = !string.IsNullOrWhiteSpace(hardware.Connectivity)
            ? hardware.Connectivity!.Trim().ToLowerInvariant()
            : (online ? "online" : Offline);

        var status = !string.IsNullOrWhiteSpace(hardware.SensorState)
            ? hardware.SensorState!.Trim().ToLowerInvariant()
            : Unknown;

        return (
            hardware.Latitude,
            hardware.Longitude,
            online,
            connectivity,
            status,
            true);
    }
}

/// <summary>
/// Utilidades de identificadores y distancia para sensores Visualiti M###.
/// </summary>
public static partial class SensorCatalog
{
    /// <summary>Canales lógicos por defecto si Visualiti no responde.</summary>
    public const int DefaultChannels = 2;

    private static readonly Regex LogicalIdRegex = MyLogicalIdRegex();

    /// <summary>
    /// Separa un ID lógico M316-1 en (M316, 1). Sin sufijo → canal null.
    /// </summary>
    public static (string Physical, int? Channel) SplitLogicalId(string sensorId)
    {
        var trimmed = sensorId.Trim();
        var match = LogicalIdRegex.Match(trimmed);
        if (match.Success)
        {
            return (match.Groups["physical"].Value, int.Parse(match.Groups["channel"].Value));
        }

        return (trimmed, null);
    }

    /// <summary>
    /// Cont Vol{n} → sensor lógico M###-n (sensor_n).
    /// </summary>
    public static string LogicalSensorId(string physicalSerial, int channel) =>
        $"{physicalSerial}-{channel}";

    /// <summary>
    /// Distancia en metros entre dos puntos WGS84 (Haversine).
    /// </summary>
    public static double HaversineM(double lat1, double lng1, double lat2, double lng2)
    {
        const double radius = 6371000.0;
        var phi1 = DegreesToRadians(lat1);
        var phi2 = DegreesToRadians(lat2);
        var dphi = DegreesToRadians(lat2 - lat1);
        var dlambda = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2)
                + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dlambda / 2) * Math.Sin(dlambda / 2);
        return 2 * radius * Math.Asin(Math.Sqrt(a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    [GeneratedRegex(@"^(?<physical>.+?)-(?<channel>\d+)$")]
    private static partial Regex MyLogicalIdRegex();
}
