using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;

namespace Hidrix.Application.Features.Sensors.Queries.GetSensorWithHistory;

/// <summary>
/// Consulta unificada: detalle del sensor + histórico en un solo viaje a Visualiti (CQRS).
/// </summary>
public class GetSensorWithHistoryQuery : IRequest<SensorWithHistoryDto>
{
    /// <summary>Id M316 o M316-1.</summary>
    public string SensorId { get; set; } = string.Empty;

    /// <summary>Rango: today|7d|30d|6m.</summary>
    public string Range { get; set; } = "7d";
}

/// <summary>
/// Handler de detalle + histórico (un fetch Visualiti por rango).
/// </summary>
public class GetSensorWithHistoryQueryHandler
    : IRequestHandler<GetSensorWithHistoryQuery, SensorWithHistoryDto>
{
    private readonly IVisualitiClient _visualiti;
    private readonly IVisualitiStationInventory _inventory;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetSensorWithHistoryQueryHandler(
        IVisualitiClient visualiti,
        IVisualitiStationInventory inventory)
    {
        _visualiti = visualiti;
        _inventory = inventory;
    }

    /// <summary>
    /// Obtiene sensor (última lectura = último punto) e histórico del rango.
    /// </summary>
    public async ValueTask<SensorWithHistoryDto> Handle(
        GetSensorWithHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!HistoryRangeHelper.IsValid(request.Range))
        {
            throw new AppException($"Rango inválido: {request.Range}");
        }

        var sensor = await _inventory.GetSensorAsync(request.SensorId, cancellationToken)
                     ?? throw new NotFoundException($"Sensor no encontrado: {request.SensorId}");

        var (_, channel) = SensorCatalog.SplitLogicalId(request.SensorId);
        var readings = await _visualiti.FetchMoistureReadingsForRangeAsync(
            sensor.Serial,
            request.Range,
            cancellationToken);

        var latest = readings.LastOrDefault();
        var stationId = StationIds.EncodeStationId(
            StationIds.GrupoFromSensor(sensor.Serial, sensor.Finca));

        var history = readings
            .Select(r => new HistoryPointDto
            {
                Timestamp = r.FechaHora,
                Depth10cm = r.Volumetrico1 ?? 0.0,
                Depth30cm = r.Volumetrico2 ?? 0.0,
            })
            .ToList();

        return new SensorWithHistoryDto
        {
            Sensor = SensorMapper.ToSensorDto(sensor, stationId, latest, channel),
            History = history,
            Range = HistoryRangeHelper.Normalize(request.Range),
        };
    }
}
