using Hidrix.Infrastructure.Auth;
using Hidrix.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Hidrix.Api.Auth;

/// <summary>Establece y elimina cookies HttpOnly de autenticación.</summary>
public class AuthCookieHelper
{
    private readonly JwtOptions _jwt;
    private readonly IHostEnvironment _environment;

    /// <summary>Inicializa el helper.</summary>
    public AuthCookieHelper(IOptions<JwtOptions> jwt, IHostEnvironment environment)
    {
        _jwt = jwt.Value;
        _environment = environment;
    }

    /// <summary>Indica si la petición solicita autenticación vía cookies.</summary>
    public static bool UsesCookieAuth(HttpRequest request) =>
        string.Equals(
            request.Headers[AuthCookieNames.AuthModeHeader].FirstOrDefault(),
            AuthCookieNames.AuthModeCookie,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Establece access y refresh en cookies HttpOnly.</summary>
    public void SetAuthCookies(HttpResponse response, string accessToken, string refreshToken)
    {
        var secure = RequestIsSecure(response.HttpContext.Request);
        var sameSite = secure ? SameSiteMode.None : SameSiteMode.Lax;

        response.Cookies.Append(
            AuthCookieNames.Access,
            accessToken,
            BuildOptions(_jwt.AccessMinutes * 60, secure, sameSite));

        response.Cookies.Append(
            AuthCookieNames.Refresh,
            refreshToken,
            BuildOptions(_jwt.RefreshDays * 24 * 60 * 60, secure, sameSite));
    }

    /// <summary>Elimina las cookies de autenticación.</summary>
    public void ClearAuthCookies(HttpResponse response)
    {
        var secure = RequestIsSecure(response.HttpContext.Request);
        var sameSite = secure ? SameSiteMode.None : SameSiteMode.Lax;
        var opts = BuildOptions(0, secure, sameSite);

        response.Cookies.Delete(AuthCookieNames.Access, opts);
        response.Cookies.Delete(AuthCookieNames.Refresh, opts);
    }

    private static CookieOptions BuildOptions(int maxAgeSeconds, bool secure, SameSiteMode sameSite) =>
        new()
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
            MaxAge = maxAgeSeconds > 0 ? TimeSpan.FromSeconds(maxAgeSeconds) : null,
        };

    private bool RequestIsSecure(HttpRequest request) =>
        request.IsHttps || (!_environment.IsDevelopment() && !IsLocalHost(request));
    
    private static bool IsLocalHost(HttpRequest request)
    {
        var host = request.Host.Host;
        return host is "localhost" or "127.0.0.1" or "::1";
    }
}
