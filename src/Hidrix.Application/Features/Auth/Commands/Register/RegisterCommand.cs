using FluentValidation;
using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;

namespace Hidrix.Application.Features.Auth.Commands.Register;

/// <summary>
/// Comando de auto-registro de usuario.
/// </summary>
public class RegisterCommand : IRequest<AuthDto>
{
    /// <summary>Correo electrónico (usado como login).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña (mínimo 8 caracteres).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Nombre para mostrar.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>País (nombre).</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Departamento.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Ciudad.</summary>
    public string City { get; set; } = string.Empty;
}

/// <summary>
/// Validador del comando de registro.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>Configura reglas de validación.</summary>
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(150);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(150);
    }
}

/// <summary>
/// Handler de auto-registro: resuelve geo, crea usuario Identity con rol User y emite JWT.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthDto>
{
    private readonly IIdentityService _identity;
    private readonly IGeoResolver _geoResolver;
    private readonly IJwtTokenService _jwt;
    private readonly IAuthSettings _authSettings;

    /// <summary>Inicializa el handler.</summary>
    public RegisterCommandHandler(
        IIdentityService identity,
        IGeoResolver geoResolver,
        IJwtTokenService jwt,
        IAuthSettings authSettings)
    {
        _identity = identity;
        _geoResolver = geoResolver;
        _jwt = jwt;
        _authSettings = authSettings;
    }

    /// <summary>Ejecuta el registro.</summary>
    public async ValueTask<AuthDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        ResolvedLocation location;
        try
        {
            location = await _geoResolver.ResolveAsync(
                request.Country, request.Department, request.City, cancellationToken);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AppException(ex.Message);
        }

        var emailConfirmed = !_authSettings.RequiresPendingEmailConfirmation();
        var result = await _identity.RegisterAsync(
            request.Email.Trim().ToLowerInvariant(),
            request.Password,
            request.Name.Trim(),
            location.CiuId,
            emailConfirmed,
            cancellationToken);

        if (!result.Success)
            throw new AppException(string.Join("; ", result.Errors));

        var nombre = request.Name.Trim();
        var roles = await _identity.GetUserRolesAsync(result.UserId, cancellationToken);
        var userDto = new UserDto
        {
            Id = result.UserId,
            Name = nombre,
            Email = request.Email.Trim().ToLowerInvariant(),
            Roles = roles,
            Country = location.PaisNombre,
            Department = location.DepartamentoNombre,
            City = location.CiudadNombre,
            CountryId = location.PaisId,
            DepartmentId = location.DepoId,
            CityId = location.CiuId,
        };

        if (result.EmailConfirmationRequired)
        {
            return new AuthDto
            {
                EmailConfirmationRequired = true,
                User = userDto,
            };
        }

        return new AuthDto
        {
            AccessToken = _jwt.CreateAccessToken(result.UserId, nombre, roles),
            RefreshToken = _jwt.CreateRefreshToken(result.UserId, nombre),
            User = userDto,
        };
    }
}
