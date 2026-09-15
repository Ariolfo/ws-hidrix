using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Services;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Combina catálogo local con /sensor y /hardware-status de Visualiti.
/// </summary>
public sealed class VisualitiStationEnricher : IVisualitiStationEnricher
{
    private readonly IVisualitiClient _visualiti;

    /// <summary>Inicializa el enricher.</summary>
    public VisualitiStationEnricher(IVisualitiClient visualiti)
    {
        _visualiti = visualiti;
    }

    /// <inheritdoc />
    public async Task<PhysicalSensor> EnrichAsync(
        PhysicalSensor sensor,
        CancellationToken cancellationToken = default)
    {
        var stationNum = VisualitiClient.ParseVisualitiStationId(sensor.Serial);
        if (stationNum is null)
        {
            return sensor;
        }

        var sensorsTask = _visualiti.GetStationSensorsAsync(stationNum.Value, cancellationToken);
        var hardwareTask = _visualiti.GetHardwareStatusAsync(stationNum.Value, cancellationToken);
        await Task.WhenAll(sensorsTask, hardwareTask).ConfigureAwait(false);

        return ApplyVisualiti(sensor, sensorsTask.Result, hardwareTask.Result);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PhysicalSensor>> EnrichManyAsync(
        IEnumerable<PhysicalSensor> sensors,
        CancellationToken cancellationToken = default)
    {
        var list = sensors.ToList();
        if (list.Count == 0)
        {
            return list;
        }

        var results = new PhysicalSensor[list.Count];
        await Parallel.ForEachAsync(
            list.Select((sensor, index) => (sensor, index)),
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                results[item.index] = await EnrichAsync(item.sensor, ct).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return results;
    }

    /// <summary>Aplica canales y hardware-status (404 → offline/desconocido).</summary>
    public static PhysicalSensor ApplyVisualiti(
        PhysicalSensor sensor,
        VisualitiStationSensors? liveSensors,
        VisualitiHardwareStatus? hardware)
    {
        var channelCount = liveSensors?.Channels.Count > 0
            ? liveSensors.Channels.Count
            : (int?)null;

        var snapshot = VisualitiHardwareDefaults.From(hardware);
        var lat = snapshot.Latitude ?? sensor.Latitud;
        var lng = snapshot.Longitude ?? sensor.Longitud;

        return sensor.WithLiveData(
            channelCount,
            lat,
            lng,
            snapshot.Online,
            snapshot.Connectivity,
            snapshot.HardwareStatus,
            snapshot.HasSnapshot);
    }
}
