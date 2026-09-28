namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Servicio de emisión y validación de JWT HS256.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Crea un token de acceso con roles opcionales.</summary>
    string CreateAccessToken(string userId, string? name = null, IEnumerable<string>? roles = null);

    /// <summary>Crea un token de refresco.</summary>
    string CreateRefreshToken(string userId, string? name = null);

    /// <summary>Valida un token y exige el tipo indicado (access|refresh).</summary>
    (string Sub, string? Name) ValidateToken(string token, string expectedType);
}
