using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Infrastructure.Options;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Emisión y validación de JWT HS256 con soporte de roles.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;

    /// <summary>Inicializa el servicio JWT.</summary>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
    }

    /// <inheritdoc />
    public string CreateAccessToken(string userId, string? name = null, IEnumerable<string>? roles = null) =>
        CreateToken(userId, "access", TimeSpan.FromMinutes(_options.AccessMinutes), name, roles);

    /// <inheritdoc />
    public string CreateRefreshToken(string userId, string? name = null) =>
        CreateToken(userId, "refresh", TimeSpan.FromDays(_options.RefreshDays), name, null);

    /// <inheritdoc />
    public (string Sub, string? Name) ValidateToken(string token, string expectedType)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, BuildValidationParameters(), out var validated);

        var jwt = (JwtSecurityToken)validated;
        var type = jwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        if (!string.Equals(type, expectedType, StringComparison.Ordinal))
            throw new SecurityTokenException("Tipo de token inválido");

        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? throw new SecurityTokenException("Token sin sub");
        var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        return (sub, name);
    }

    private string CreateToken(string userId, string type, TimeSpan lifetime, string? name, IEnumerable<string>? roles)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new("type", type),
            new(JwtRegisteredClaimNames.Iat, Epoch(now).ToString(), ClaimValueTypes.Integer64),
        };

        if (!string.IsNullOrWhiteSpace(name))
            claims.Add(new Claim("name", name.Trim()));

        if (roles is not null)
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters BuildValidationParameters() =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

    private static long Epoch(DateTime utc) => new DateTimeOffset(utc).ToUnixTimeSeconds();
}
