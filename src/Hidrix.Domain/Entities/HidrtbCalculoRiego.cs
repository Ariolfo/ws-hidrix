namespace Hidrix.Domain.Entities;

/// <summary>
/// Registro de una consulta en la calculadora de riego (por usuario).
/// </summary>
public class HidrtbCalculoRiego
{
    /// <summary>Identificador.</summary>
    public int CalcId { get; set; }

    /// <summary>Usuario que registró el cálculo (FK HidrtbUsuario).</summary>
    public string UsuaId { get; set; } = string.Empty;

    /// <summary>Nombre del cultivo (catálogo u otro escrito por el usuario).</summary>
    public string CalcCultivoNombre { get; set; } = string.Empty;

    /// <summary>Id de cultivo del catálogo si aplica.</summary>
    public int? CultId { get; set; }

    /// <summary>Capacidad de campo (%).</summary>
    public decimal CalcCapacidadCampo { get; set; }

    /// <summary>Límite máximo de riego (%).</summary>
    public decimal CalcLimiteMaxRiego { get; set; }

    /// <summary>Umbral de decisión de riego (%).</summary>
    public decimal CalcDecisionRiego { get; set; }

    /// <summary>Fecha de la consulta.</summary>
    public DateOnly CalcFechaConsulta { get; set; }

    /// <summary>Humedad de la mañana (%).</summary>
    public decimal CalcHumedadManana { get; set; }

    /// <summary>Humedad de la tarde (%).</summary>
    public decimal CalcHumedadTarde { get; set; }

    /// <summary>Recomendación: Regar / No regar.</summary>
    public string CalcRecomendacion { get; set; } = string.Empty;

    /// <summary>Si realizó el riego (Sí / No).</summary>
    public string? CalcRealizoRiego { get; set; }

    /// <summary>Observación libre.</summary>
    public string? CalcObservacion { get; set; }

    /// <summary>Registro activo.</summary>
    public bool CalcActivo { get; set; } = true;

    /// <summary>Fecha de creación UTC.</summary>
    public DateTime CalcFechaCreacion { get; set; }

    /// <summary>Fecha de actualización UTC.</summary>
    public DateTime CalcFechaActualizacion { get; set; }
}
