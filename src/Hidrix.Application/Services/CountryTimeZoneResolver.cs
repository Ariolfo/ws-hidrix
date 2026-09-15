namespace Hidrix.Application.Services;

/// <summary>
/// Zona horaria IANA por país de la red del sensor (Visualiti envía hora local del país).
/// </summary>
public static class CountryTimeZoneResolver
{
    /// <summary>Fallback cuando no hay país asociado.</summary>
    public const string DefaultTimeZoneId = "America/Bogota";

    private static readonly Dictionary<int, string> ByCountryId = new()
    {
        [170] = "America/Bogota",
        [218] = "America/Guayaquil",
        [340] = "America/Tegucigalpa",
    };

    private static readonly Dictionary<string, string> ByCountryName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["COLOMBIA"] = "America/Bogota",
            ["ECUADOR"] = "America/Guayaquil",
            ["HONDURAS"] = "America/Tegucigalpa",
        };

    /// <summary>Resuelve IANA tz desde el id del catágeo (<see cref="HidrtbPais.PaisId"/>).</summary>
    public static string ResolveForCountryId(int? countryId) =>
        countryId is int id && ByCountryId.TryGetValue(id, out var tz)
            ? tz
            : DefaultTimeZoneId;

    /// <summary>Resuelve IANA tz desde el nombre del país.</summary>
    public static string ResolveForCountryName(string? countryName) =>
        !string.IsNullOrWhiteSpace(countryName)
        && ByCountryName.TryGetValue(countryName.Trim(), out var tz)
            ? tz
            : DefaultTimeZoneId;

    /// <summary>Resuelve IANA tz desde el número de estación Visualiti (M###).</summary>
    public static string ResolveForStation(int stationNum) =>
        stationNum switch
        {
            >= 312 and <= 323 or 336 => "America/Bogota",
            >= 333 and <= 335 => "America/Guayaquil",
            >= 324 and <= 332 => "America/Tegucigalpa",
            _ => DefaultTimeZoneId,
        };

    /// <summary>Obtiene <see cref="TimeZoneInfo"/> tolerando ids desconocidos.</summary>
    public static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
