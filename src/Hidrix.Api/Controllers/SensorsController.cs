using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hidrix.Application.Common.Models;
using Hidrix.Application.DTOs;
using Hidrix.Application.Features.Sensors.Queries.GetSensorDetail;
using Hidrix.Application.Features.Sensors.Queries.GetSensorHistory;
using Hidrix.Application.Features.Sensors.Queries.GetSensorWithHistory;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Endpoints de sensores e histórico Visualiti.
/// </summary>
[ApiController]
[Route("api/v1/sensors")]
[Authorize]
public class SensorsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Inicializa el controlador.
    /// </summary>
    /// <param name="mediator">Mediator.</param>
    public SensorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Detalle de sensor físico o lógico (M316 / M316-1).
    /// </summary>
    /// <param name="sensorId">Identificador del sensor.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{sensorId}")]
    [ProducesResponseType(typeof(ApiResponse<SensorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SensorDto>>> GetDetail(
        string sensorId,
        CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(
            new GetSensorDetailQuery { SensorId = sensorId },
            cancellationToken);
        return Ok(ApiResponse<SensorDto>.Ok(data));
    }

    /// <summary>
    /// Detalle + histórico en una sola respuesta (un viaje a Visualiti por rango).
    /// </summary>
    /// <param name="sensorId">Identificador del sensor.</param>
    /// <param name="range">Rango today|7d|30d|6m.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{sensorId}/with-history")]
    [ProducesResponseType(typeof(ApiResponse<SensorWithHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SensorWithHistoryDto>>> GetWithHistory(
        string sensorId,
        [FromQuery] string range = "7d",
        CancellationToken cancellationToken = default)
    {
        var data = await _mediator.Send(
            new GetSensorWithHistoryQuery { SensorId = sensorId, Range = range },
            cancellationToken);
        return Ok(ApiResponse<SensorWithHistoryDto>.Ok(data));
    }

    /// <summary>
    /// Histórico de humedad Cont Vol1/Vol2 (sensor_1 / sensor_2).
    /// </summary>
    /// <param name="sensorId">Identificador del sensor.</param>
    /// <param name="range">Rango today|7d|30d|6m.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{sensorId}/history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HistoryPointDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HistoryPointDto>>>> GetHistory(
        string sensorId,
        [FromQuery] string range = "7d",
        CancellationToken cancellationToken = default)
    {
        var data = await _mediator.Send(
            new GetSensorHistoryQuery { SensorId = sensorId, Range = range },
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<HistoryPointDto>>.Ok(data));
    }
}
