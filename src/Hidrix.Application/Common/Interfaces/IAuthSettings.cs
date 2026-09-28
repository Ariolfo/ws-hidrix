namespace Hidrix.Application.Common.Interfaces;

/// <summary>Políticas de autenticación configurables.</summary>
public interface IAuthSettings
{
    /// <summary>Indica si el registro debe crear usuarios sin confirmar email.</summary>
    bool RequiresPendingEmailConfirmation();

    /// <summary>Indica si el login exige email confirmado.</summary>
    bool RequireEmailConfirmation { get; }
}
