namespace Hidrix.Infrastructure.Options;

/// <summary>Opciones de autenticación (sección Auth).</summary>
public class AuthOptions
{
    /// <summary>Nombre de sección en configuración.</summary>
    public const string SectionName = "Auth";

    /// <summary>Exige confirmación de correo antes de emitir sesión (recomendado en producción).</summary>
    public bool RequireEmailConfirmation { get; set; }

    /// <summary>Auto-confirma email al registrarse en Development (flujo local sin SMTP).</summary>
    public bool AutoConfirmEmailInDevelopment { get; set; } = true;
}
