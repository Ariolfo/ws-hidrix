using Microsoft.AspNetCore.Identity;

namespace Hidrix.Infrastructure.Identity;

/// <summary>Rechaza contraseñas demasiado comunes o predecibles.</summary>
public sealed class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        "12345678",
        "123456789",
        "password",
        "password1",
        "Password1",
        "qwerty123",
        "admin123",
        "hidrix123",
        "agrosavia",
        "11111111",
        "00000000",
        "abc12345",
        "letmein1",
        "welcome1",
        "changeme",
    };

    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        var normalized = password.Trim();
        if (Blocked.Contains(normalized))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "CommonPassword",
                Description = "La contraseña es demasiado común. Elija una más segura.",
            }));
        }

        if (normalized.Equals(user.Email, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(user.UserName)
                && normalized.Equals(user.UserName, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordMatchesUser",
                Description = "La contraseña no puede ser igual al correo o nombre de usuario.",
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
