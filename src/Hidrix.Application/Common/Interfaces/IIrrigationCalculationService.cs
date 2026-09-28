using Hidrix.Application.DTOs;

namespace Hidrix.Application.Common.Interfaces;

/// <summary>Registros de la calculadora de riego por usuario.</summary>
public interface IIrrigationCalculationService
{
    /// <summary>Lista registros del usuario, opcionalmente filtrados por cultivo.</summary>
    Task<IReadOnlyList<IrrigationCalculationDto>> ListAsync(
        string userId,
        string? cropName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Crea un registro para el usuario autenticado.</summary>
    Task<IrrigationCalculationDto> CreateAsync(
        string userId,
        CreateIrrigationCalculationRequest request,
        CancellationToken cancellationToken = default);
}
