namespace Hidrix.Domain.Entities;

/// <summary>
/// Sensor de humedad (estación Visualiti M###).
/// </summary>
public class HidrtbSensor
{
    /// <summary>Identificador interno.</summary>
    public int SensId { get; set; }

    /// <summary>Nombre / serial (ej. M333, M312-1).</summary>
    public string SensNombre { get; set; } = string.Empty;

    /// <summary>Red a la que pertenece.</summary>
    public int RedId { get; set; }

    /// <summary>Cultivo asociado (opcional).</summary>
    public int? CultId { get; set; }

    /// <summary>Latitud WGS84.</summary>
    public decimal? SensLatitud { get; set; }

    /// <summary>Longitud WGS84.</summary>
    public decimal? SensLongitud { get; set; }

    /// <summary>Estado del sensor (bueno, etc.).</summary>
    public string? SensEstado { get; set; }

    /// <summary>Conectividad de estación (online/offline).</summary>
    public string? SensConectividad { get; set; }

    /// <summary>Finca / parcela (agrupa estaciones).</summary>
    public string? SensFinca { get; set; }

    /// <summary>Canales lógicos expuestos.</summary>
    public int SensCanales { get; set; } = 2;

    /// <summary>Si el registro está activo.</summary>
    public bool SensActivo { get; set; } = true;

    /// <summary>Fecha de creación UTC.</summary>
    public DateTime SensFechaCreacion { get; set; }

    /// <summary>Fecha de actualización UTC.</summary>
    public DateTime SensFechaActualizacion { get; set; }

    /// <summary>Capacidad de campo estimada (%).</summary>
    public decimal? SensCcEstimado { get; set; }

    /// <summary>Método usado para estimar CC (ej. SWDP-R).</summary>
    public string? SensMetodoCc { get; set; }

    /// <summary>Fecha UTC de la última estimación de CC.</summary>
    public DateTime? SensFechaEstimacionCc { get; set; }

    /// <summary>Navegación a la red.</summary>
    public HidrtbRed? Red { get; set; }

    /// <summary>Navegación al cultivo.</summary>
    public HidrtbCultivo? Cultivo { get; set; }
}
