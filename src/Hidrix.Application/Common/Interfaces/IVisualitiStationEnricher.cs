using Hidrix.Application.Services;

namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Enriquece sensores del catálogo local con datos en vivo de Visualiti (canales, coords, estado).
/// </summary>
public interface IVisualitiStationEnricher
{
    /// <summary>
    /// Aplica /sensor y /hardware-status sobre un sensor del catálogo.
    /// </summary>
    Task<PhysicalSensor> EnrichAsync(PhysicalSensor sensor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriquece varios sensores en paralelo (máx. 8 concurrentes).
    /// </summary>
    Task<IReadOnlyList<PhysicalSensor>> EnrichManyAsync(
        IEnumerable<PhysicalSensor> sensors,
        CancellationToken cancellationToken = default);
}
