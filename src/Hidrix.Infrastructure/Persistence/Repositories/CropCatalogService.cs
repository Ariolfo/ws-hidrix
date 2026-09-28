using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Catálogo de cultivos desde HidrtbCultivo.
/// </summary>
public sealed class CropCatalogService : ICropCatalogService
{
    private readonly HidrixDbContext _db;

    /// <summary>Inicializa el servicio.</summary>
    public CropCatalogService(HidrixDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CropDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Cultivos.AsNoTracking()
            .Where(c => c.CultActivo)
            .OrderBy(c => c.CultNombre)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<CropDto> CreateAsync(CreateCropRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del cultivo es obligatorio.");
        }

        var exists = await _db.Cultivos.AnyAsync(
            c => c.CultNombre == name,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Ya existe el cultivo {name}.");
        }

        var now = DateTime.UtcNow;
        var entity = new HidrtbCultivo
        {
            CultNombre = name,
            CultCapacidadCampo = (decimal)request.FieldCapacity,
            CultPorcentajeMaximo = (decimal)request.MaxIrrigationLimit,
            CultDecisionRiego = (decimal)request.IrrigationDecision,
            CultActivo = true,
            CultFechaCreacion = now,
            CultFechaActualizacion = now,
        };

        _db.Cultivos.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<CropDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Cultivos.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CultId == id && c.CultActivo, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<CropDto> UpdateAsync(int id, CreateCropRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Cultivos
            .FirstOrDefaultAsync(c => c.CultId == id && c.CultActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe el cultivo {id}.");

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del cultivo es obligatorio.");
        }

        var duplicate = await _db.Cultivos.AnyAsync(
            c => c.CultNombre == name && c.CultId != id,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"Ya existe el cultivo {name}.");
        }

        entity.CultNombre = name;
        entity.CultCapacidadCampo = (decimal)request.FieldCapacity;
        entity.CultPorcentajeMaximo = (decimal)request.MaxIrrigationLimit;
        entity.CultDecisionRiego = (decimal)request.IrrigationDecision;
        entity.CultFechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Cultivos
            .FirstOrDefaultAsync(c => c.CultId == id && c.CultActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe el cultivo {id}.");

        entity.CultActivo = false;
        entity.CultFechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CropMoistureProfile> ResolveAsync(
        string? cultivoOrText,
        CancellationToken cancellationToken = default)
    {
        var crops = await ListAsync(cancellationToken);
        if (crops.Count == 0)
        {
            return CropMoistureProfiles.Resolve(cultivoOrText);
        }

        if (string.IsNullOrWhiteSpace(cultivoOrText))
        {
            var cacao = crops.FirstOrDefault(c => Normalize(c.Name).Contains("cacao", StringComparison.Ordinal));
            return cacao is null ? ToProfile(crops[0]) : ToProfile(cacao);
        }

        var normalized = Normalize(cultivoOrText);
        foreach (var crop in crops)
        {
            var n = Normalize(crop.Name);
            if (normalized.Contains(n, StringComparison.Ordinal) || n.Contains(normalized, StringComparison.Ordinal))
            {
                return ToProfile(crop);
            }
        }

        // Compatibilidad con textos legacy (Lima ácida Tahiti, etc.)
        return CropMoistureProfiles.Resolve(cultivoOrText);
    }

    private static CropDto ToDto(HidrtbCultivo c) => new()
    {
        Id = c.CultId,
        Name = c.CultNombre,
        FieldCapacity = (double)c.CultCapacidadCampo,
        MaxIrrigationLimit = (double)c.CultPorcentajeMaximo,
        IrrigationDecision = (double)c.CultDecisionRiego,
    };

    private static CropMoistureProfile ToProfile(CropDto c) =>
        new(c.Name, c.FieldCapacity, c.MaxIrrigationLimit, c.IrrigationDecision);

    private static string Normalize(string text)
    {
        var formD = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
