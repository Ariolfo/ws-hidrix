using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Infrastructure.Options;

namespace Hidrix.Infrastructure.Services;

/// <summary>Lee políticas de autenticación desde configuración y entorno.</summary>
public sealed class AuthSettings : IAuthSettings
{
    private readonly AuthOptions _options;
    private readonly IHostEnvironment _environment;

    /// <summary>Inicializa el servicio.</summary>
    public AuthSettings(IOptions<AuthOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    /// <inheritdoc />
    public bool RequireEmailConfirmation => _options.RequireEmailConfirmation;

    /// <inheritdoc />
    public bool RequiresPendingEmailConfirmation() =>
        _options.RequireEmailConfirmation
        && !(_options.AutoConfirmEmailInDevelopment && _environment.IsDevelopment());
}
