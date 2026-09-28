namespace Hidrix.Application.Common.Interfaces;

/// <summary>Resultado de registro de usuario.</summary>
public record RegisterResult(bool Success, string UserId, string[] Errors, bool EmailConfirmationRequired = false);

/// <summary>Resultado de login.</summary>
public record LoginResult(bool Success, string UserId, string DisplayName, string Email, string[] Roles, string[] Errors);

/// <summary>
/// Abstracción sobre ASP.NET Core Identity para operaciones de usuario.
/// </summary>
public interface IIdentityService
{
    /// <summary>Registra un nuevo usuario y le asigna el rol User.</summary>
    Task<RegisterResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        int? ciuId,
        bool emailConfirmed = true,
        CancellationToken ct = default);

    /// <summary>Autentica usuario por email y contraseña.</summary>
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Confirma el correo con el token de Identity.</summary>
    Task<(bool Success, string[] Errors)> ConfirmEmailAsync(
        string email,
        string token,
        CancellationToken ct = default);

    /// <summary>Obtiene los roles activos del usuario.</summary>
    Task<string[]> GetUserRolesAsync(string userId, CancellationToken ct = default);

    /// <summary>Asigna un rol al usuario (reemplaza los anteriores si replaceExisting=true).</summary>
    Task<string[]> AssignRoleAsync(string userId, string role, CancellationToken ct = default);

    /// <summary>Quita un rol al usuario.</summary>
    Task<bool> RemoveRoleAsync(string userId, string role, CancellationToken ct = default);

    /// <summary>Busca un usuario por id y devuelve displayName y email.</summary>
    Task<(string? DisplayName, string? Email)> GetUserInfoAsync(string userId, CancellationToken ct = default);

    /// <summary>Indica si el usuario puede renovar tokens (activo y no bloqueado).</summary>
    Task<bool> CanRefreshAsync(string userId, CancellationToken ct = default);
}
