namespace Hidrix.Domain.Entities;

/// <summary>
/// Cultivo con parámetros de capacidad de campo / decisión de riego.
/// </summary>
public class HidrtbCultivo
{
    /// <summary>Identificador del cultivo.</summary>
    public int CultId { get; set; }

    /// <summary>Nombre del cultivo.</summary>
    public string CultNombre { get; set; } = string.Empty;

    /// <summary>Capacidad de campo (%).</summary>
    public decimal CultCapacidadCampo { get; set; }

    /// <summary>% máximo de riego (≈ 80 % CC).</summary>
    public decimal CultPorcentajeMaximo { get; set; }

    /// <summary>% decisión de riego (≈ 64 % CC).</summary>
    public decimal CultDecisionRiego { get; set; }

    /// <summary>Si el cultivo está activo.</summary>
    public bool CultActivo { get; set; } = true;

    /// <summary>Fecha de creación UTC.</summary>
    public DateTime CultFechaCreacion { get; set; }

    /// <summary>Fecha de actualización UTC.</summary>
    public DateTime CultFechaActualizacion { get; set; }

    /// <summary>Metadatos de sensores asociados.</summary>
    public ICollection<HidrtbSensorMeta> SensorMetas { get; set; } = new List<HidrtbSensorMeta>();
}
