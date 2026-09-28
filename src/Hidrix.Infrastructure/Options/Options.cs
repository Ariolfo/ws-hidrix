namespace Hidrix.Infrastructure.Options;

/// <summary>
/// Opciones de JWT (sección Jwt).
/// </summary>
public class JwtOptions
{
    /// <summary>Nombre de sección en configuración.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Secreto HS256.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Minutos de vigencia del access token.</summary>
    public int AccessMinutes { get; set; } = 60;

    /// <summary>Días de vigencia del refresh token.</summary>
    public int RefreshDays { get; set; } = 30;

    /// <summary>Emisor JWT (iss).</summary>
    public string Issuer { get; set; } = "hidrix-api";

    /// <summary>Audiencia JWT (aud).</summary>
    public string Audience { get; set; } = "hidrix-app";
}

/// <summary>
/// Opciones del cliente Visualiti (sección Visualiti).
/// </summary>
public class VisualitiOptions
{
    /// <summary>Nombre de sección en configuración.</summary>
    public const string SectionName = "Visualiti";

    /// <summary>Host unificado preferido (login + datos).</summary>
    public const string DefaultBaseUrl = "https://api.appgricultor.com";

    /// <summary>Habilita llamadas reales a Visualiti.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// URL base unificada (https://api.appgricultor.com).
    /// Si LoginUrl/ApiUrl están vacíos, se derivan de aquí.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// URL de login. Vacío → <c>{BaseUrl}/api/login</c>.
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL base de datos. Vacío → <see cref="BaseUrl"/>.
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>Cliente Visualiti.</summary>
    public string Cliente { get; set; } = string.Empty;

    /// <summary>Usuario Visualiti.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Contraseña Visualiti.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Verificar certificado SSL.</summary>
    public bool SslVerify { get; set; }

    /// <summary>TTL de devices /sensor /hardware-status (segundos).</summary>
    public int MetaCacheSeconds { get; set; } = 600;

    /// <summary>TTL del inventario físico construido (segundos).</summary>
    public int InventoryCacheSeconds { get; set; } = 600;

    /// <summary>
    /// Minutos máximos para servir inventario expirado si Visualiti falla.
    /// </summary>
    public int StaleInventoryMaxMinutes { get; set; } = 60;

    /// <summary>URL de API de datos resuelta.</summary>
    public string ResolveApiUrl()
    {
        var api = string.IsNullOrWhiteSpace(ApiUrl) ? BaseUrl : ApiUrl;
        return (api ?? DefaultBaseUrl).Trim().TrimEnd('/');
    }

    /// <summary>URL de login resuelta.</summary>
    public string ResolveLoginUrl()
    {
        if (!string.IsNullOrWhiteSpace(LoginUrl))
        {
            return LoginUrl.Trim();
        }

        return $"{ResolveApiUrl()}/api/login";
    }

    /// <summary>TTL de meta de estación.</summary>
    public TimeSpan MetaCacheTtl =>
        TimeSpan.FromSeconds(Math.Clamp(MetaCacheSeconds <= 0 ? 600 : MetaCacheSeconds, 30, 3600));

    /// <summary>TTL de inventario.</summary>
    public TimeSpan InventoryCacheTtl =>
        TimeSpan.FromSeconds(Math.Clamp(InventoryCacheSeconds <= 0 ? 600 : InventoryCacheSeconds, 30, 3600));

    /// <summary>Ventana máxima de inventario stale.</summary>
    public TimeSpan StaleInventoryWindow =>
        TimeSpan.FromMinutes(Math.Clamp(StaleInventoryMaxMinutes <= 0 ? 60 : StaleInventoryMaxMinutes, 1, 720));
}
