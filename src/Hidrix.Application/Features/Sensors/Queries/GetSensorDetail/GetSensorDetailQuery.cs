using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;

namespace Hidrix.Application.Features.Sensors.Queries.GetSensorDetail;

/// <summary>
/// Consulta detalle de un sensor físico o lógico.
/// </summary>
public class GetSensorDetailQuery : IRequest<SensorDto>
{
    /// <summary>Id M316 o M316-1.</summary>
    public string SensorId { get; set; } = string.Empty;
}

/// <summary>
/// Handler de detalle de sensor.
/// </summary>
public class GetSensorDetailQueryHandler : IRequestHandler<GetSensorDetailQuery, SensorDto>
{
    private readonly IVisualitiClient _visualiti;
    private readonly IVisualitiStationInventory _inventory;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetSensorDetailQueryHandler(
        IVisualitiClient visualiti,
        IVisualitiStationInventory inventory)
    {
        _visualiti = visualiti;
        _inventory = inventory;
    }

    /// <summary>
    /// Obtiene el sensor con última lectura.
    /// </summary>
    public async ValueTask<SensorDto> Handle(GetSensorDetailQuery request, CancellationToken cancellationToken)
    {
        var sensor = await _inventory.GetSensorAsync(request.SensorId, cancellationToken)
                     ?? throw new NotFoundException($"Sensor no encontrado: {request.SensorId}");

        var (_, channel) = SensorCatalog.SplitLogicalId(request.SensorId);
        var reading = await _visualiti.GetLatestReadingCachedAsync(sensor.Serial, cancellationToken);
        var stationId = StationIds.EncodeStationId(StationIds.GrupoFromSensor(sensor.Serial, sensor.Finca));
        return SensorMapper.ToSensorDto(sensor, stationId, reading, channel);
    }
}
