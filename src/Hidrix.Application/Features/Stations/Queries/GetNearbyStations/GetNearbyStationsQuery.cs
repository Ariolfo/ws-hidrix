using Mediator;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;

namespace Hidrix.Application.Features.Stations.Queries.GetNearbyStations;

/// <summary>
/// Consulta estaciones geolocalizadas (cercanas o catálogo completo).
/// </summary>
public class GetNearbyStationsQuery : IRequest<IReadOnlyList<StationDto>>
{
    /// <summary>Latitud del usuario (para distancia; opcional si AllGeolocated).</summary>
    public double Lat { get; set; }

    /// <summary>Longitud del usuario.</summary>
    public double Lng { get; set; }

    /// <summary>Radio en km (default 50). Ignorado si AllGeolocated.</summary>
    public double Radius { get; set; } = 50;

    /// <summary>Si true, incluye sensores con lecturas cacheadas.</summary>
    public bool IncludeSensors { get; set; }

    /// <summary>Si true, devuelve todas las estaciones geolocalizadas (CO/EC/HN).</summary>
    public bool AllGeolocated { get; set; }
}

/// <summary>
/// Handler de estaciones (inventario Visualiti).
/// </summary>
public class GetNearbyStationsQueryHandler : IRequestHandler<GetNearbyStationsQuery, IReadOnlyList<StationDto>>
{
    private readonly IVisualitiClient _visualiti;
    private readonly IVisualitiStationInventory _inventory;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetNearbyStationsQueryHandler(
        IVisualitiClient visualiti,
        IVisualitiStationInventory inventory)
    {
        _visualiti = visualiti;
        _inventory = inventory;
    }

    /// <summary>
    /// Obtiene estaciones dentro del radio o el catálogo completo.
    /// </summary>
    public async ValueTask<IReadOnlyList<StationDto>> Handle(
        GetNearbyStationsQuery request,
        CancellationToken cancellationToken)
    {
        var allSensors = await _inventory.ListSensorsAsync(cancellationToken);
        var geolocated = allSensors
            .Where(s => s.Latitud is not null && s.Longitud is not null)
            .ToList();

        IEnumerable<(PhysicalSensor Sensor, double Dist)> scored;
        if (request.AllGeolocated)
        {
            scored = geolocated.Select(s => (
                Sensor: s,
                Dist: SensorCatalog.HaversineM(
                    request.Lat, request.Lng, s.Latitud!.Value, s.Longitud!.Value)));
        }
        else
        {
            var radiusKm = Math.Clamp(request.Radius <= 0 ? 50 : request.Radius, 0.1, 500);
            var radiusM = radiusKm * 1000.0;
            scored = geolocated
                .Select(s => (
                    Sensor: s,
                    Dist: SensorCatalog.HaversineM(
                        request.Lat, request.Lng, s.Latitud!.Value, s.Longitud!.Value)))
                .Where(x => x.Dist <= radiusM);
        }

        var nearby = scored.OrderBy(x => x.Dist).ToList();

        var grouped = new Dictionary<string, (string Nombre, List<PhysicalSensor> Sensors, double MinDist)>(
            StringComparer.Ordinal);

        foreach (var (sensor, dist) in nearby)
        {
            var grupo = StationIds.GrupoFromSensor(sensor.Serial, sensor.Finca);
            if (!grouped.TryGetValue(grupo, out var entry))
            {
                var nombre = !string.IsNullOrWhiteSpace(sensor.Finca)
                    ? grupo
                    : (!string.IsNullOrWhiteSpace(sensor.DisplayName)
                        ? sensor.DisplayName!
                        : $"Sensor {sensor.Serial}");
                grouped[grupo] = (nombre, [sensor], dist);
            }
            else
            {
                entry.Sensors.Add(sensor);
                grouped[grupo] = (entry.Nombre, entry.Sensors, Math.Min(entry.MinDist, dist));
            }
        }

        var stations = new List<StationDto>();
        foreach (var (grupo, entry) in grouped)
        {
            var sensors = entry.Sensors;
            var (online, connectivity, hardwareStatus) = AggregateHardware(sensors);
            stations.Add(new StationDto
            {
                Id = StationIds.EncodeStationId(grupo),
                Name = entry.Nombre,
                Latitude = Math.Round(sensors.Average(s => s.Latitud!.Value), 7),
                Longitude = Math.Round(sensors.Average(s => s.Longitud!.Value), 7),
                SensorCount = sensors.Sum(s => s.Canales),
                DistanceKm = Math.Round(entry.MinDist / 1000.0, 2),
                Online = online,
                Connectivity = connectivity,
                HardwareStatus = hardwareStatus,
                Sensors = [],
            });
        }

        stations = stations.OrderBy(s => s.DistanceKm ?? 0).ToList();

        if (!request.IncludeSensors)
        {
            return stations;
        }

        var allSerials = grouped.Values.SelectMany(e => e.Sensors.Select(s => s.Serial)).Distinct().ToList();
        var latest = new Dictionary<string, VisualitiReading?>(StringComparer.Ordinal);
        await Parallel.ForEachAsync(
            allSerials,
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken },
            async (serial, ct) =>
            {
                var reading = await _visualiti.GetLatestReadingCachedAsync(serial, ct);
                lock (latest)
                {
                    latest[serial] = reading;
                }
            });

        var sensorsByStation = new Dictionary<string, List<SensorDto>>(StringComparer.Ordinal);
        foreach (var (grupo, entry) in grouped)
        {
            var stationId = StationIds.EncodeStationId(grupo);
            sensorsByStation[stationId] = entry.Sensors
                .SelectMany(s => SensorMapper.ToLogicalSensorDtos(
                    s,
                    stationId,
                    latest.GetValueOrDefault(s.Serial)))
                .ToList();
        }

        foreach (var station in stations)
        {
            station.Sensors = sensorsByStation.GetValueOrDefault(station.Id) ?? [];
        }

        return stations;
    }

    private static (bool? Online, string Connectivity, string HardwareStatus) AggregateHardware(
        IReadOnlyList<PhysicalSensor> sensors)
    {
        if (sensors.Count == 0)
        {
            return (false, VisualitiHardwareDefaults.Offline, VisualitiHardwareDefaults.Unknown);
        }

        var anyOnline = sensors.Any(s => s.Online == true);
        var allOffline = sensors.All(s => s.Online == false);
        bool? online = anyOnline ? true : (allOffline ? false : null);

        var connectivity = anyOnline
            ? "online"
            : (sensors.Select(s => s.Connectivity).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))
               ?? VisualitiHardwareDefaults.Offline);

        var hardwareStatus = sensors
            .Select(s => s.HardwareStatus)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .DefaultIfEmpty(VisualitiHardwareDefaults.Unknown)
            .OrderByDescending(RankHardwareStatus)
            .First()!;

        return (online, connectivity, hardwareStatus);
    }

    private static int RankHardwareStatus(string? status) =>
        (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "malo" or "critico" or "crítico" => 3,
            "aceptable" or "regular" => 2,
            "bueno" => 1,
            _ => 0,
        };
}
