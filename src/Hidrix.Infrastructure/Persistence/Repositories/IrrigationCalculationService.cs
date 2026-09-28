using Microsoft.EntityFrameworkCore;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Persistence.Repositories;

/// <summary>Persistencia de registros de la calculadora de riego.</summary>
public sealed class IrrigationCalculationService : IIrrigationCalculationService
{
    private readonly HidrixDbContext _db;

    /// <summary>Inicializa el servicio.</summary>
    public IrrigationCalculationService(HidrixDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IrrigationCalculationDto>> ListAsync(
        string userId,
        string? cropName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CalculosRiego.AsNoTracking()
            .Where(c => c.CalcActivo && c.UsuaId == userId);

        if (!string.IsNullOrWhiteSpace(cropName))
        {
            var name = cropName.Trim();
            query = query.Where(c => c.CalcCultivoNombre == name);
        }

        var rows = await query
            .OrderBy(c => c.CalcFechaConsulta)
            .ThenBy(c => c.CalcId)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IrrigationCalculationDto> CreateAsync(
        string userId,
        CreateIrrigationCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        var cropName = (request.CropName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cropName))
        {
            throw new ArgumentException("El cultivo es obligatorio.");
        }

        if (!DateOnly.TryParse(request.ConsultationDate, out var consultationDate))
        {
            throw new ArgumentException("La fecha de consulta no es válida.");
        }

        var recommendation = (request.Recommendation ?? string.Empty).Trim();
        if (recommendation is not ("Regar" or "No regar"))
        {
            throw new ArgumentException("La recomendación debe ser 'Regar' o 'No regar'.");
        }

        ValidatePercent(request.FieldCapacity, "capacidad de campo");
        ValidatePercent(request.MaxIrrigationLimit, "límite máximo de riego");
        ValidatePercent(request.IrrigationDecision, "decisión de riego");
        ValidatePercent(request.MorningMoisture, "humedad de la mañana");
        ValidatePercent(request.AfternoonMoisture, "humedad de la tarde");

        int? cultId = request.CropId;
        if (cultId is null)
        {
            cultId = await _db.Cultivos.AsNoTracking()
                .Where(c => c.CultActivo && c.CultNombre == cropName)
                .Select(c => (int?)c.CultId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var action = string.IsNullOrWhiteSpace(request.IrrigationAction)
            ? null
            : request.IrrigationAction.Trim();
        if (action is not null && action is not ("Sí" or "Si" or "No"))
        {
            throw new ArgumentException("El campo '¿Realizó el riego?' debe ser Sí o No.");
        }

        if (action == "Si")
        {
            action = "Sí";
        }

        var now = DateTime.UtcNow;
        var entity = new HidrtbCalculoRiego
        {
            UsuaId = userId,
            CalcCultivoNombre = cropName,
            CultId = cultId,
            CalcCapacidadCampo = (decimal)request.FieldCapacity,
            CalcLimiteMaxRiego = (decimal)request.MaxIrrigationLimit,
            CalcDecisionRiego = (decimal)request.IrrigationDecision,
            CalcFechaConsulta = consultationDate,
            CalcHumedadManana = (decimal)request.MorningMoisture,
            CalcHumedadTarde = (decimal)request.AfternoonMoisture,
            CalcRecomendacion = recommendation,
            CalcRealizoRiego = action,
            CalcObservacion = string.IsNullOrWhiteSpace(request.Observation)
                ? null
                : request.Observation.Trim(),
            CalcActivo = true,
            CalcFechaCreacion = now,
            CalcFechaActualizacion = now,
        };

        _db.CalculosRiego.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static void ValidatePercent(double value, string field)
    {
        if (double.IsNaN(value) || value < 0 || value > 100)
        {
            throw new ArgumentException($"El valor de {field} debe estar entre 0 y 100.");
        }
    }

    private static IrrigationCalculationDto ToDto(HidrtbCalculoRiego c) => new()
    {
        Id = c.CalcId,
        CropName = c.CalcCultivoNombre,
        CropId = c.CultId,
        FieldCapacity = (double)c.CalcCapacidadCampo,
        MaxIrrigationLimit = (double)c.CalcLimiteMaxRiego,
        IrrigationDecision = (double)c.CalcDecisionRiego,
        ConsultationDate = c.CalcFechaConsulta.ToString("yyyy-MM-dd"),
        MorningMoisture = (double)c.CalcHumedadManana,
        AfternoonMoisture = (double)c.CalcHumedadTarde,
        Recommendation = c.CalcRecomendacion,
        IrrigationAction = c.CalcRealizoRiego,
        Observation = c.CalcObservacion,
    };
}
