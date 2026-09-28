using FluentValidation;
using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;

namespace Hidrix.Application.Features.Auth.Commands.Login;

/// <summary>
/// Comando de inicio de sesión con email y contraseña.
/// </summary>
public class LoginCommand : IRequest<AuthDto>
{
    /// <summary>Correo electrónico.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña.</summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Validador del comando de login.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Configura reglas de validación.</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(1);
    }
}

/// <summary>
/// Handler de login: valida credenciales y emite JWT con roles.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthDto>
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly IGeoRepository _geo;

    /// <summary>Inicializa el handler.</summary>
    public LoginCommandHandler(
        IIdentityService identity,
        IJwtTokenService jwt,
        IGeoRepository geo)
    {
        _identity = identity;
        _jwt = jwt;
        _geo = geo;
    }

    /// <summary>Ejecuta el login.</summary>
    public async ValueTask<AuthDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.LoginAsync(
            request.Email.Trim().ToLowerInvariant(),
            request.Password,
            cancellationToken);

        if (!result.Success)
            throw new UnauthorizedAppException("Credenciales inválidas o cuenta bloqueada.");

        var location = await _geo.GetUserLocationByUserIdAsync(result.UserId, cancellationToken);

        return new AuthDto
        {
            AccessToken = _jwt.CreateAccessToken(result.UserId, result.DisplayName, result.Roles),
            RefreshToken = _jwt.CreateRefreshToken(result.UserId, result.DisplayName),
            User = new UserDto
            {
                Id = result.UserId,
                Name = result.DisplayName,
                Email = result.Email,
                Roles = result.Roles,
                Country = location?.CountryName,
                Department = location?.DepartmentName,
                City = location?.CityName,
                CountryId = location?.CountryId,
                DepartmentId = location?.DepartmentId,
                CityId = location?.CityId,
            },
        };
    }
}
