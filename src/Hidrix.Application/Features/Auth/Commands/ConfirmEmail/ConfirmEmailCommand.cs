using FluentValidation;
using Mediator;
using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;

namespace Hidrix.Application.Features.Auth.Commands.ConfirmEmail;

/// <summary>Confirma el correo de un usuario registrado.</summary>
public class ConfirmEmailCommand : IRequest<bool>
{
    /// <summary>Correo electrónico.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Token de confirmación (Identity).</summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>Validador del comando de confirmación de email.</summary>
public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    /// <summary>Configura reglas de validación.</summary>
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Token).NotEmpty().MinimumLength(10);
    }
}

/// <summary>Handler de confirmación de correo.</summary>
public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, bool>
{
    private readonly IIdentityService _identity;

    /// <summary>Inicializa el handler.</summary>
    public ConfirmEmailCommandHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    /// <summary>Ejecuta la confirmación.</summary>
    public async ValueTask<bool> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var (success, errors) = await _identity.ConfirmEmailAsync(
            request.Email.Trim().ToLowerInvariant(),
            request.Token,
            cancellationToken);

        if (!success)
        {
            throw new AppException(string.Join("; ", errors));
        }

        return true;
    }
}
