namespace Hidrix.Application.Services;

/// <summary>
/// Rangos de histórico soportados y conversión a ventana temporal.
/// </summary>
public static class HistoryRangeHelper
{
    /// <summary>Rangos válidos: today, 7d, 30d, 6m.</summary>
    public static readonly HashSet<string> ValidRanges = new(StringComparer.OrdinalIgnoreCase)
    {
        "today", "7d", "30d", "6m",
    };

    /// <summary>
    /// Indica si el rango es válido.
    /// </summary>
    /// <param name="rangeKey">Clave de rango.</param>
    /// <returns>True si es válido.</returns>
    public static bool IsValid(string rangeKey) => ValidRanges.Contains(rangeKey);

    /// <summary>
    /// Normaliza la clave de rango a minúsculas.
    /// </summary>
    /// <param name="rangeKey">Clave de entrada.</param>
    /// <returns>Clave normalizada.</returns>
    public static string Normalize(string rangeKey) => rangeKey.Trim().ToLowerInvariant();

    /// <summary>
    /// Calcula el inicio UTC del rango.
    /// </summary>
    /// <param name="rangeKey">today|7d|30d|6m.</param>
    /// <param name="timeZoneId">Zona IANA del país del sensor (para <c>today</c>).</param>
    /// <returns>Instantánea de inicio en UTC.</returns>
    public static DateTimeOffset ToSince(string rangeKey, string? timeZoneId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var normalized = Normalize(rangeKey);
        if (normalized == "today")
        {
            var tz = CountryTimeZoneResolver.GetTimeZone(
                timeZoneId ?? CountryTimeZoneResolver.DefaultTimeZoneId);
            var localNow = TimeZoneInfo.ConvertTime(now, tz);
            var localMidnight = new DateTime(
                localNow.Year,
                localNow.Month,
                localNow.Day,
                0,
                0,
                0,
                DateTimeKind.Unspecified);
            var offset = tz.GetUtcOffset(localMidnight);
            return new DateTimeOffset(localMidnight, offset);
        }

        return normalized switch
        {
            "7d" => now.AddDays(-7),
            "30d" => now.AddDays(-30),
            _ => now.AddDays(-180),
        };
    }
}
