namespace Hidrix.Application.Services;

/// <summary>
/// Conversión de valores Visualiti a porcentaje de humedad.
/// </summary>
public static class MoisturePercentConverter
{
    /// <summary>
    /// Si el valor es fracción (≤ 1.5) lo multiplica por 100; si no, lo redondea.
    /// </summary>
    /// <param name="value">Valor crudo de Visualiti.</param>
    /// <returns>Porcentaje con 2 decimales.</returns>
    public static double ToPercent(double value)
    {
        if (value is >= 0 and <= 1.5)
        {
            return Math.Round(value * 100.0, 2);
        }

        return Math.Round(value, 2);
    }
}

/// <summary>
/// Interpretación de humedad según umbrales del cultivo (3 zonas).
/// Estados: normal, drain, irrigate_deficit, no_data.
/// </summary>
public static class MoistureStatusInterpreter
{
    /// <summary>
    /// Interpreta el estado a partir de la humedad y el cultivo.
    /// </summary>
    /// <param name="value">Porcentaje o null.</param>
    /// <param name="cultivo">Cultivo del sensor (para resolver CC).</param>
    /// <returns>Status y mensaje de alerta.</returns>
    public static (string Status, string? AlertMessage) Interpret(
        double? value,
        string? cultivo = null)
    {
        if (value is null)
        {
            return ("no_data", null);
        }

        var profile = CropMoistureProfiles.Resolve(cultivo);
        return Interpret(value.Value, profile);
    }

    /// <summary>
    /// Interpreta el estado con un perfil CC explícito.
    /// </summary>
    /// <param name="value">Porcentaje de humedad.</param>
    /// <param name="profile">Perfil del cultivo.</param>
    /// <returns>Status y mensaje de alerta.</returns>
    public static (string Status, string? AlertMessage) Interpret(
        double value,
        CropMoistureProfile profile)
    {
        // Normal: entre decisión de riego (64 % CC) y límite máximo (80 % CC).
        if (value >= profile.IrrigationDecision && value <= profile.MaxIrrigationLimit)
        {
            return ("normal", "Normal, No regar");
        }

        // Arriba del límite máximo (80 % CC): drenar / saturación.
        if (value > profile.MaxIrrigationLimit)
        {
            return ("drain", "Alerta Drenar - saturación");
        }

        // Debajo de decisión de riego: regar por déficit.
        return ("irrigate_deficit", "Alerta REGAR por déficit");
    }
}
