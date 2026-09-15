using System.Text.RegularExpressions;

namespace Hidrix.Application.Services;

/// <summary>
/// Sensor físico del catálogo estático (estación Visualiti M###).
/// </summary>
public sealed class PhysicalSensor
{
    /// <summary>
    /// Crea un sensor físico del catálogo.
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
        string? timeZoneId = null)
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

    /// <summary>Latitud WGS84.</summary>
    public double? Latitud { get; }

    /// <summary>Longitud WGS84.</summary>
    public double? Longitud { get; }

    /// <summary>Canales lógicos expuestos.</summary>
    public int Canales { get; }

    /// <summary>
    /// Copia con datos en vivo de Visualiti (canales, coordenadas).
    /// </summary>
    public PhysicalSensor WithLiveData(
        int? canales = null,
        double? latitud = null,
        double? longitud = null) =>
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
            TimeZoneId);
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
    /// <param name="sensorId">Id físico o lógico.</param>
    /// <returns>Serial físico y canal opcional.</returns>
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
    /// <param name="physicalSerial">Serial físico.</param>
    /// <param name="channel">Canal.</param>
    /// <returns>Id lógico.</returns>
    public static string LogicalSensorId(string physicalSerial, int channel) =>
        $"{physicalSerial}-{channel}";

    /// <summary>
    /// Distancia en metros entre dos puntos WGS84 (Haversine).
    /// </summary>
    /// <param name="lat1">Latitud origen.</param>
    /// <param name="lng1">Longitud origen.</param>
    /// <param name="lat2">Latitud destino.</param>
    /// <param name="lng2">Longitud destino.</param>
    /// <returns>Distancia en metros.</returns>
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
