using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Infrastructure.Options;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Health check del servicio y de la base de datos.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HealthOptions _healthOptions;

    /// <summary>
    /// Inicializa el controlador.
    /// </summary>
    public HealthController(IUnitOfWork unitOfWork, IOptions<HealthOptions> healthOptions)
    {
        _unitOfWork = unitOfWork;
        _healthOptions = healthOptions.Value;
    }

    /// <summary>
    /// Verifica estado del API. Detalle de BD solo con header X-Health-Key válido.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthDto>> Get(CancellationToken cancellationToken)
    {
        if (!HasDetailedAccess())
        {
            return Ok(new HealthDto { Status = "ok" });
        }

        var connected = false;
        try
        {
            connected = await _unitOfWork.CanConnectAsync(cancellationToken);
        }
        catch
        {
            connected = false;
        }

        return Ok(new HealthDto
        {
            Status = connected ? "ok" : "degraded",
            Database = connected ? "connected" : "unavailable",
        });
    }

    private bool HasDetailedAccess()
    {
        if (string.IsNullOrWhiteSpace(_healthOptions.DetailedKey))
        {
            return false;
        }

        return string.Equals(
            Request.Headers["X-Health-Key"].FirstOrDefault(),
            _healthOptions.DetailedKey,
            StringComparison.Ordinal);
    }
}
