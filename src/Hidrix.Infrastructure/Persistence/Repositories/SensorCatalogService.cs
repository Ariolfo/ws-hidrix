using Microsoft.EntityFrameworkCore;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Persistence;
using Hidrix.Infrastructure.Services;

namespace Hidrix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Metadatos de sensores (HidrtbSensorMeta) + inventario Visualiti para el listado admin.
/// </summary>
public sealed class SensorCatalogService : ISensorCatalogService
{
    private readonly HidrixDbContext _db;
    private readonly IVisualitiClient _visualiti;

    /// <summary>Inicializa el servicio.</summary>
    public SensorCatalogService(HidrixDbContext db, IVisualitiClient visualiti)
    {
        _db = db;
        _visualiti = visualiti;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PhysicalSensor>> ListSensorsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryActive().ToListAsync(cancellationToken);
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
        var devices = await _visualiti.GetDevicesAsync(cancellationToken);
        var metas = await QueryActive().ToListAsync(cancellationToken);
        var metaByName = metas.ToDictionary(m => m.SensNombre, StringComparer.OrdinalIgnoreCase);

        if (devices.Count == 0)
        {
            return metas
                .OrderBy(m => InferCountry(m.SensNombre) ?? string.Empty)
                .ThenBy(m => m.SensNombre)
                .Select(m => ToDto(m))
                .ToList();
        }

        var result = new List<CatalogSensorDto>(devices.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var device in devices.OrderBy(d => d.StationId))
        {
            seen.Add(device.Serial);
            metaByName.TryGetValue(device.Serial, out var meta);
            if (meta is null)
            {
                meta = await EnsureMetaAsync(device.Serial, cancellationToken);
            }

            result.Add(ToDtoFromDevice(device, meta));
        }

        // Metadatos huérfanos (serial ya no en Visualiti) siguen visibles para CC/cultivo.
        foreach (var meta in metas.Where(m => !seen.Contains(m.SensNombre)).OrderBy(m => m.SensNombre))
        {
            result.Add(ToDto(meta));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CatalogSensorDto?> GetCatalogByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await QueryActive()
            .FirstOrDefaultAsync(s => s.MetaId == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var devices = await _visualiti.GetDevicesAsync(cancellationToken);
        var device = devices.FirstOrDefault(d =>
            string.Equals(d.Serial, row.SensNombre, StringComparison.OrdinalIgnoreCase));
        if (device is not null)
        {
            return await ToDtoFromVisualitiAsync(device, row, cancellationToken);
        }

        return ToDto(row);
    }

    /// <inheritdoc />
    public async Task<CatalogSensorDto> AssignCropAsync(
        AssignSensorCropRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeSerial(request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El serial del sensor es obligatorio (ej. M333).");
        }

        await ValidateCropAsync(request.CropId, cancellationToken);

        var entity = await _db.SensorMetas
            .Include(s => s.Cultivo)
            .FirstOrDefaultAsync(s => s.SensNombre == name, cancellationToken);

        var now = DateTime.UtcNow;
        if (entity is null)
        {
            entity = new HidrtbSensorMeta
            {
                SensNombre = name,
                MetaActivo = true,
                MetaFechaCreacion = now,
                MetaFechaActualizacion = now,
            };
            _db.SensorMetas.Add(entity);
        }
        else if (!entity.MetaActivo)
        {
            entity.MetaActivo = true;
        }

        entity.CultId = request.CropId;
        entity.SensFinca = string.IsNullOrWhiteSpace(request.Farm) ? null : request.Farm.Trim();
        entity.MetaFechaActualizacion = now;

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(entity).Reference(s => s.Cultivo).LoadAsync(cancellationToken);
        return await EnrichDtoAsync(ToDto(entity), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CatalogSensorDto> UpdateCropAsync(
        int id,
        AssignSensorCropRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateCropAsync(request.CropId, cancellationToken);

        var entity = await _db.SensorMetas
            .Include(s => s.Cultivo)
            .FirstOrDefaultAsync(s => s.MetaId == id && s.MetaActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe la relación de sensor {id}.");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = NormalizeSerial(request.Name);
            if (!string.Equals(entity.SensNombre, name, StringComparison.OrdinalIgnoreCase))
            {
                var clash = await _db.SensorMetas.AnyAsync(
                    s => s.SensNombre == name && s.MetaId != id,
                    cancellationToken);
                if (clash)
                {
                    throw new InvalidOperationException($"Ya existe metadatos para {name}.");
                }

                entity.SensNombre = name;
            }
        }

        entity.CultId = request.CropId;
        entity.SensFinca = string.IsNullOrWhiteSpace(request.Farm) ? null : request.Farm.Trim();
        entity.MetaFechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(entity).Reference(s => s.Cultivo).LoadAsync(cancellationToken);
        return await EnrichDtoAsync(ToDto(entity), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SensorMetas
            .FirstOrDefaultAsync(s => s.MetaId == id && s.MetaActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe la relación de sensor {id}.");

        // Conserva CC; solo limpia cultivo/finca y deja el registro activo para estimaciones.
        entity.CultId = null;
        entity.SensFinca = null;
        entity.MetaFechaActualizacion = DateTime.UtcNow;
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

        var entity = await _db.SensorMetas
            .Include(s => s.Cultivo)
            .FirstOrDefaultAsync(s => s.MetaId == id && s.MetaActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe la relación de sensor {id}.");

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
        entity.MetaFechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return await EnrichDtoAsync(ToDto(entity), cancellationToken);
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

    private async Task<HidrtbSensorMeta> EnsureMetaAsync(string serial, CancellationToken cancellationToken)
    {
        var existing = await _db.SensorMetas
            .Include(s => s.Cultivo)
            .FirstOrDefaultAsync(s => s.SensNombre == serial, cancellationToken);
        if (existing is not null)
        {
            if (!existing.MetaActivo)
            {
                existing.MetaActivo = true;
                existing.MetaFechaActualizacion = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var now = DateTime.UtcNow;
        var entity = new HidrtbSensorMeta
        {
            SensNombre = serial,
            MetaActivo = true,
            MetaFechaCreacion = now,
            MetaFechaActualizacion = now,
        };
        _db.SensorMetas.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task ValidateCropAsync(int? cropId, CancellationToken cancellationToken)
    {
        if (cropId is not int id)
        {
            return;
        }

        var cropOk = await _db.Cultivos.AnyAsync(c => c.CultId == id && c.CultActivo, cancellationToken);
        if (!cropOk)
        {
            throw new ArgumentException("El cultivo indicado no existe.");
        }
    }

    private static CatalogSensorDto ToDtoFromDevice(VisualitiDevice device, HidrtbSensorMeta meta)
    {
        var country = InferCountry(device.Serial) ?? string.Empty;
        return new CatalogSensorDto
        {
            Id = meta.MetaId,
            Name = device.Serial,
            DeviceName = string.IsNullOrWhiteSpace(device.DeviceName) ? null : device.DeviceName,
            NetworkId = 0,
            NetworkName = string.IsNullOrWhiteSpace(device.Origen) ? "Visualiti" : device.Origen,
            CountryId = CountryIdFromName(country),
            CountryName = country,
            CropId = meta.CultId,
            CropName = meta.Cultivo?.CultNombre
                ?? VisualitiStationInventory.InferCrop(device.DeviceName),
            Farm = meta.SensFinca,
            EstimatedFieldCapacity = meta.SensCcEstimado.HasValue ? (double)meta.SensCcEstimado.Value : null,
            EstimationMethod = meta.SensMetodoCc,
            EstimationDate = meta.SensFechaEstimacionCc,
        };
    }

    private async Task<CatalogSensorDto> ToDtoFromVisualitiAsync(
        VisualitiDevice device,
        HidrtbSensorMeta meta,
        CancellationToken cancellationToken)
    {
        var dto = ToDtoFromDevice(device, meta);
        var hardware = await _visualiti.GetHardwareStatusAsync(device.StationId, cancellationToken);
        var snapshot = VisualitiHardwareDefaults.From(hardware);
        dto.Latitude = snapshot.Latitude;
        dto.Longitude = snapshot.Longitude;
        dto.SensorStatus = snapshot.HardwareStatus;
        dto.Connectivity = snapshot.Connectivity;
        return dto;
    }

    private async Task<CatalogSensorDto> EnrichDtoAsync(CatalogSensorDto dto, CancellationToken cancellationToken)
    {
        var station = VisualitiClient.ParseVisualitiStationId(dto.Name);
        if (station is null)
        {
            return dto;
        }

        var hardware = await _visualiti.GetHardwareStatusAsync(station.Value, cancellationToken);
        var snapshot = VisualitiHardwareDefaults.From(hardware);
        dto.Latitude = snapshot.Latitude ?? dto.Latitude;
        dto.Longitude = snapshot.Longitude ?? dto.Longitude;
        dto.SensorStatus = snapshot.HardwareStatus;
        dto.Connectivity = snapshot.Connectivity;

        var devices = await _visualiti.GetDevicesAsync(cancellationToken);
        var device = devices.FirstOrDefault(d =>
            string.Equals(d.Serial, dto.Name, StringComparison.OrdinalIgnoreCase));
        if (device is not null)
        {
            dto.DeviceName = string.IsNullOrWhiteSpace(device.DeviceName) ? dto.DeviceName : device.DeviceName;
            dto.NetworkName = string.IsNullOrWhiteSpace(device.Origen) ? dto.NetworkName : device.Origen;
            if (string.IsNullOrWhiteSpace(dto.CropName))
            {
                dto.CropName = VisualitiStationInventory.InferCrop(device.DeviceName);
            }
        }

        return dto;
    }

    private IQueryable<HidrtbSensorMeta> QueryActive() =>
        _db.SensorMetas.AsNoTracking()
            .Include(s => s.Cultivo)
            .Where(s => s.MetaActivo);

    private static PhysicalSensor ToPhysical(HidrtbSensorMeta s)
    {
        var country = InferCountry(s.SensNombre);
        return new PhysicalSensor(
            s.SensNombre,
            "Visualiti",
            s.Cultivo?.CultNombre,
            s.SensFinca,
            country,
            null,
            null,
            SensorCatalog.DefaultChannels,
            CountryIdFromName(country),
            CountryTimeZoneResolver.ResolveForCountryName(country));
    }

    private static CatalogSensorDto ToDto(HidrtbSensorMeta s)
    {
        var country = InferCountry(s.SensNombre) ?? string.Empty;
        return new CatalogSensorDto
        {
            Id = s.MetaId,
            Name = s.SensNombre,
            NetworkId = 0,
            NetworkName = "Visualiti",
            CountryId = CountryIdFromName(country),
            CountryName = country,
            CropId = s.CultId,
            CropName = s.Cultivo?.CultNombre,
            Farm = s.SensFinca,
            EstimatedFieldCapacity = s.SensCcEstimado.HasValue ? (double)s.SensCcEstimado.Value : null,
            EstimationMethod = s.SensMetodoCc,
            EstimationDate = s.SensFechaEstimacionCc,
        };
    }

    private static string NormalizeSerial(string? raw)
    {
        var (physical, _) = SensorCatalog.SplitLogicalId((raw ?? string.Empty).Trim());
        return physical.ToUpperInvariant();
    }

    private static string? InferCountry(string serial)
    {
        var station = VisualitiClient.ParseVisualitiStationId(serial);
        return station is int id ? VisualitiStationInventory.InferCountry(id) : null;
    }

    private static int CountryIdFromName(string? country) =>
        country?.Trim().ToUpperInvariant() switch
        {
            "COLOMBIA" => 170,
            "ECUADOR" => 218,
            "HONDURAS" => 340,
            _ => 0,
        };
}
