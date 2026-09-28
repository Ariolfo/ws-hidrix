using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Infrastructure.Identity;
using Hidrix.Infrastructure.Options;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Implementación de IIdentityService usando ASP.NET Core Identity.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthOptions _authOptions;

    /// <summary>Inicializa el servicio.</summary>
    public IdentityService(UserManager<ApplicationUser> userManager, IOptions<AuthOptions> authOptions)
    {
        _userManager = userManager;
        _authOptions = authOptions.Value;
    }

    /// <inheritdoc />
    public async Task<RegisterResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        int? ciuId,
        bool emailConfirmed = true,
        CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            UsuaNombre = displayName,
            CiuId = ciuId,
            UsuaActivo = true,
            UsuaFechaRegistro = DateTime.UtcNow,
            UsuaFechaCreacion = DateTime.UtcNow,
            UsuaFechaActualizacion = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new RegisterResult(false, string.Empty, result.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);
        return new RegisterResult(
            true,
            user.Id,
            [],
            EmailConfirmationRequired: _authOptions.RequireEmailConfirmation && !emailConfirmed);
    }

    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.UsuaActivo)
        {
            return new LoginResult(false, string.Empty, string.Empty, string.Empty, [], ["Credenciales inválidas."]);
        }

        if (_authOptions.RequireEmailConfirmation && !user.EmailConfirmed)
        {
            return new LoginResult(
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                ["Confirme su correo electrónico antes de iniciar sesión."]);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return new LoginResult(false, string.Empty, string.Empty, string.Empty, [], ["Cuenta bloqueada temporalmente."]);
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            return new LoginResult(false, string.Empty, string.Empty, string.Empty, [], ["Credenciales inválidas."]);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        return new LoginResult(true, user.Id, user.UsuaNombre, user.Email ?? string.Empty, roles, []);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string[] Errors)> ConfirmEmailAsync(
        string email,
        string token,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is null)
        {
            return (false, ["Usuario no encontrado."]);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray());
        }

        return (true, []);
    }

    /// <inheritdoc />
    public async Task<string[]> GetUserRolesAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return [];
        return (await _userManager.GetRolesAsync(user)).ToArray();
    }

    /// <inheritdoc />
    public async Task<string[]> AssignRoleAsync(string userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ["Usuario no encontrado."];

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<bool> RemoveRoleAsync(string userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        var result = await _userManager.RemoveFromRoleAsync(user, role);
        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task<(string? DisplayName, string? Email)> GetUserInfoAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return (user?.UsuaNombre, user?.Email);
    }

    /// <inheritdoc />
    public async Task<bool> CanRefreshAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.UsuaActivo)
        {
            return false;
        }

        if (_authOptions.RequireEmailConfirmation && !user.EmailConfirmed)
        {
            return false;
        }

        return !await _userManager.IsLockedOutAsync(user);
    }
}
