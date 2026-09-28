namespace Hidrix.Domain.Entities;

/// <summary>
/// Catálogo de departamentos / provincias.
/// </summary>
public class HidrtbDepartamento
{
    /// <summary>Identificador del departamento.</summary>
    public int DepoId { get; set; }

    /// <summary>Identificador del país al que pertenece.</summary>
    public int PaisId { get; set; }

    /// <summary>Código textual del departamento.</summary>
    public string DepoCode { get; set; } = string.Empty;

    /// <summary>Nombre del departamento.</summary>
    public string DepoNombre { get; set; } = string.Empty;

    /// <summary>País navegación.</summary>
    public HidrtbPais? Pais { get; set; }

    /// <summary>Ciudades del departamento.</summary>
    public ICollection<HidrtbCiudad> Ciudades { get; set; } = new List<HidrtbCiudad>();
}
