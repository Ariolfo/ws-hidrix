using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Services;
using Hidrix.Infrastructure.Options;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Cliente HTTP Visualiti. La caché de token/latest/histórico vive en
/// <see cref="IVisualitiMoistureCache"/> (singleton) para sobrevivir al cliente tipado Transient.
/// Tolera respuestas sin datos, timestamps con hora de un dígito y canales Cont Vol / Volumetrico.
/// </summary>
public partial class VisualitiClient : IVisualitiClient
{
    private static readonly TimeSpan LatestTtl = TimeSpan.FromSeconds(600);
    private static readonly TimeSpan HistoryTtl = TimeSpan.FromSeconds(300);
    private static readonly TimeSpan StationMetaTtl = TimeSpan.FromSeconds(600);

    private readonly ConcurrentDictionary<int, (VisualitiStationSensors Value, DateTimeOffset Expires)> _stationSensors =
        new();

    private readonly ConcurrentDictionary<int, (VisualitiHardwareStatus? Value, DateTimeOffset Expires)> _stationHardware =
        new();

    private readonly HttpClient _http;
    private readonly VisualitiOptions _options;
    private readonly ILogger<VisualitiClient> _logger;
    private readonly IVisualitiMoistureCache _cache;
    private readonly ISensorCatalogService _catalog;

    /// <summary>
    /// Inicializa el cliente Visualiti.
    /// </summary>
    /// <param name="http">HttpClient tipado.</param>
    /// <param name="options">Opciones Visualiti.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="cache">Caché singleton compartida.</param>
    /// <param name="catalog">Catálogo de sensores (país → zona horaria).</param>
    public VisualitiClient(
        HttpClient http,
        IOptions<VisualitiOptions> options,
        ILogger<VisualitiClient> logger,
        IVisualitiMoistureCache cache,
        ISensorCatalogService catalog)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _cache = cache;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<VisualitiReading?> GetLatestReadingCachedAsync(
        string sensorSerial,
        CancellationToken cancellationToken = default)
    {
        var physicalSerial = SensorCatalog.SplitLogicalId(sensorSerial).Physical;

        if (_cache.TryGetLatest(physicalSerial, out var cachedLatest))
        {
            return cachedLatest;
        }

        // Preferir último punto de cualquier serie histórica en caché (no dispara Visualiti).
        var fromHistory = _cache.FindLatestFromHistory(physicalSerial);
        if (fromHistory is not null)
        {
            _cache.SetLatest(physicalSerial, fromHistory, LatestTtl);
            return fromHistory;
        }

        try
        {
            // Sin histórico en caché: un fetch de 7d (queda cacheado) y Latest = último punto.
            // Evita lookbacks dedicados de 30d/3d solo para un valor.
            var readings = await FetchMoistureReadingsForRangeAsync(
                sensorSerial,
                "7d",
                cancellationToken);
            var latest = readings.LastOrDefault();
            _cache.SetLatest(physicalSerial, latest, LatestTtl);
            return latest;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo lectura Visualiti para {Serial}", sensorSerial);
            _cache.SetLatest(physicalSerial, null, TimeSpan.FromSeconds(60));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VisualitiReading>> FetchMoistureReadingsForRangeAsync(
        string sensorSerial,
        string rangeKey,
        CancellationToken cancellationToken = default)
    {
        var physicalSerial = SensorCatalog.SplitLogicalId(sensorSerial).Physical;
        var normalized = HistoryRangeHelper.Normalize(rangeKey);

        if (_cache.TryGetHistory(physicalSerial, normalized, out var cached))
        {
            return cached;
        }

        try
        {
            var timeZoneId = await ResolveTimeZoneForSerialAsync(sensorSerial, cancellationToken);
            return await FetchAndCacheRangeAsync(
                physicalSerial,
                sensorSerial,
                normalized,
                timeZoneId,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Visualiti caído / 503 → vacío (no_data) en lugar de 500 al cliente.
            _logger.LogWarning(
                ex,
                "Error obteniendo histórico Visualiti para {Serial} rango {Range}",
                sensorSerial,
                normalized);
            _cache.SetHistory(physicalSerial, normalized, [], TimeSpan.FromSeconds(60));
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<VisualitiStationSensors?> GetStationSensorsAsync(
        int stationId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        if (_stationSensors.TryGetValue(stationId, out var cached) && cached.Expires > DateTimeOffset.UtcNow)
        {
            return cached.Value;
        }

        var token = await GetTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var url = $"{_options.ApiUrl.TrimEnd('/')}/api/devices/4/{stationId}/sensor";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Visualiti sensor list {Status} estación {Station}", response.StatusCode, stationId);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var parsed = ParseStationSensors(doc.RootElement);
        _stationSensors[stationId] = (parsed, DateTimeOffset.UtcNow.Add(StationMetaTtl));
        return parsed;
    }

    /// <inheritdoc />
    public async Task<VisualitiHardwareStatus?> GetHardwareStatusAsync(
        int stationId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        if (_stationHardware.TryGetValue(stationId, out var cached) && cached.Expires > DateTimeOffset.UtcNow)
        {
            return cached.Value;
        }

        var token = await GetTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var url = $"{_options.ApiUrl.TrimEnd('/')}/api/devices/4/{stationId}/hardware-status";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _stationHardware[stationId] = (null, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(120)));
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Visualiti hardware {Status} estación {Station}", response.StatusCode, stationId);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var parsed = ParseHardwareStatus(doc.RootElement, stationId);
        _stationHardware[stationId] = (parsed, DateTimeOffset.UtcNow.Add(StationMetaTtl));
        return parsed;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VisualitiReading>> FetchMoistureReadingsAsync(
        string sensorSerial,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return [];
        }

        var physicalSerial = SensorCatalog.SplitLogicalId(sensorSerial).Physical;
        var stationNum = ParseVisualitiStationId(physicalSerial);
        if (stationNum is null)
        {
            _logger.LogDebug("Serial {Serial} no es estación Visualiti conocida", sensorSerial);
            return [];
        }

        var timeZoneId = await ResolveTimeZoneForSerialAsync(sensorSerial, cancellationToken);

        try
        {
            var requested = await FetchRawAsync(stationNum.Value, since, timeZoneId, cancellationToken);
            if (requested.Count > 0)
            {
                return requested;
            }

            // Proveedor sin puntos en la ventana pedida → ampliar lookback y filtrar.
            var fallbackSince = DateTimeOffset.UtcNow.AddDays(-180);
            if (since <= fallbackSince.AddDays(1))
            {
                _logger.LogInformation(
                    "Visualiti sin datos para estación {Station} desde {Since:u}",
                    stationNum,
                    since);
                return [];
            }

            _logger.LogInformation(
                "Visualiti sin datos en ventana pedida para {Station}; reintentando desde {Fallback:u}",
                stationNum,
                fallbackSince);

            var expanded = await FetchRawAsync(stationNum.Value, fallbackSince, timeZoneId, cancellationToken);
            if (expanded.Count == 0)
            {
                return [];
            }

            var inWindow = expanded.Where(r => r.FechaHora >= since).ToList();
            if (inWindow.Count > 0)
            {
                return inWindow;
            }

            // Últimos disponibles aunque queden fuera del rango (evita gráfico vacío).
            var span = DateTimeOffset.UtcNow - since;
            if (span < TimeSpan.FromHours(1))
            {
                span = TimeSpan.FromDays(7);
            }

            var last = expanded[^1].FechaHora;
            var sliced = expanded.Where(r => r.FechaHora >= last - span).ToList();
            if (sliced.Count == 0)
            {
                sliced = expanded.TakeLast(Math.Min(500, expanded.Count)).ToList();
            }

            _logger.LogInformation(
                "Devolviendo {Count} puntos históricos (último {Last:u}) fuera de ventana para estación {Station}",
                sliced.Count,
                last,
                stationNum);
            return sliced;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Error consultando Visualiti estación {Station} desde {Since:u}",
                stationNum,
                since);
            return [];
        }
    }

    private async Task<IReadOnlyList<VisualitiReading>> FetchAndCacheRangeAsync(
        string physicalSerial,
        string sensorSerial,
        string rangeKey,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        var since = HistoryRangeHelper.ToSince(rangeKey, timeZoneId);
        var readings = await FetchMoistureReadingsAsync(sensorSerial, since, cancellationToken);
        _cache.SetHistory(physicalSerial, rangeKey, readings, HistoryTtl);

        var latest = readings.LastOrDefault();
        if (latest is not null)
        {
            _cache.SetLatest(physicalSerial, latest, LatestTtl);
        }

        return readings;
    }

    private async Task<List<VisualitiReading>> FetchRawAsync(
        int stationNum,
        DateTimeOffset since,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return [];
        }

        var startUnix = since.ToUnixTimeSeconds();
        var endUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var url = $"{_options.ApiUrl.TrimEnd('/')}/api/devices/4/{stationNum}/data/{startUnix}/{endUnix}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Visualiti data {Status} para estación {Station}", response.StatusCode, stationNum);
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ParsePayload(doc.RootElement, stationNum, timeZoneId);
    }

    private async Task<string> ResolveTimeZoneForSerialAsync(
        string sensorSerial,
        CancellationToken cancellationToken)
    {
        var (physical, _) = SensorCatalog.SplitLogicalId(sensorSerial);

        // M### → tz por estación sin tocar DbContext (seguro en paralelo).
        var station = ParseVisualitiStationId(physical);
        if (station is int n)
        {
            return CountryTimeZoneResolver.ResolveForStation(n);
        }

        var sensor = await _catalog.GetSensorAsync(physical, cancellationToken);
        if (sensor is not null)
        {
            return sensor.TimeZoneId;
        }

        return CountryTimeZoneResolver.DefaultTimeZoneId;
    }

    /// <summary>
    /// Interpreta el JSON Visualiti (<c>data</c> por timestamp o <c>detail: Not Data Found</c>).
    /// </summary>
    public static List<VisualitiReading> ParsePayload(
        JsonElement root,
        int stationNum = 0,
        string timeZoneId = CountryTimeZoneResolver.DefaultTimeZoneId)
    {
        if (root.TryGetProperty("detail", out var detailEl))
        {
            var detail = detailEl.GetString();
            if (!string.IsNullOrWhiteSpace(detail) &&
                detail.Contains("not data", StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }
        }

        if (!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var result = new List<VisualitiReading>();
        foreach (var period in dataEl.EnumerateObject())
        {
            if (period.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var items = period.Value.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Object)
                .ToList();

            if (items.Count == 0)
            {
                continue;
            }

            var timestampRaw = items[0].TryGetProperty("time", out var t)
                ? t.GetString()
                : period.Name;
            var channels = ExtractChannels(items);
            if (channels.Count == 0)
            {
                continue;
            }

            result.Add(new VisualitiReading
            {
                FechaHora = ParseReadingTimestamp(timestampRaw ?? period.Name, timeZoneId),
                Valores = channels,
            });
        }

        return result.OrderBy(r => r.FechaHora).ToList();
    }

    /// <summary>
    /// Obtiene token Bearer (caché singleton + un solo login concurrente).
    /// Devuelve vacío si Visualiti no responde, sin lanzar excepción.
    /// </summary>
    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetToken(out var cachedToken))
        {
            return cachedToken;
        }

        return await _cache.RunExclusiveAsync(async ct =>
        {
            if (_cache.TryGetToken(out cachedToken))
            {
                return cachedToken;
            }

            var payload = new
            {
                cliente = _options.Cliente,
                usuario = _options.Usuario,
                password = _options.Password,
            };

            using var response = await _http.PostAsJsonAsync(_options.LoginUrl, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Visualiti login {Status}: {Reason}",
                    (int)response.StatusCode,
                    response.ReasonPhrase);
                return string.Empty;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("access_token", out var tokenEl))
            {
                _logger.LogWarning("Visualiti login sin access_token");
                return string.Empty;
            }

            var token = tokenEl.GetString();
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Visualiti login con access_token vacío");
                return string.Empty;
            }

            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp)
                ? exp.GetInt32()
                : 36000;

            _cache.SetToken(token, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
            return token;
        }, cancellationToken);
    }

    /// <summary>
    /// Parsea el número de estación Visualiti desde el serial M### (ignora sufijo -n).
    /// </summary>
    public static int? ParseVisualitiStationId(string sensorSerial)
    {
        var physical = SensorCatalog.SplitLogicalId(sensorSerial).Physical;
        var match = SerialRegex().Match(physical.Trim());
        if (!match.Success)
        {
            return null;
        }

        var id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return id > 0 ? id : null;
    }

    /// <summary>
    /// Parsea GET …/sensor → canales lógicos deduplicados.
    /// </summary>
    public static VisualitiStationSensors ParseStationSensors(JsonElement root)
    {
        var channels = new SortedSet<int>();
        if (root.ValueKind != JsonValueKind.Array)
        {
            return new VisualitiStationSensors();
        }

        foreach (var item in root.EnumerateArray())
        {
            var name = ExtractSensorName(item);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var match = ChannelNameRegex().Match(name);
            if (match.Success)
            {
                channels.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            }
        }

        return new VisualitiStationSensors { Channels = channels.ToList() };
    }

    /// <summary>
    /// Parsea GET …/hardware-status.
    /// </summary>
    public static VisualitiHardwareStatus? ParseHardwareStatus(JsonElement root, int stationId)
    {
        if (root.TryGetProperty("message", out _) && !root.TryGetProperty("coordenadas_estacion", out _))
        {
            return null;
        }

        double? lat = null;
        double? lng = null;
        if (root.TryGetProperty("coordenadas_estacion", out var coords))
        {
            lat = TryReadDouble(coords, "latitud");
            lng = TryReadDouble(coords, "longitud");
        }

        bool? online = null;
        string? connectivity = null;
        if (root.TryGetProperty("conectividad_estacion", out var conn))
        {
            if (conn.TryGetProperty("online", out var onlineEl) &&
                onlineEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                online = onlineEl.GetBoolean();
            }

            connectivity = conn.TryGetProperty("estado", out var est) ? est.GetString() : null;
        }

        string? sensorState = null;
        if (root.TryGetProperty("estado_sensores", out var estado) &&
            estado.TryGetProperty("estado", out var estadoEl))
        {
            sensorState = estadoEl.GetString();
        }

        string? stationName = null;
        if (root.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("estacion", out var estacion) &&
            estacion.TryGetProperty("nombre", out var nombre))
        {
            stationName = nombre.GetString();
        }

        return new VisualitiHardwareStatus
        {
            StationId = stationId,
            StationName = stationName,
            Latitude = lat,
            Longitude = lng,
            Online = online,
            SensorState = sensorState,
            Connectivity = connectivity,
        };
    }

    private static string? ExtractSensorName(JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in item.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.String)
                {
                    return part.GetString()?.Trim();
                }
            }

            return null;
        }

        if (item.ValueKind == JsonValueKind.Object)
        {
            if (item.TryGetProperty("sensor", out var sensorEl))
            {
                return sensorEl.GetString()?.Trim();
            }

            if (item.TryGetProperty("nombre", out var nameEl))
            {
                return nameEl.GetString()?.Trim();
            }
        }

        return item.ValueKind == JsonValueKind.String ? item.GetString()?.Trim() : null;
    }

    private static double? TryReadDouble(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetDouble(),
            JsonValueKind.String => double.TryParse(el.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                ? d
                : null,
            _ => null,
        };
    }

    /// <summary>
    /// Extrae canales Cont Vol{n} / Volumetrico{n} a porcentaje.
    /// </summary>
    public static Dictionary<int, double> ExtractChannels(IEnumerable<JsonElement> readings)
    {
        var values = new Dictionary<int, double>();
        foreach (var item in readings)
        {
            if (!item.TryGetProperty("sensor", out var sensorEl))
            {
                continue;
            }

            var name = sensorEl.GetString()?.Trim() ?? string.Empty;
            if (!item.TryGetProperty("value", out var valueEl) || valueEl.ValueKind is JsonValueKind.Null)
            {
                continue;
            }

            var match = ChannelNameRegex().Match(name);
            if (!match.Success)
            {
                continue;
            }

            double num;
            try
            {
                num = valueEl.ValueKind == JsonValueKind.Number
                    ? valueEl.GetDouble()
                    : double.Parse(valueEl.GetString() ?? "0", CultureInfo.InvariantCulture);
            }
            catch
            {
                continue;
            }

            values[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)] =
                MoisturePercentConverter.ToPercent(num);
        }

        return values;
    }

    /// <summary>
    /// Parsea timestamps Visualiti (hora local del país) con hora de 1 o 2 dígitos.
    /// </summary>
    public static DateTimeOffset ParseReadingTimestamp(
        string raw,
        string timeZoneId = CountryTimeZoneResolver.DefaultTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DateTimeOffset.UtcNow;
        }

        var tz = CountryTimeZoneResolver.GetTimeZone(timeZoneId);

        string[] formats =
        [
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd H:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd H:mm:ss",
        ];

        foreach (var fmt in formats)
        {
            if (DateTime.TryParseExact(
                    raw,
                    fmt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var local))
            {
                var offset = tz.GetUtcOffset(local);
                return new DateTimeOffset(local, offset);
            }
        }

        if (DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var loose))
        {
            var offset = tz.GetUtcOffset(loose);
            return new DateTimeOffset(loose, offset);
        }

        return DateTimeOffset.UtcNow;
    }

    [GeneratedRegex(@"^M(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SerialRegex();

    [GeneratedRegex(@"^(?:Cont\s*Vol|Volumetrico)\s*(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ChannelNameRegex();
}
