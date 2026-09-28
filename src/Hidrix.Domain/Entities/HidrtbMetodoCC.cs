namespace Hidrix.Domain.Entities;

/// <summary>
/// Método para determinar o estimar la capacidad de campo (CC).
/// </summary>
public class HidrtbMetodoCC
{
    /// <summary>Identificador.</summary>
    public int MetoId { get; set; }

    /// <summary>Nombre del método.</summary>
    public string MetoNombre { get; set; } = string.Empty;

    /// <summary>Descripción / procedimiento.</summary>
    public string? MetoDescripcion { get; set; }

    /// <summary>Si está activo.</summary>
    public bool MetoActivo { get; set; } = true;

    /// <summary>Fecha de creación UTC.</summary>
    public DateTime MetoFechaCreacion { get; set; }

    /// <summary>Fecha de actualización UTC.</summary>
    public DateTime MetoFechaActualizacion { get; set; }
}
