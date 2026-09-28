using System.Text.RegularExpressions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Services;
using Hidrix.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Inventario de estaciones desde GET /api/devices, enriquecido con /sensor y /hardware-status.
/// Caché singleton + fallback stale si Visualiti no responde.
/// </summary>
public sealed partial class VisualitiStationInventory : IVisualitiStationInventory
{
    private readonly IVisualitiClient _visualiti;
    private readonly ISensorCatalogService _catalog;
    private readonly IVisualitiMoistureCache _cache;
    private readonly VisualitiOptions _options;
    private readonly ILogger<VisualitiStationInventory> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Inicializa el inventario Visualiti.</summary>
    public VisualitiStationInventory(
        IVisualitiClient visualiti,
        ISensorCatalogService catalog,
        IVisualitiMoistureCache cache,
        IOptions<VisualitiOptions> options,
        ILogger<VisualitiStationInventory> logger)
    {
        _visualiti = visualiti;
        _catalog = catalog;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PhysicalSensor>> ListSensorsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetInventory(out var fresh, out _, allowStale: false))
        {
            return fresh;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetInventory(out fresh, out _, allowStale: false))
            {
                return fresh;
            }

            try
            {
                var built = await BuildInventoryAsync(cancellationToken).ConfigureAwait(false);
                if (built.Count > 0)
                {
                    _cache.SetInventory(built, _options.InventoryCacheTtl, _options.StaleInventoryWindow);
                    return built;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Error construyendo inventario Visualiti");
            }

            if (_cache.TryGetInventory(out var stale, out var isStale, allowStale: true) &&
                isStale &&
                stale.Count > 0)
            {
                _logger.LogWarning(
                    "Sirviendo inventario Visualiti stale ({Count} sensores; ventana {Window})",
                    stale.Count,
                    _options.StaleInventoryWindow);
                return stale;
            }

            _logger.LogWarning("Visualiti sin inventario; usando metadatos locales HidrtbSensorMeta");
            return await _catalog.ListSensorsAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PhysicalSensor?> GetSensorAsync(
        string sensorId,
        CancellationToken cancellationToken = default)
    {
        var (physical, _) = SensorCatalog.SplitLogicalId(sensorId);
        var all = await ListSensorsAsync(cancellationToken).ConfigureAwait(false);
        var match = all.FirstOrDefault(s =>
            string.Equals(s.Serial, physical, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return match;
        }

        return await _catalog.GetSensorAsync(sensorId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<PhysicalSensor>> BuildInventoryAsync(
        CancellationToken cancellationToken)
    {
        var devices = await _visualiti.GetDevicesAsync(cancellationToken).ConfigureAwait(false);
        if (devices.Count == 0)
        {
            return [];
        }

        var localBySerial = (await _catalog.ListSensorsAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Serial, StringComparer.OrdinalIgnoreCase);

        var results = new PhysicalSensor[devices.Count];
        await Parallel.ForEachAsync(
            devices.Select((device, index) => (device, index)),
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                results[item.index] = await BuildOneAsync(item.device, localBySerial, ct)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

        return results
            .Where(s => s is not null)
            .OrderBy(s => s.Serial, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<PhysicalSensor> BuildOneAsync(
        VisualitiDevice device,
        IReadOnlyDictionary<string, PhysicalSensor> localBySerial,
        CancellationToken cancellationToken)
    {
        localBySerial.TryGetValue(device.Serial, out var local);

        var sensorsTask = _visualiti.GetStationSensorsAsync(device.StationId, cancellationToken);
        var hardwareTask = _visualiti.GetHardwareStatusAsync(device.StationId, cancellationToken);
        await Task.WhenAll(sensorsTask, hardwareTask).ConfigureAwait(false);

        var liveSensors = sensorsTask.Result;
        var hardware = hardwareTask.Result;

        var channelCount = liveSensors?.Channels.Count > 0
            ? liveSensors.Channels.Count
            : local?.Canales ?? SensorCatalog.DefaultChannels;

        var snapshot = VisualitiHardwareDefaults.From(hardware);
        var lat = snapshot.Latitude ?? local?.Latitud;
        var lng = snapshot.Longitude ?? local?.Longitud;
        var cultivo = local?.Cultivo ?? InferCrop(device.DeviceName);
        var timeZoneId = local?.TimeZoneId
            ?? CountryTimeZoneResolver.ResolveForStation(device.StationId);

        return new PhysicalSensor(
            device.Serial,
            local?.Red ?? (string.IsNullOrWhiteSpace(device.Origen) ? "Visualiti" : device.Origen),
            cultivo,
            local?.Finca,
            local?.Pais ?? InferCountry(device.StationId),
            lat,
            lng,
            channelCount,
            local?.CountryId,
            timeZoneId,
            string.IsNullOrWhiteSpace(device.DeviceName) ? null : device.DeviceName,
            snapshot.Online,
            snapshot.Connectivity,
            snapshot.HardwareStatus,
            snapshot.HasSnapshot);
    }

    /// <summary>Infiere cultivo desde name_device Visualiti.</summary>
    public static string? InferCrop(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return null;
        }

        var upper = deviceName.ToUpperInvariant();
        var match = CropTokenRegex().Match(upper);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups[1].Value switch
        {
            "AGUACATE" => "Aguacate",
            "CACAO" => "Cacao",
            "PAPAYA" => "Papaya",
            "LIMA" => "Lima",
            _ => null,
        };
    }

    [GeneratedRegex(@"(?<![A-Z])(AGUACATE|CACAO|PAPAYA|LIMA)(?![A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex CropTokenRegex();

    /// <summary>Infiere país Fontagro desde id de estación Visualiti.</summary>
    public static string? InferCountry(int stationId) =>
        stationId switch
        {
            >= 312 and <= 323 or 336 => "Colombia",
            >= 333 and <= 335 => "Ecuador",
            >= 324 and <= 332 => "Honduras",
            _ => null,
        };
}
