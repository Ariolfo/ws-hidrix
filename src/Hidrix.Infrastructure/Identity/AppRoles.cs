namespace Hidrix.Infrastructure.Identity;

/// <summary>
/// Constantes de roles de la aplicación.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = [Admin, User];
}
