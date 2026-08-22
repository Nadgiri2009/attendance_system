using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EWMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EWMS.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    // In production, persist refresh tokens in the database (see RefreshToken entity)
    // and validate against the store rather than only the signature/expiry.
    private static readonly Dictionary<string, (Guid userId, DateTime expires)> RefreshTokenStore = new();

    public TokenService(IConfiguration configuration) => _configuration = configuration;

    public TokenResult GenerateTokens(Guid userId, string userName, string email, IEnumerable<string> roles, Guid? employeeId)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (employeeId.HasValue)
            claims.Add(new Claim("employeeId", employeeId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expiresMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");
        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        var refreshDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");
        RefreshTokenStore[refreshToken] = (userId, DateTime.UtcNow.AddDays(refreshDays));

        return new TokenResult(accessToken, refreshToken, expires);
    }

    public (bool isValid, Guid userId) ValidateRefreshToken(string refreshToken)
    {
        if (RefreshTokenStore.TryGetValue(refreshToken, out var entry) && entry.expires > DateTime.UtcNow)
            return (true, entry.userId);

        return (false, Guid.Empty);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
