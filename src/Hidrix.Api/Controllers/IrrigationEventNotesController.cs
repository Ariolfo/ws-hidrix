using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Common.Models;
using Hidrix.Application.DTOs;
using Hidrix.Infrastructure.Identity;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Notas de evento de riego del usuario autenticado (cliente).
/// </summary>
[ApiController]
[Route("api/v1/irrigation-event-notes")]
[Authorize]
public class IrrigationEventNotesController : ControllerBase
{
    private readonly IIrrigationEventNoteService _service;

    /// <summary>Inicializa el controlador.</summary>
    public IrrigationEventNotesController(IIrrigationEventNoteService service)
    {
        _service = service;
    }

    /// <summary>Lista notas del usuario autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IrrigationEventNoteDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IrrigationEventNoteDto>>>> List(
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<IrrigationEventNoteDto>>.Fail("Usuario no autenticado"));
        }

        var data = await _service.ListAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<IrrigationEventNoteDto>>.Ok(data));
    }

    /// <summary>Registra una nota de evento de riego.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IrrigationEventNoteDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<IrrigationEventNoteDto>>> Create(
        [FromBody] CreateIrrigationEventNoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IrrigationEventNoteDto>.Fail("Usuario no autenticado"));
        }

        try
        {
            var data = await _service.CreateAsync(userId, request, cancellationToken);
            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<IrrigationEventNoteDto>.Ok(data, "Nota guardada"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<IrrigationEventNoteDto>.Fail(ex.Message));
        }
    }

    /// <summary>Descarga Excel con todas las notas en un rango de fechas (admin).</summary>
    [HttpGet("export")]
    [Authorize(Roles = AppRoles.Admin)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export(
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(from, out var fromDate))
        {
            return BadRequest(ApiResponse<object>.Fail("La fecha inicial no es válida."));
        }

        if (!DateOnly.TryParse(to, out var toDate))
        {
            return BadRequest(ApiResponse<object>.Fail("La fecha final no es válida."));
        }

        try
        {
            var bytes = await _service.ExportExcelAsync(fromDate, toDate, cancellationToken);
            var fileName = $"notas-evento-riego_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
