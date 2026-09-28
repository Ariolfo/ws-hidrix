namespace Hidrix.Domain.Entities;

/// <summary>
/// Catálogo de ciudades / municipios / cantones.
/// Solo referencia a departamento (el país se obtiene vía Departamento → País).
/// </summary>
public class HidrtbCiudad
{
    /// <summary>Identificador de la ciudad.</summary>
    public int CiuId { get; set; }

    /// <summary>Nombre de la ciudad.</summary>
    public string CiuNombre { get; set; } = string.Empty;

    /// <summary>Identificador del departamento.</summary>
    public int DepoId { get; set; }

    /// <summary>Código de la ciudad (DANE / GeoNames admin2).</summary>
    public string CiuCod { get; set; } = string.Empty;

    /// <summary>Departamento navegación.</summary>
    public HidrtbDepartamento? Departamento { get; set; }
}
