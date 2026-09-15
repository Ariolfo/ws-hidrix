using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;

namespace Hidrix.Application.Features.Stations.Queries.GetStationSensors;

/// <summary>
/// Consulta sensores de una estación.
/// </summary>
public class GetStationSensorsQuery : IRequest<IReadOnlyList<SensorDto>>
{
    /// <summary>Id de estación (fin-... o sn-...).</summary>
    public string StationId { get; set; } = string.Empty;
}

/// <summary>
/// Handler de sensores por estación (inventario Visualiti).
/// </summary>
public class GetStationSensorsQueryHandler : IRequestHandler<GetStationSensorsQuery, IReadOnlyList<SensorDto>>
{
    private readonly IVisualitiClient _visualiti;
    private readonly IVisualitiStationInventory _inventory;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetStationSensorsQueryHandler(
        IVisualitiClient visualiti,
        IVisualitiStationInventory inventory)
    {
        _visualiti = visualiti;
        _inventory = inventory;
    }

    /// <summary>
    /// Obtiene sensores de la estación con lecturas en vivo.
    /// </summary>
    public async ValueTask<IReadOnlyList<SensorDto>> Handle(
        GetStationSensorsQuery request,
        CancellationToken cancellationToken)
    {
        string kind;
        string key;
        try
        {
            (kind, key) = StationIds.ParseStationId(request.StationId);
        }
        catch (ArgumentException)
        {
            throw new NotFoundException($"Estación no encontrada: {request.StationId}");
        }

        List<PhysicalSensor> sensors;
        string responseStationId;
        var all = await _inventory.ListSensorsAsync(cancellationToken);

        if (kind == "serial")
        {
            var sensor = all.FirstOrDefault(s =>
                             string.Equals(s.Serial, key, StringComparison.OrdinalIgnoreCase))
                         ?? await _inventory.GetSensorAsync(key, cancellationToken)
                         ?? throw new NotFoundException($"Estación no encontrada: {request.StationId}");
            sensors = [sensor];
            responseStationId = StationIds.EncodeStationId(key);
        }
        else
        {
            sensors = all
                .Where(s => s.Finca is not null && StationIds.FincaSlug(s.Finca) == key)
                .ToList();
            if (sensors.Count == 0)
            {
                throw new NotFoundException($"Estación no encontrada: {request.StationId}");
            }

            responseStationId = request.StationId;
        }

        var latest = new Dictionary<string, VisualitiReading?>(StringComparer.Ordinal);
        await Parallel.ForEachAsync(
            sensors.Select(s => s.Serial),
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken },
            async (serial, ct) =>
            {
                var reading = await _visualiti.GetLatestReadingCachedAsync(serial, ct);
                lock (latest)
                {
                    latest[serial] = reading;
                }
            });

        return sensors
            .SelectMany(s => SensorMapper.ToLogicalSensorDtos(
                s,
                responseStationId,
                latest.GetValueOrDefault(s.Serial)))
            .ToList();
    }
}
