using Hidrix.Application.DTOs;

namespace Hidrix.Application.Common.Interfaces;

/// <summary>Notas de evento de riego por usuario.</summary>
public interface IIrrigationEventNoteService
{
    /// <summary>Lista notas del usuario autenticado.</summary>
    Task<IReadOnlyList<IrrigationEventNoteDto>> ListAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>Crea una nota para el usuario autenticado.</summary>
    Task<IrrigationEventNoteDto> CreateAsync(
        string userId,
        CreateIrrigationEventNoteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Exporta todas las notas activas en un rango de fechas (admin).</summary>
    Task<byte[]> ExportExcelAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}
