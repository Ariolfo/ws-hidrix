using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hidrix.Application.Common.Models;
using Hidrix.Application.DTOs;
using Hidrix.Application.Features.Stations.Queries.GetNearbyStations;
using Hidrix.Application.Features.Stations.Queries.GetStationSensors;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Endpoints de estaciones (agrupación por finca / serial).
/// </summary>
[ApiController]
[Route("api/v1/stations")]
[Authorize]
public class StationsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Inicializa el controlador.
    /// </summary>
    public StationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lista estaciones cercanas o el catálogo geolocalizado completo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StationDto>>>> GetNearby(
        [FromQuery] double lat = 0,
        [FromQuery] double lng = 0,
        [FromQuery] double radius = 50,
        [FromQuery] bool includeSensors = false,
        [FromQuery] bool all = false,
        CancellationToken cancellationToken = default)
    {
        var data = await _mediator.Send(
            new GetNearbyStationsQuery
            {
                Lat = lat,
                Lng = lng,
                Radius = radius,
                IncludeSensors = includeSensors,
                AllGeolocated = all,
            },
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StationDto>>.Ok(data));
    }

    /// <summary>
    /// Lista sensores de una estación (fin-... o sn-...).
    /// </summary>
    /// <param name="stationId">Id de estación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{stationId}/sensors")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SensorDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SensorDto>>>> GetStationSensors(
        string stationId,
        CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(
            new GetStationSensorsQuery { StationId = stationId },
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SensorDto>>.Ok(data));
    }
}
