using System.Collections.Concurrent;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Services;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Caché en memoria thread-safe de sesión Visualiti (singleton DI).
/// </summary>
public sealed class VisualitiMoistureCache : IVisualitiMoistureCache
{
    private readonly object _tokenLock = new();
    private readonly SemaphoreSlim _exclusiveGate = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    private readonly ConcurrentDictionary<string, CacheEntry<VisualitiReading?>> _latest = new(
        StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, CacheEntry<IReadOnlyList<VisualitiReading>>> _history = new(
        StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<int, CacheEntry<VisualitiStationSensors>> _stationSensors = new();
    private readonly ConcurrentDictionary<int, CacheEntry<VisualitiHardwareStatus?>> _stationHardware = new();

    private readonly object _devicesLock = new();
    private CacheEntry<IReadOnlyList<VisualitiDevice>>? _devices;

    private readonly object _inventoryLock = new();
    private InventoryEntry? _inventory;

    private sealed record InventoryEntry(
        IReadOnlyList<PhysicalSensor> Value,
        DateTimeOffset SoftExpiresAt,
        DateTimeOffset HardExpiresAt);

    /// <inheritdoc />
    public bool TryGetToken(out string token)
    {
        lock (_tokenLock)
        {
            if (_token is not null && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-60))
            {
                token = _token;
                return true;
            }
        }

        token = string.Empty;
        return false;
    }

    /// <inheritdoc />
    public void SetToken(string token, DateTimeOffset expiresAt)
    {
        lock (_tokenLock)
        {
            _token = token;
            _tokenExpiresAt = expiresAt;
        }
    }

    /// <inheritdoc />
    public async Task<T> RunExclusiveAsync<T>(
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        await _exclusiveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await factory(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _exclusiveGate.Release();
        }
    }

    /// <inheritdoc />
    public bool TryGetLatest(string physicalSerial, out VisualitiReading? reading)
    {
        if (_latest.TryGetValue(physicalSerial, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            reading = entry.Value;
            return true;
        }

        reading = null;
        return false;
    }

    /// <inheritdoc />
    public void SetLatest(string physicalSerial, VisualitiReading? reading, TimeSpan ttl)
    {
        _latest[physicalSerial] = new CacheEntry<VisualitiReading?>(
            reading,
            DateTimeOffset.UtcNow.Add(ttl));
    }

    /// <inheritdoc />
    public bool TryGetHistory(
        string physicalSerial,
        string rangeKey,
        out IReadOnlyList<VisualitiReading> readings)
    {
        var key = HistoryKey(physicalSerial, rangeKey);
        if (_history.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            readings = entry.Value;
            return true;
        }

        readings = Array.Empty<VisualitiReading>();
        return false;
    }

    /// <inheritdoc />
    public void SetHistory(
        string physicalSerial,
        string rangeKey,
        IReadOnlyList<VisualitiReading> readings,
        TimeSpan ttl)
    {
        var key = HistoryKey(physicalSerial, rangeKey);
        _history[key] = new CacheEntry<IReadOnlyList<VisualitiReading>>(
            readings,
            DateTimeOffset.UtcNow.Add(ttl));
    }

    /// <inheritdoc />
    public VisualitiReading? FindLatestFromHistory(string physicalSerial)
    {
        VisualitiReading? best = null;
        foreach (var pair in _history)
        {
            if (!pair.Key.StartsWith(physicalSerial + "|", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pair.Value.ExpiresAt <= DateTimeOffset.UtcNow || pair.Value.Value.Count == 0)
            {
                continue;
            }

            var last = pair.Value.Value[^1];
            if (best is null || last.FechaHora > best.FechaHora)
            {
                best = last;
            }
        }

        return best;
    }

    /// <inheritdoc />
    public bool TryGetDevices(out IReadOnlyList<VisualitiDevice> devices)
    {
        lock (_devicesLock)
        {
            if (_devices is { } entry && entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                devices = entry.Value;
                return true;
            }
        }

        devices = [];
        return false;
    }

    /// <inheritdoc />
    public void SetDevices(IReadOnlyList<VisualitiDevice> devices, TimeSpan ttl)
    {
        lock (_devicesLock)
        {
            _devices = new CacheEntry<IReadOnlyList<VisualitiDevice>>(
                devices,
                DateTimeOffset.UtcNow.Add(ttl));
        }
    }

    /// <inheritdoc />
    public bool TryGetStationSensors(int stationId, out VisualitiStationSensors sensors)
    {
        if (_stationSensors.TryGetValue(stationId, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            sensors = entry.Value;
            return true;
        }

        sensors = new VisualitiStationSensors();
        return false;
    }

    /// <inheritdoc />
    public void SetStationSensors(int stationId, VisualitiStationSensors sensors, TimeSpan ttl)
    {
        _stationSensors[stationId] = new CacheEntry<VisualitiStationSensors>(
            sensors,
            DateTimeOffset.UtcNow.Add(ttl));
    }

    /// <inheritdoc />
    public bool TryGetHardware(int stationId, out VisualitiHardwareStatus? hardware, out bool found)
    {
        if (_stationHardware.TryGetValue(stationId, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            hardware = entry.Value;
            found = true;
            return true;
        }

        hardware = null;
        found = false;
        return false;
    }

    /// <inheritdoc />
    public void SetHardware(int stationId, VisualitiHardwareStatus? hardware, TimeSpan ttl)
    {
        _stationHardware[stationId] = new CacheEntry<VisualitiHardwareStatus?>(
            hardware,
            DateTimeOffset.UtcNow.Add(ttl));
    }

    /// <inheritdoc />
    public bool TryGetInventory(
        out IReadOnlyList<PhysicalSensor> inventory,
        out bool isStale,
        bool allowStale = false)
    {
        lock (_inventoryLock)
        {
            if (_inventory is { } entry)
            {
                var now = DateTimeOffset.UtcNow;
                if (now < entry.SoftExpiresAt)
                {
                    inventory = entry.Value;
                    isStale = false;
                    return true;
                }

                if (allowStale && now < entry.HardExpiresAt)
                {
                    inventory = entry.Value;
                    isStale = true;
                    return true;
                }
            }
        }

        inventory = [];
        isStale = false;
        return false;
    }

    /// <inheritdoc />
    public void SetInventory(
        IReadOnlyList<PhysicalSensor> inventory,
        TimeSpan ttl,
        TimeSpan staleWindow)
    {
        var now = DateTimeOffset.UtcNow;
        var hard = ttl > staleWindow ? ttl : staleWindow;
        lock (_inventoryLock)
        {
            _inventory = new InventoryEntry(
                inventory,
                now.Add(ttl),
                now.Add(hard));
        }
    }

    private static string HistoryKey(string physicalSerial, string rangeKey) =>
        $"{physicalSerial}|{rangeKey.Trim().ToLowerInvariant()}";

    private sealed record CacheEntry<T>(T Value, DateTimeOffset ExpiresAt);
}
