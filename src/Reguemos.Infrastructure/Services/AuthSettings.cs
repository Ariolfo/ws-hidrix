using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Reguemos.Application.Common.Interfaces;
using Reguemos.Infrastructure.Options;

namespace Reguemos.Infrastructure.Services;

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
