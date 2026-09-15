namespace Hidrix.Domain.Entities;

/// <summary>
/// Metadatos Hidrix de un sensor Visualiti (cultivo, finca, CC estimada).
/// El inventario de estaciones/canales/coords vive en Visualiti.
/// </summary>
public class HidrtbSensorMeta
{
    /// <summary>Identificador interno.</summary>
    public int MetaId { get; set; }

    /// <summary>Serial Visualiti (ej. M333).</summary>
    public string SensNombre { get; set; } = string.Empty;

    /// <summary>Cultivo asociado (opcional).</summary>
    public int? CultId { get; set; }

    /// <summary>Finca / parcela (agrupa en UI).</summary>
    public string? SensFinca { get; set; }

    /// <summary>Capacidad de campo estimada (%).</summary>
    public decimal? SensCcEstimado { get; set; }

    /// <summary>Método usado para estimar CC.</summary>
    public string? SensMetodoCc { get; set; }

    /// <summary>Fecha UTC de la última estimación de CC.</summary>
    public DateTime? SensFechaEstimacionCc { get; set; }

    /// <summary>Si el registro está activo.</summary>
    public bool MetaActivo { get; set; } = true;

    /// <summary>Fecha de creación UTC.</summary>
    public DateTime MetaFechaCreacion { get; set; }

    /// <summary>Fecha de actualización UTC.</summary>
    public DateTime MetaFechaActualizacion { get; set; }

    /// <summary>Navegación al cultivo.</summary>
    public HidrtbCultivo? Cultivo { get; set; }
}
