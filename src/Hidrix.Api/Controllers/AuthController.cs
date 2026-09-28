using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Hidrix.Api.Auth;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Common.Models;
using Hidrix.Application.DTOs;
using Hidrix.Application.Features.Auth.Commands.ConfirmEmail;
using Hidrix.Application.Features.Auth.Commands.Login;
using Hidrix.Application.Features.Auth.Commands.Refresh;
using Hidrix.Application.Features.Auth.Commands.Register;
using Hidrix.Infrastructure.Auth;

namespace Hidrix.Api.Controllers;

/// <summary>
/// Endpoints de autenticación (registro, login y refresh).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IGeoRepository _geo;
    private readonly IIdentityService _identity;
    private readonly AuthCookieHelper _cookies;

    /// <summary>Inicializa el controlador.</summary>
    public AuthController(
        IMediator mediator,
        IGeoRepository geo,
        IIdentityService identity,
        AuthCookieHelper cookies)
    {
        _mediator = mediator;
        _geo = geo;
        _identity = identity;
        _cookies = cookies;
    }

    /// <summary>
    /// Registra un nuevo usuario con email y contraseña. Se le asigna el rol User.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthDto>>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(command, cancellationToken);
        if (!data.EmailConfirmationRequired
            && AuthCookieHelper.UsesCookieAuth(Request)
            && !string.IsNullOrEmpty(data.AccessToken))
        {
            _cookies.SetAuthCookies(Response, data.AccessToken, data.RefreshToken);
            data.AccessToken = string.Empty;
            data.RefreshToken = string.Empty;
        }

        var message = data.EmailConfirmationRequired
            ? "Registro recibido. Confirme su correo electrónico para iniciar sesión."
            : "Registro exitoso";

        return Ok(ApiResponse<AuthDto>.Ok(data, message));
    }

    /// <summary>
    /// Inicia sesión con email y contraseña. Devuelve tokens JWT con roles.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthDto>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var data = await _mediator.Send(command, cancellationToken);
        if (AuthCookieHelper.UsesCookieAuth(Request))
        {
            _cookies.SetAuthCookies(Response, data.AccessToken, data.RefreshToken);
            data.AccessToken = string.Empty;
            data.RefreshToken = string.Empty;
        }

        return Ok(ApiResponse<AuthDto>.Ok(data, "Sesión iniciada"));
    }

    /// <summary>
    /// Renueva access y refresh tokens (incluye roles actualizados).
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<RefreshDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<RefreshDto>>> Refresh(
        [FromBody] RefreshCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            command.RefreshToken = Request.Cookies[AuthCookieNames.Refresh] ?? string.Empty;
        }

        var data = await _mediator.Send(command, cancellationToken);
        if (AuthCookieHelper.UsesCookieAuth(Request))
        {
            var refresh = data.RefreshToken ?? command.RefreshToken;
            _cookies.SetAuthCookies(Response, data.AccessToken, refresh);
            data.AccessToken = string.Empty;
            data.RefreshToken = string.Empty;
        }

        return Ok(ApiResponse<RefreshDto>.Ok(data, "Tokens renovados"));
    }

    /// <summary>
    /// Cierra sesión eliminando cookies HttpOnly (modo web).
    /// </summary>
    [HttpPost("logout")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> Logout()
    {
        _cookies.ClearAuthCookies(Response);
        return Ok(ApiResponse<object>.Ok(null!, "Sesión cerrada"));
    }

    /// <summary>
    /// Perfil del usuario autenticado (roles desde el servidor, no del cliente).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("Usuario no autenticado"));
        }

        var (displayName, email) = await _identity.GetUserInfoAsync(userId, cancellationToken);
        displayName ??= User.FindFirstValue("name");
        email ??= User.FindFirstValue(ClaimTypes.Email);

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();
        var location = await _geo.GetUserLocationByUserIdAsync(userId, cancellationToken);

        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = userId,
            Name = displayName ?? string.Empty,
            Email = email ?? string.Empty,
            Roles = roles,
            Country = location?.CountryName,
            Department = location?.DepartmentName,
            City = location?.CityName,
            CountryId = location?.CountryId,
            DepartmentId = location?.DepartmentId,
            CityId = location?.CityId,
        }));
    }

    /// <summary>
    /// Confirma el correo con el token enviado por email (Identity).
    /// </summary>
    [HttpPost("confirm-email")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmEmail(
        [FromBody] ConfirmEmailCommand command,
        CancellationToken cancellationToken)
    {
        var ok = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(ok, "Correo confirmado"));
    }

    /// <summary>
    /// Ubicación del usuario autenticado (país, departamento y ciudad del catálogo).
    /// </summary>
    [HttpGet("location")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserLocationDto>>> GetLocation(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<UserLocationDto>.Fail("Usuario no autenticado"));
        }

        var location = await _geo.GetUserLocationByUserIdAsync(userId, cancellationToken);
        if (location is null)
        {
            return NotFound(ApiResponse<UserLocationDto>.Fail("El usuario no tiene ciudad registrada."));
        }

        return Ok(ApiResponse<UserLocationDto>.Ok(location));
    }
}
