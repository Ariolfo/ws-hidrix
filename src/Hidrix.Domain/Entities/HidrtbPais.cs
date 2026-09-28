namespace Hidrix.Domain.Entities;

/// <summary>
/// Catálogo de países.
/// </summary>
public class HidrtbPais
{
    /// <summary>Identificador numérico del país.</summary>
    public int PaisId { get; set; }

    /// <summary>Nombre del país.</summary>
    public string PaisNombre { get; set; } = string.Empty;

    /// <summary>Estado del registro (por defecto 2).</summary>
    public int PaisEstado { get; set; } = 2;

    /// <summary>Departamentos asociados al país.</summary>
    public ICollection<HidrtbDepartamento> Departamentos { get; set; } = new List<HidrtbDepartamento>();
}
