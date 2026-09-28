namespace Hidrix.Infrastructure.Options;

/// <summary>Opciones del endpoint /health.</summary>
public class HealthOptions
{
    /// <summary>Nombre de sección en configuración.</summary>
    public const string SectionName = "Health";

    /// <summary>Clave opcional para ver detalle de BD (header X-Health-Key).</summary>
    public string DetailedKey { get; set; } = string.Empty;
}
