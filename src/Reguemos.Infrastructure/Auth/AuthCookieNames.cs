namespace Reguemos.Infrastructure.Auth;

/// <summary>Nombres de cookies HttpOnly para tokens JWT.</summary>
public static class AuthCookieNames
{
    /// <summary>Cookie del access token.</summary>
    public const string Access = "reguemos_access";

    /// <summary>Cookie del refresh token.</summary>
    public const string Refresh = "reguemos_refresh";

    /// <summary>Header que indica al backend que use cookies HttpOnly en lugar del cuerpo JSON.</summary>
    public const string AuthModeHeader = "X-Reguemos-Auth-Mode";

    /// <summary>Valor del header para modo cookie.</summary>
    public const string AuthModeCookie = "cookie";
}
