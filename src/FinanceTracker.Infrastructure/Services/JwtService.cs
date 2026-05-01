using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secret;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    public JwtService(IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        _issuer = jwt["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured.");
        _audience = jwt["Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured.");
        _secret = jwt["Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        _accessTokenExpiryMinutes = int.Parse(jwt["AccessTokenExpiryMinutes"] ?? "15");
        _refreshTokenExpiryDays = int.Parse(jwt["RefreshTokenExpiryDays"] ?? "5");
    }

    public AuthTokens GenerateTokens(int userId, string email)
    {
        var now = DateTime.UtcNow;
        var accessExpiry = now.AddMinutes(_accessTokenExpiryMinutes);
        var refreshExpiry = now.AddDays(_refreshTokenExpiryDays);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: accessExpiry,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new AuthTokens(accessToken, refreshToken, accessExpiry, refreshExpiry);
    }
}
