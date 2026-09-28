using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Common.Models;
using Hidrix.Infrastructure.Identity;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Administración de usuarios con rol Admin (solo Admin).
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGeoResolver _geoResolver;
    private readonly IGeoRepository _geo;

    /// <summary>Inicializa el controlador.</summary>
    public AdminController(
        UserManager<ApplicationUser> userManager,
        IGeoResolver geoResolver,
        IGeoRepository geo)
    {
        _userManager = userManager;
        _geoResolver = geoResolver;
        _geo = geo;
    }

    /// <summary>Lista usuarios con rol Admin.</summary>
    [HttpGet("admins")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminUserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserDto>>>> ListAdmins(
        CancellationToken cancellationToken)
    {
        var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        var data = admins
            .OrderBy(u => u.UsuaNombre)
            .Select(u => ToDto(u))
            .ToList();
        return Ok(ApiResponse<IReadOnlyList<AdminUserDto>>.Ok(data));
    }

    /// <summary>Obtiene un admin por id.</summary>
    [HttpGet("admins/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> GetAdmin(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return NotFound(ApiResponse<AdminUserDto>.Fail("Admin no encontrado."));
        }

        return Ok(ApiResponse<AdminUserDto>.Ok(await ToDtoAsync(user, cancellationToken)));
    }

    /// <summary>Crea un nuevo usuario Admin.</summary>
    [HttpPost("admins")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> CreateAdmin(
        [FromBody] CreateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var name = (request.Name ?? string.Empty).Trim();
        var password = request.Password ?? string.Empty;
        var country = (request.Country ?? string.Empty).Trim();
        var department = (request.Department ?? string.Empty).Trim();
        var city = (request.City ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Nombre, correo y contraseña son obligatorios."));
        }

        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(department) || string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("País, departamento y ciudad son obligatorios."));
        }

        if (password.Length < 8)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("La contraseña debe tener al menos 8 caracteres."));
        }

        ResolvedLocation location;
        try
        {
            location = await _geoResolver.ResolveAsync(country, department, city, cancellationToken);
        }
        catch (AppException ex)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail(ex.Message));
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (await _userManager.IsInRoleAsync(existing, AppRoles.Admin))
            {
                return Conflict(ApiResponse<AdminUserDto>.Fail("Ya existe un admin con ese correo."));
            }

            // Promueve usuario existente a Admin y reactiva.
            existing.UsuaActivo = true;
            existing.UsuaFechaActualizacion = DateTime.UtcNow;
            existing.CiuId = location.CiuId;
            if (!string.IsNullOrWhiteSpace(name))
            {
                existing.UsuaNombre = name;
            }

            var update = await _userManager.UpdateAsync(existing);
            if (!update.Succeeded)
            {
                return BadRequest(ApiResponse<AdminUserDto>.Fail(string.Join("; ", update.Errors.Select(e => e.Description))));
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(existing);
                var pwdResult = await _userManager.ResetPasswordAsync(existing, token, password);
                if (!pwdResult.Succeeded)
                {
                    return BadRequest(ApiResponse<AdminUserDto>.Fail(string.Join("; ", pwdResult.Errors.Select(e => e.Description))));
                }
            }

            await _userManager.AddToRoleAsync(existing, AppRoles.Admin);
            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<AdminUserDto>.Ok(await ToDtoAsync(existing, cancellationToken), "Admin creado"));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            UsuaNombre = name,
            CiuId = location.CiuId,
            UsuaActivo = true,
            UsuaFechaRegistro = DateTime.UtcNow,
            UsuaFechaCreacion = DateTime.UtcNow,
            UsuaFechaActualizacion = DateTime.UtcNow,
        };

        var create = await _userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail(string.Join("; ", create.Errors.Select(e => e.Description))));
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Admin);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<AdminUserDto>.Ok(await ToDtoAsync(user, cancellationToken), "Admin creado"));
    }

    /// <summary>Actualiza datos básicos de un admin.</summary>
    [HttpPut("admins/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateAdmin(
        string userId,
        [FromBody] UpdateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return NotFound(ApiResponse<AdminUserDto>.Fail("Admin no encontrado."));
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("El nombre es obligatorio."));
        }

        user.UsuaNombre = name;
        user.UsuaFechaActualizacion = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));
        }

        return Ok(ApiResponse<AdminUserDto>.Ok(await ToDtoAsync(user, cancellationToken), "Admin actualizado"));
    }

    /// <summary>Activa o inactiva un admin.</summary>
    [HttpPut("admins/{userId}/active")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> SetActive(
        string userId,
        [FromBody] SetAdminActiveRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return NotFound(ApiResponse<AdminUserDto>.Fail("Admin no encontrado."));
        }

        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!request.Active && string.Equals(user.Id, currentId, StringComparison.Ordinal))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("No puedes inactivar tu propia cuenta."));
        }

        if (!request.Active)
        {
            var activeAdmins = (await _userManager.GetUsersInRoleAsync(AppRoles.Admin))
                .Count(u => u.UsuaActivo && u.Id != user.Id);
            if (activeAdmins < 1)
            {
                return BadRequest(ApiResponse<AdminUserDto>.Fail("Debe quedar al menos un admin activo."));
            }
        }

        user.UsuaActivo = request.Active;
        user.UsuaFechaActualizacion = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));
        }

        var msg = request.Active ? "Admin activado" : "Admin inactivado";
        return Ok(ApiResponse<AdminUserDto>.Ok(await ToDtoAsync(user, cancellationToken), msg));
    }

    /// <summary>Elimina un admin (borra el usuario).</summary>
    [HttpDelete("admins/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<AdminActionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminActionDto>>> DeleteAdmin(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return NotFound(ApiResponse<AdminActionDto>.Fail("Admin no encontrado."));
        }

        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.Equals(user.Id, currentId, StringComparison.Ordinal))
        {
            return BadRequest(ApiResponse<AdminActionDto>.Fail("No puedes eliminar tu propia cuenta."));
        }

        var otherAdmins = (await _userManager.GetUsersInRoleAsync(AppRoles.Admin))
            .Count(u => u.Id != user.Id);
        if (otherAdmins < 1)
        {
            return BadRequest(ApiResponse<AdminActionDto>.Fail("Debe quedar al menos un admin."));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<AdminActionDto>.Fail(string.Join("; ", result.Errors.Select(e => e.Description))));
        }

        return Ok(ApiResponse<AdminActionDto>.Ok(new AdminActionDto { Ok = true }, "Admin eliminado"));
    }

    private static AdminUserDto ToDto(ApplicationUser u) => new()
    {
        Id = u.Id,
        Name = u.UsuaNombre,
        Email = u.Email ?? string.Empty,
        Active = u.UsuaActivo,
        RegisteredAt = u.UsuaFechaRegistro,
    };

    private async Task<AdminUserDto> ToDtoAsync(
        ApplicationUser u,
        CancellationToken cancellationToken)
    {
        var dto = ToDto(u);
        var location = await _geo.GetUserLocationByUserIdAsync(u.Id, cancellationToken);
        if (location is null)
        {
            return dto;
        }

        dto.Country = location.CountryName;
        dto.Department = location.DepartmentName;
        dto.City = location.CityName;
        dto.CountryId = location.CountryId;
        dto.DepartmentId = location.DepartmentId;
        dto.CityId = location.CityId;
        return dto;
    }
}

/// <summary>DTO de admin para listado/detalle.</summary>
public sealed class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string? Country { get; set; }
    public string? Department { get; set; }
    public string? City { get; set; }
    public int? CountryId { get; set; }
    public int? DepartmentId { get; set; }
    public int? CityId { get; set; }
}

/// <summary>Request para crear admin.</summary>
public sealed class CreateAdminRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

/// <summary>Request para actualizar admin.</summary>
public sealed class UpdateAdminRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Request para activar/inactivar.</summary>
public sealed class SetAdminActiveRequest
{
    public bool Active { get; set; }
}

/// <summary>Respuesta de acciones sin entidad.</summary>
public sealed class AdminActionDto
{
    public bool Ok { get; set; }
}
