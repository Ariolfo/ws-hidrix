using Hidrix.Application.Services;

namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Caché en memoria compartida (singleton) de sesión Visualiti:
/// token, lecturas, devices, meta de estación e inventario construido.
/// </summary>
public interface IVisualitiMoistureCache
{
    /// <summary>Intenta obtener el token Bearer vigente.</summary>
    bool TryGetToken(out string token);

    /// <summary>Guarda el token con su expiración absoluta.</summary>
    void SetToken(string token, DateTimeOffset expiresAt);

    /// <summary>Ejecuta factory bajo candado exclusivo (p. ej. login).</summary>
    Task<T> RunExclusiveAsync<T>(
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>Última lectura cacheada del serial físico.</summary>
    bool TryGetLatest(string physicalSerial, out VisualitiReading? reading);

    /// <summary>Guarda la última lectura con TTL.</summary>
    void SetLatest(string physicalSerial, VisualitiReading? reading, TimeSpan ttl);

    /// <summary>Histórico cacheado por serial y rango.</summary>
    bool TryGetHistory(
        string physicalSerial,
        string rangeKey,
        out IReadOnlyList<VisualitiReading> readings);

    /// <summary>Guarda el histórico con TTL.</summary>
    void SetHistory(
        string physicalSerial,
        string rangeKey,
        IReadOnlyList<VisualitiReading> readings,
        TimeSpan ttl);

    /// <summary>Último punto de históricos cacheados del serial.</summary>
    VisualitiReading? FindLatestFromHistory(string physicalSerial);

    /// <summary>Inventario GET /api/devices (vigente).</summary>
    bool TryGetDevices(out IReadOnlyList<VisualitiDevice> devices);

    /// <summary>Guarda inventario de devices.</summary>
    void SetDevices(IReadOnlyList<VisualitiDevice> devices, TimeSpan ttl);

    /// <summary>Canales de estación (vigente).</summary>
    bool TryGetStationSensors(int stationId, out VisualitiStationSensors sensors);

    /// <summary>Guarda canales de estación.</summary>
    void SetStationSensors(int stationId, VisualitiStationSensors sensors, TimeSpan ttl);

    /// <summary>
    /// Hardware-status. <paramref name="found"/> es true aunque el valor sea null (404 cacheado).
    /// </summary>
    bool TryGetHardware(int stationId, out VisualitiHardwareStatus? hardware, out bool found);

    /// <summary>Guarda hardware-status (null = 404 / sin snapshot).</summary>
    void SetHardware(int stationId, VisualitiHardwareStatus? hardware, TimeSpan ttl);

    /// <summary>
    /// Inventario físico construido. Si <paramref name="allowStale"/>, acepta entradas
    /// dentro de la ventana hard-expire configurada al guardar.
    /// </summary>
    bool TryGetInventory(
        out IReadOnlyList<PhysicalSensor> inventory,
        out bool isStale,
        bool allowStale = false);

    /// <summary>Guarda inventario físico (fresh TTL + ventana stale adicional).</summary>
    void SetInventory(
        IReadOnlyList<PhysicalSensor> inventory,
        TimeSpan ttl,
        TimeSpan staleWindow);
}
