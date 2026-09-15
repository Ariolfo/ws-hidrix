using Hidrix.Application.Services;

namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Inventario de estaciones/sensores construido desde Visualiti (GET /api/devices + meta).
/// Fusiona metadatos locales (finca, cultivo) cuando existen en HidrtbSensorMeta.
/// </summary>
public interface IVisualitiStationInventory
{
    /// <summary>
    /// Lista sensores físicos enriquecidos desde Visualiti.
    /// </summary>
    Task<IReadOnlyList<PhysicalSensor>> ListSensorsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un sensor por serial físico o lógico (M316 / M316-1).
    /// </summary>
    Task<PhysicalSensor?> GetSensorAsync(
        string sensorId,
        CancellationToken cancellationToken = default);
}
