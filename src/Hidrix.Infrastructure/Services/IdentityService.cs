using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Infrastructure.Identity;
using Hidrix.Infrastructure.Options;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Implementación de IIdentityService usando ASP.NET Core Identity.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthOptions _authOptions;
    private readonly HidrixDbContext _db;

    /// <summary>Inicializa el servicio.</summary>
    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IOptions<AuthOptions> authOptions,
        HidrixDbContext db)
    {
        _userManager = userManager;
        _authOptions = authOptions.Value;
        _db = db;
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

    /// <inheritdoc />
    public async Task<DeleteAccountResult> DeleteAccountAsync(
        string userId,
        string password,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new DeleteAccountResult(false, "Usuario no encontrado.");
        }

        if (string.IsNullOrEmpty(password) || !await _userManager.CheckPasswordAsync(user, password))
        {
            return new DeleteAccountResult(false, "La contraseña no es correcta.", InvalidPassword: true);
        }

        // Evita dejar el sistema sin administradores.
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            if (admins.Count <= 1)
            {
                return new DeleteAccountResult(
                    false,
                    "No se puede eliminar la única cuenta de administrador. Asigne otro administrador primero.");
            }
        }

        // Las tablas de notas y cálculos tienen FK a HidrtbUsuario sin cascada:
        // se borran primero, y todo va en una sola transacción.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.NotasEventoRiego.Where(n => n.UsuaId == userId).ExecuteDeleteAsync(ct);
        await _db.CalculosRiego.Where(c => c.UsuaId == userId).ExecuteDeleteAsync(ct);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            await tx.RollbackAsync(ct);
            return new DeleteAccountResult(false, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await tx.CommitAsync(ct);
        return new DeleteAccountResult(true);
    }
}
