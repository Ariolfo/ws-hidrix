using System.Globalization;
using System.Text;

namespace Hidrix.Application.Services;

/// <summary>
/// Perfil de humedad por cultivo (CC, 80 % CC y 64 % CC), alineado con la app.
/// </summary>
/// <param name="Name">Nombre del cultivo.</param>
/// <param name="FieldCapacity">Capacidad de campo (%).</param>
/// <param name="MaxIrrigationLimit">Límite superior de riego / 80 % CC (%).</param>
/// <param name="IrrigationDecision">Umbral de decisión / 64 % CC (%).</param>
public readonly record struct CropMoistureProfile(
    string Name,
    double FieldCapacity,
    double MaxIrrigationLimit,
    double IrrigationDecision);

/// <summary>
/// Catálogo de perfiles CC por cultivo.
/// </summary>
public static class CropMoistureProfiles
{
    /// <summary>Piso crítico del suelo (%), igual que el gráfico.</summary>
    public const double SoilFloor = 10.0;

    /// <summary>Aguacate: CC 39 %.</summary>
    public static readonly CropMoistureProfile Aguacate = new("Aguacate", 39, 31.2, 24.96);

    /// <summary>Cacao: CC 34 %.</summary>
    public static readonly CropMoistureProfile Cacao = new("Cacao", 34, 27.2, 21.76);

    /// <summary>Lima: CC 36 %.</summary>
    public static readonly CropMoistureProfile Lima = new("Lima", 36, 28.8, 23.04);

    /// <summary>Papaya: CC 34 %.</summary>
    public static readonly CropMoistureProfile Papaya = new("Papaya", 34, 27.2, 21.76);

    /// <summary>
    /// Resuelve el perfil a partir del cultivo o texto del sensor.
    /// </summary>
    /// <param name="cultivoOrText">Cultivo o nombre (p. ej. «Cacao», «Lima ácida Tahiti»).</param>
    /// <returns>Perfil CC; por defecto Cacao.</returns>
    public static CropMoistureProfile Resolve(string? cultivoOrText)
    {
        if (string.IsNullOrWhiteSpace(cultivoOrText))
        {
            return Cacao;
        }

        var normalized = Normalize(cultivoOrText);

        if (normalized.Contains("aguacate", StringComparison.Ordinal))
        {
            return Aguacate;
        }

        if (normalized.Contains("cacao", StringComparison.Ordinal))
        {
            return Cacao;
        }

        if (normalized.Contains("papaya", StringComparison.Ordinal))
        {
            return Papaya;
        }

        if (normalized.Contains("lima", StringComparison.Ordinal) ||
            normalized.Contains("tahiti", StringComparison.Ordinal))
        {
            return Lima;
        }

        return Cacao;
    }

    private static string Normalize(string text)
    {
        var formD = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
