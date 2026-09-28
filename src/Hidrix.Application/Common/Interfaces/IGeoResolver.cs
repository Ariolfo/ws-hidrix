namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Resultado de resolución geográfica a códigos de catálogo.
/// </summary>
public class ResolvedLocation
{
    /// <summary>Id de ciudad (única referencia geo del usuario).</summary>
    public int CiuId { get; set; }

    /// <summary>Id del país.</summary>
    public int PaisId { get; set; }

    /// <summary>Id del departamento.</summary>
    public int DepoId { get; set; }

    /// <summary>Código de país (Pais_Id como texto, informativo).</summary>
    public string CodigoPais { get; set; } = string.Empty;

    /// <summary>Código de departamento (informativo).</summary>
    public string CodigoDepartamento { get; set; } = string.Empty;

    /// <summary>Código de ciudad (informativo).</summary>
    public string CodigoCiudad { get; set; } = string.Empty;

    /// <summary>Nombre canónico del país.</summary>
    public string PaisNombre { get; set; } = string.Empty;

    /// <summary>Nombre canónico del departamento.</summary>
    public string DepartamentoNombre { get; set; } = string.Empty;

    /// <summary>Nombre canónico de la ciudad.</summary>
    public string CiudadNombre { get; set; } = string.Empty;
}

/// <summary>
/// Resuelve nombres geográficos contra HidrtbPais/Departamento/Ciudad.
/// </summary>
public interface IGeoResolver
{
    /// <summary>
    /// Resuelve país, departamento y ciudad a la ciudad master.
    /// </summary>
    Task<ResolvedLocation> ResolveAsync(
        string country,
        string department,
        string city,
        CancellationToken cancellationToken = default);
}
