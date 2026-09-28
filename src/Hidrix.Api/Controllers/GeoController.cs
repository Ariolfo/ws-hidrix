using Mediator;
using Microsoft.AspNetCore.Mvc;
using Hidrix.Application.Common.Models;
using Hidrix.Application.DTOs;
using Hidrix.Application.Features.Geo.Queries.GetCities;
using Hidrix.Application.Features.Geo.Queries.GetCountries;
using Hidrix.Application.Features.Geo.Queries.GetDepartments;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Endpoints de catálogo geográfico.
/// </summary>
[ApiController]
[Route("api/v1/geo")]
public class GeoController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Inicializa el controlador.
    /// </summary>
    public GeoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lista países.
    /// </summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GeoCountryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GeoCountryDto>>>> GetCountries(
        CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(new GetCountriesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GeoCountryDto>>.Ok(data));
    }

    /// <summary>
    /// Lista departamentos de un país.
    /// </summary>
    /// <param name="paisId">Id del país.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("countries/{paisId:int}/departments")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GeoDepartmentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GeoDepartmentDto>>>> GetDepartments(
        int paisId,
        CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(new GetDepartmentsQuery { PaisId = paisId }, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GeoDepartmentDto>>.Ok(data));
    }

    /// <summary>
    /// Lista ciudades de un departamento.
    /// </summary>
    /// <param name="depoId">Id del departamento.</param>
    /// <param name="q">Filtro opcional.</param>
    /// <param name="limit">Límite de resultados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("departments/{depoId:int}/cities")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GeoCityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GeoCityDto>>>> GetCities(
        int depoId,
        [FromQuery] string? q,
        [FromQuery] int limit = 2000,
        CancellationToken cancellationToken = default)
    {
        var data = await _mediator.Send(
            new GetCitiesQuery { DepoId = depoId, Q = q, Limit = limit },
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GeoCityDto>>.Ok(data));
    }
}
