using FluentValidation;
using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;

namespace Hidrix.Application.Features.Auth.Commands.Refresh;

/// <summary>
/// Comando de refresco de tokens JWT.
/// </summary>
public class RefreshCommand : IRequest<RefreshDto>
{
    /// <summary>Token de refresco.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Validador del comando de refresh.
/// </summary>
public class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    /// <summary>Configura reglas de validación.</summary>
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MinimumLength(10);
    }
}

/// <summary>
/// Handler que emite nuevos access/refresh con los roles actualizados del usuario.
/// </summary>
public class RefreshCommandHandler : IRequestHandler<RefreshCommand, RefreshDto>
{
    private readonly IJwtTokenService _jwt;
    private readonly IIdentityService _identity;

    /// <summary>Inicializa el handler.</summary>
    public RefreshCommandHandler(IJwtTokenService jwt, IIdentityService identity)
    {
        _jwt = jwt;
        _identity = identity;
    }

    /// <summary>Ejecuta el refresco.</summary>
    public async ValueTask<RefreshDto> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var (sub, name) = _jwt.ValidateToken(request.RefreshToken, "refresh");
            if (!await _identity.CanRefreshAsync(sub, cancellationToken))
            {
                throw new UnauthorizedAppException("Sesión expirada, vuelve a iniciar sesión.");
            }

            var roles = await _identity.GetUserRolesAsync(sub, cancellationToken);
            return new RefreshDto
            {
                AccessToken = _jwt.CreateAccessToken(sub, name, roles),
                RefreshToken = _jwt.CreateRefreshToken(sub, name),
            };
        }
        catch
        {
            throw new UnauthorizedAppException("Sesión expirada, vuelve a iniciar sesión.");
        }
    }
}
