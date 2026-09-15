namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Lectura de humedad obtenida desde Visualiti.
/// </summary>
public class VisualitiReading
{
    /// <summary>Marca de tiempo UTC de la lectura.</summary>
    public DateTimeOffset FechaHora { get; set; }

    /// <summary>Valores en porcentaje por canal lógico (1 = Cont Vol1, 2 = Cont Vol2).</summary>
    public Dictionary<int, double> Valores { get; set; } = new();

    /// <summary>Valor del canal 1 (Cont Vol1 / sensor_1).</summary>
    public double? Volumetrico1 => Valores.TryGetValue(1, out var v) ? v : null;

    /// <summary>Valor del canal 2 (Cont Vol2 / sensor_2).</summary>
    public double? Volumetrico2 => Valores.TryGetValue(2, out var v) ? v : null;

    /// <summary>
    /// Obtiene el valor de un canal.
    /// </summary>
    /// <param name="channel">Número de canal.</param>
    /// <returns>Porcentaje o null.</returns>
    public double? ValueForChannel(int channel) =>
        Valores.TryGetValue(channel, out var v) ? v : null;

    /// <summary>Canales presentes ordenados.</summary>
    public IReadOnlyList<int> Channels => Valores.Keys.OrderBy(k => k).ToList();
}

/// <summary>Estación/dispositivo del inventario dinámico (GET /api/devices).</summary>
public sealed class VisualitiDevice
{
    /// <summary>Id de plataforma Visualiti (normalmente 4).</summary>
    public int OrigenId { get; init; }

    /// <summary>Nombre de la red/origen.</summary>
    public string Origen { get; init; } = string.Empty;

    /// <summary>Id numérico de estación Visualiti.</summary>
    public int StationId { get; init; }

    /// <summary>Nombre del dispositivo (ej. LIMA1 SUELO M312).</summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>Serial Hidrix (M###).</summary>
    public string Serial => $"M{StationId}";
}

/// <summary>Sensores/canales actuales de una estación Visualiti (/sensor).</summary>
public sealed class VisualitiStationSensors
{
    /// <summary>Canales lógicos deduplicados (1=Cont Vol1, 2=Cont Vol2, …).</summary>
    public IReadOnlyList<int> Channels { get; init; } = [];
}

/// <summary>Estado de hardware de una estación (/hardware-status).</summary>
public sealed class VisualitiHardwareStatus
{
    /// <summary>Id numérico de estación.</summary>
    public int StationId { get; init; }

    /// <summary>Nombre descriptivo en APPGRICULTOR.</summary>
    public string? StationName { get; init; }

    /// <summary>Latitud WGS84.</summary>
    public double? Latitude { get; init; }

    /// <summary>Longitud WGS84.</summary>
    public double? Longitude { get; init; }

    /// <summary>Estación online según recepción reciente de datos.</summary>
    public bool? Online { get; init; }

    /// <summary>Estado general de sensores (bueno, …).</summary>
    public string? SensorState { get; init; }

    /// <summary>Conectividad textual (online/offline).</summary>
    public string? Connectivity { get; init; }
}

/// <summary>
/// Cliente HTTP hacia la API Visualiti (appgricultor).
/// </summary>
public interface IVisualitiClient
{
    /// <summary>
    /// Obtiene la última lectura.
    /// Si hay serie histórica en caché, Latest = último punto (sin llamada Visualiti).
    /// Si no, carga rango 7d (queda en caché) y toma el último punto.
    /// </summary>
    /// <param name="sensorSerial">Serial M### o lógico M###-n.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lectura o null.</returns>
    Task<VisualitiReading?> GetLatestReadingCachedAsync(
        string sensorSerial,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene lecturas de humedad desde una fecha (sin clave de rango; no usa caché por rango).
    /// Preferir <see cref="FetchMoistureReadingsForRangeAsync"/> cuando se conozca el rango.
    /// </summary>
    /// <param name="sensorSerial">Serial M### o lógico.</param>
    /// <param name="since">Inicio del rango (UTC).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de lecturas ordenadas.</returns>
    Task<IReadOnlyList<VisualitiReading>> FetchMoistureReadingsAsync(
        string sensorSerial,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene lecturas para un rango tipado (today|7d|30d|6m) con caché en memoria.
    /// </summary>
    /// <param name="sensorSerial">Serial M### o lógico.</param>
    /// <param name="rangeKey">Clave de rango.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de lecturas ordenadas.</returns>
    Task<IReadOnlyList<VisualitiReading>> FetchMoistureReadingsForRangeAsync(
        string sensorSerial,
        string rangeKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inventario dinámico de estaciones del cliente (GET /api/devices).
    /// </summary>
    Task<IReadOnlyList<VisualitiDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Canales/sensores actualmente asociados a la estación (GET …/sensor).
    /// </summary>
    Task<VisualitiStationSensors?> GetStationSensorsAsync(
        int stationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estado de hardware, coords y conectividad (GET …/hardware-status). Null si 404.
    /// </summary>
    Task<VisualitiHardwareStatus?> GetHardwareStatusAsync(
        int stationId,
        CancellationToken cancellationToken = default);
}
