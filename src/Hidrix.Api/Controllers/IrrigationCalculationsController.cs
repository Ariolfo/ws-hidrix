using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Common.Models;
using Hidrix.Application.DTOs;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Registros de la calculadora de riego del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/v1/irrigation-calculations")]
[Authorize]
public class IrrigationCalculationsController : ControllerBase
{
    private readonly IIrrigationCalculationService _service;

    /// <summary>Inicializa el controlador.</summary>
    public IrrigationCalculationsController(IIrrigationCalculationService service)
    {
        _service = service;
    }

    /// <summary>Lista registros del usuario (filtro opcional por cultivo).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IrrigationCalculationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IrrigationCalculationDto>>>> List(
        [FromQuery] string? crop = null,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<IrrigationCalculationDto>>.Fail("Usuario no autenticado"));
        }

        var data = await _service.ListAsync(userId, crop, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<IrrigationCalculationDto>>.Ok(data));
    }

    /// <summary>Guarda un registro de la calculadora.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IrrigationCalculationDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<IrrigationCalculationDto>>> Create(
        [FromBody] CreateIrrigationCalculationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IrrigationCalculationDto>.Fail("Usuario no autenticado"));
        }

        try
        {
            var data = await _service.CreateAsync(userId, request, cancellationToken);
            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<IrrigationCalculationDto>.Ok(data, "Registro guardado"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<IrrigationCalculationDto>.Fail(ex.Message));
        }
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
