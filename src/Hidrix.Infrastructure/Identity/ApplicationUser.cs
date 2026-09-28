using Microsoft.AspNetCore.Identity;
using Hidrix.Domain.Entities;

namespace Hidrix.Infrastructure.Identity;

/// <summary>
/// Usuario Identity con campos de dominio adicionales.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Nombre para mostrar del usuario.</summary>
    public string UsuaNombre { get; set; } = string.Empty;

    /// <summary>Ciudad de residencia (FK opcional).</summary>
    public int? CiuId { get; set; }

    /// <summary>Fecha de primer registro.</summary>
    public DateTime UsuaFechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de creación del registro.</summary>
    public DateTime UsuaFechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de última actualización.</summary>
    public DateTime UsuaFechaActualizacion { get; set; } = DateTime.UtcNow;

    /// <summary>Indica si el usuario está activo.</summary>
    public bool UsuaActivo { get; set; } = true;

    /// <summary>Navegación a ciudad.</summary>
    public HidrtbCiudad? Ciudad { get; set; }
}
