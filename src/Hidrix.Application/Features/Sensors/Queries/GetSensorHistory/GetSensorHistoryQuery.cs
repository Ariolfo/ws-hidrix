using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;

namespace Hidrix.Application.Features.Sensors.Queries.GetSensorHistory;

/// <summary>
/// Consulta histórico de humedad desde Visualiti (con caché por rango).
/// </summary>
public class GetSensorHistoryQuery : IRequest<IReadOnlyList<HistoryPointDto>>
{
    /// <summary>Id M316 o M316-1.</summary>
    public string SensorId { get; set; } = string.Empty;

    /// <summary>Rango: today|7d|30d|6m.</summary>
    public string Range { get; set; } = "7d";
}

/// <summary>
/// Handler de histórico de sensor.
/// </summary>
public class GetSensorHistoryQueryHandler : IRequestHandler<GetSensorHistoryQuery, IReadOnlyList<HistoryPointDto>>
{
    private readonly IVisualitiClient _visualiti;
    private readonly IVisualitiStationInventory _inventory;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetSensorHistoryQueryHandler(IVisualitiClient visualiti, IVisualitiStationInventory inventory)
    {
        _visualiti = visualiti;
        _inventory = inventory;
    }

    /// <summary>
    /// Obtiene puntos Cont Vol1/Vol2 (sensor_1 / sensor_2).
    /// </summary>
    public async ValueTask<IReadOnlyList<HistoryPointDto>> Handle(
        GetSensorHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!HistoryRangeHelper.IsValid(request.Range))
        {
            throw new AppException($"Rango inválido: {request.Range}");
        }

        var sensor = await _inventory.GetSensorAsync(request.SensorId, cancellationToken)
                     ?? throw new NotFoundException($"Sensor no encontrado: {request.SensorId}");

        var readings = await _visualiti.FetchMoistureReadingsForRangeAsync(
            sensor.Serial,
            request.Range,
            cancellationToken);

        return readings
            .Select(r => new HistoryPointDto
            {
                Timestamp = r.FechaHora,
                Depth10cm = r.Volumetrico1 ?? 0.0,
                Depth30cm = r.Volumetrico2 ?? 0.0,
            })
            .ToList();
    }
}
