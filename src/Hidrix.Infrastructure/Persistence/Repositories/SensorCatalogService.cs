using Microsoft.EntityFrameworkCore;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Catálogo de sensores desde HidrtbSensor / HidrtbRed.
/// </summary>
public sealed class SensorCatalogService : ISensorCatalogService
{
    private readonly HidrixDbContext _db;

    /// <summary>Inicializa el servicio.</summary>
    public SensorCatalogService(HidrixDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PhysicalSensor>> ListSensorsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryActive().ToListAsync(cancellationToken);
        return rows.Select(ToPhysical).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PhysicalSensor>> ListGeolocatedSensorsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryActive()
            .Where(s => s.SensLatitud != null && s.SensLongitud != null)
            .ToListAsync(cancellationToken);
        return rows.Select(ToPhysical).ToList();
    }

    /// <inheritdoc />
    public async Task<PhysicalSensor?> GetSensorAsync(
        string sensorId,
        CancellationToken cancellationToken = default)
    {
        var (physical, _) = SensorCatalog.SplitLogicalId(sensorId);
        var row = await QueryActive()
            .FirstOrDefaultAsync(
                s => s.SensNombre == physical || s.SensNombre == sensorId.Trim(),
                cancellationToken);
        return row is null ? null : ToPhysical(row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CatalogSensorDto>> ListCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryActive()
            .OrderBy(s => s.Red!.Pais!.PaisNombre)
            .ThenBy(s => s.Red!.RedNombre)
            .ThenBy(s => s.SensNombre)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<CatalogSensorDto> CreateAsync(
        CreateCatalogSensorRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del sensor es obligatorio.");
        }

        var exists = await _db.Sensores.AnyAsync(s => s.SensNombre == name, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Ya existe el sensor {name}.");
        }

        var red = await _db.Redes.Include(r => r.Pais)
            .FirstOrDefaultAsync(r => r.RedId == request.NetworkId, cancellationToken)
            ?? throw new ArgumentException("La red indicada no existe.");

        if (request.CropId is int cropId)
        {
            var cropOk = await _db.Cultivos.AnyAsync(c => c.CultId == cropId && c.CultActivo, cancellationToken);
            if (!cropOk)
            {
                throw new ArgumentException("El cultivo indicado no existe.");
            }
        }

        var now = DateTime.UtcNow;
        var entity = new HidrtbSensor
        {
            SensNombre = name,
            RedId = red.RedId,
            CultId = request.CropId,
            SensLatitud = request.Latitude.HasValue ? (decimal)request.Latitude.Value : null,
            SensLongitud = request.Longitude.HasValue ? (decimal)request.Longitude.Value : null,
            SensEstado = string.IsNullOrWhiteSpace(request.SensorStatus) ? "desconocido" : request.SensorStatus.Trim(),
            SensConectividad = string.IsNullOrWhiteSpace(request.Connectivity) ? "offline" : request.Connectivity.Trim().ToLowerInvariant(),
            SensFinca = string.IsNullOrWhiteSpace(request.Farm) ? null : request.Farm.Trim(),
            SensCanales = 2,
            SensActivo = true,
            SensFechaCreacion = now,
            SensFechaActualizacion = now,
        };

        _db.Sensores.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(entity).Reference(s => s.Red).LoadAsync(cancellationToken);
        await _db.Entry(entity).Reference(s => s.Cultivo).LoadAsync(cancellationToken);
        if (entity.Red is not null)
        {
            await _db.Entry(entity.Red).Reference(r => r.Pais).LoadAsync(cancellationToken);
        }

        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<CatalogSensorDto?> GetCatalogByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await QueryActive().FirstOrDefaultAsync(s => s.SensId == id, cancellationToken);
        return row is null ? null : ToDto(row);
    }

    /// <inheritdoc />
    public async Task<CatalogSensorDto> UpdateAsync(
        int id,
        CreateCatalogSensorRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sensores
            .Include(s => s.Red)!.ThenInclude(r => r!.Pais)
            .Include(s => s.Cultivo)
            .FirstOrDefaultAsync(s => s.SensId == id && s.SensActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe el sensor {id}.");

        var name = (request.Name ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del sensor es obligatorio.");
        }

        var duplicate = await _db.Sensores.AnyAsync(
            s => s.SensNombre == name && s.SensId != id,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"Ya existe el sensor {name}.");
        }

        var red = await _db.Redes.Include(r => r.Pais)
            .FirstOrDefaultAsync(r => r.RedId == request.NetworkId, cancellationToken)
            ?? throw new ArgumentException("La red indicada no existe.");

        if (request.CropId is int cropId)
        {
            var cropOk = await _db.Cultivos.AnyAsync(c => c.CultId == cropId && c.CultActivo, cancellationToken);
            if (!cropOk)
            {
                throw new ArgumentException("El cultivo indicado no existe.");
            }
        }

        entity.SensNombre = name;
        entity.RedId = red.RedId;
        entity.CultId = request.CropId;
        entity.SensLatitud = request.Latitude.HasValue ? (decimal)request.Latitude.Value : null;
        entity.SensLongitud = request.Longitude.HasValue ? (decimal)request.Longitude.Value : null;
        entity.SensEstado = string.IsNullOrWhiteSpace(request.SensorStatus) ? "desconocido" : request.SensorStatus.Trim();
        entity.SensConectividad = string.IsNullOrWhiteSpace(request.Connectivity)
            ? "offline"
            : request.Connectivity.Trim().ToLowerInvariant();
        entity.SensFinca = string.IsNullOrWhiteSpace(request.Farm) ? null : request.Farm.Trim();
        entity.SensFechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(entity).Reference(s => s.Red).LoadAsync(cancellationToken);
        await _db.Entry(entity).Reference(s => s.Cultivo).LoadAsync(cancellationToken);
        if (entity.Red is not null)
        {
            await _db.Entry(entity.Red).Reference(r => r.Pais).LoadAsync(cancellationToken);
        }

        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sensores
            .FirstOrDefaultAsync(s => s.SensId == id && s.SensActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe el sensor {id}.");

        entity.SensActivo = false;
        entity.SensFechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CatalogSensorDto> SaveEstimatedFieldCapacityAsync(
        int id,
        SaveEstimatedFieldCapacityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FieldCapacity is < 0 or > 100)
        {
            throw new ArgumentException("La CC estimada debe estar entre 0 y 100 %.");
        }

        var method = (request.Method ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("El método de estimación es obligatorio.");
        }

        var entity = await _db.Sensores
            .Include(s => s.Red)!.ThenInclude(r => r!.Pais)
            .Include(s => s.Cultivo)
            .FirstOrDefaultAsync(s => s.SensId == id && s.SensActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe el sensor {id}.");

        var estimatedAt = request.EstimatedAt ?? DateTime.UtcNow;
        if (estimatedAt.Kind == DateTimeKind.Unspecified)
        {
            estimatedAt = DateTime.SpecifyKind(estimatedAt, DateTimeKind.Utc);
        }
        else if (estimatedAt.Kind == DateTimeKind.Local)
        {
            estimatedAt = estimatedAt.ToUniversalTime();
        }

        entity.SensCcEstimado = Math.Round((decimal)request.FieldCapacity, 2);
        entity.SensMetodoCc = method;
        entity.SensFechaEstimacionCc = estimatedAt;
        entity.SensFechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetworkDto>> ListNetworksAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Redes.AsNoTracking()
            .Include(r => r.Pais)
            .OrderBy(r => r.Pais!.PaisNombre)
            .ThenBy(r => r.RedNombre)
            .Select(r => new NetworkDto
            {
                Id = r.RedId,
                Name = r.RedNombre,
                CountryId = r.PaisId,
                CountryName = r.Pais!.PaisNombre,
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<HidrtbSensor> QueryActive() =>
        _db.Sensores.AsNoTracking()
            .Include(s => s.Red)!.ThenInclude(r => r!.Pais)
            .Include(s => s.Cultivo)
            .Where(s => s.SensActivo);

    private static PhysicalSensor ToPhysical(HidrtbSensor s) =>
        new(
            s.SensNombre,
            s.Red?.RedNombre ?? string.Empty,
            s.Cultivo?.CultNombre,
            s.SensFinca,
            s.Red?.Pais?.PaisNombre,
            s.SensLatitud.HasValue ? (double)s.SensLatitud.Value : null,
            s.SensLongitud.HasValue ? (double)s.SensLongitud.Value : null,
            s.SensCanales <= 0 ? SensorCatalog.DefaultChannels : s.SensCanales,
            s.Red?.PaisId);

    private static CatalogSensorDto ToDto(HidrtbSensor s) => new()
    {
        Id = s.SensId,
        Name = s.SensNombre,
        NetworkId = s.RedId,
        NetworkName = s.Red?.RedNombre ?? string.Empty,
        CountryId = s.Red?.PaisId ?? 0,
        CountryName = s.Red?.Pais?.PaisNombre ?? string.Empty,
        CropId = s.CultId,
        CropName = s.Cultivo?.CultNombre,
        Latitude = s.SensLatitud.HasValue ? (double)s.SensLatitud.Value : null,
        Longitude = s.SensLongitud.HasValue ? (double)s.SensLongitud.Value : null,
        SensorStatus = s.SensEstado,
        Connectivity = s.SensConectividad,
        Farm = s.SensFinca,
        EstimatedFieldCapacity = s.SensCcEstimado.HasValue ? (double)s.SensCcEstimado.Value : null,
        EstimationMethod = s.SensMetodoCc,
        EstimationDate = s.SensFechaEstimacionCc,
    };
}
