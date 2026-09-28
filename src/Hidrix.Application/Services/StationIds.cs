using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Hidrix.Application.Services;

/// <summary>
/// Codificación/decodificación de IDs de estación (finca o sensor individual).
/// </summary>
public static class StationIds
{
    /// <summary>
    /// Genera id estable: sn-{serial} o fin-{slug}.
    /// </summary>
    /// <param name="grupo">Nombre de finca o serial.</param>
    /// <returns>Id de estación.</returns>
    public static string EncodeStationId(string grupo)
    {
        if ((grupo.StartsWith('M') || grupo.StartsWith('C') || grupo.StartsWith('R')) && grupo.Length <= 6)
        {
            return $"sn-{grupo}";
        }

        return $"fin-{SlugHelper.Slugify(grupo, "-")}";
    }

    /// <summary>
    /// Parsea un id de estación.
    /// </summary>
    /// <param name="stationId">Id fin-... o sn-...</param>
    /// <returns>Tipo (finca|serial) y clave.</returns>
    public static (string Kind, string Key) ParseStationId(string stationId)
    {
        if (stationId.StartsWith("sn-", StringComparison.Ordinal))
        {
            var serial = stationId[3..].Trim();
            if (string.IsNullOrEmpty(serial))
            {
                throw new ArgumentException("ID de estación inválido");
            }

            return ("serial", serial);
        }

        if (stationId.StartsWith("fin-", StringComparison.Ordinal))
        {
            var slug = stationId[4..].Trim();
            if (string.IsNullOrEmpty(slug))
            {
                throw new ArgumentException("ID de estación inválido");
            }

            return ("finca", slug);
        }

        throw new ArgumentException("ID de estación inválido");
    }

    /// <summary>
    /// Slug de nombre de finca.
    /// </summary>
    /// <param name="nombreFinca">Nombre de finca.</param>
    /// <returns>Slug.</returns>
    public static string FincaSlug(string nombreFinca) =>
        SlugHelper.Slugify(nombreFinca.Trim(), "-");

    /// <summary>
    /// Clave de agrupación: finca si existe, si no el serial.
    /// </summary>
    /// <param name="serial">Serial físico.</param>
    /// <param name="nombreFinca">Nombre de finca opcional.</param>
    /// <returns>Clave de grupo.</returns>
    public static string GrupoFromSensor(string serial, string? nombreFinca)
    {
        var finca = (nombreFinca ?? string.Empty).Trim();
        return string.IsNullOrEmpty(finca) ? serial : finca;
    }
}

/// <summary>
/// Generación de slugs estilo python-slugify.
/// </summary>
internal static class SlugHelper
{
    internal static string Slugify(string text, string separator = "-")
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : separator);
        }

        var slug = Regex.Replace(sb.ToString(), $"{Regex.Escape(separator)}+", separator);
        return slug.Trim(separator.ToCharArray());
    }
}
