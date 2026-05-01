using System.IdentityModel.Tokens.Jwt;
using FinanceTracker.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Infrastructure.Tests.Services;

public class JwtServiceTests
{
    private const string TestSecret = "very-long-secret-key-for-hmac-sha256-must-be-256-bits-or-more";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private static IConfiguration BuildConfig(int accessExpiryMin = 15, int refreshExpiryDays = 5) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
                ["Jwt:Secret"] = TestSecret,
                ["Jwt:AccessTokenExpiryMinutes"] = accessExpiryMin.ToString(),
                ["Jwt:RefreshTokenExpiryDays"] = refreshExpiryDays.ToString(),
            })
            .Build();

    private static JwtService CreateService() => new(BuildConfig());

    [Fact]
    public void GenerateTokens_ReturnsNonEmptyAccessToken()
    {
        var tokens = CreateService().GenerateTokens(7, "alice@example.com");

        tokens.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateTokens_AccessTokenExpiresIn15Minutes()
    {
        var before = DateTime.UtcNow;

        var tokens = CreateService().GenerateTokens(7, "alice@example.com");

        var expected = before.AddMinutes(15);
        tokens.AccessTokenExpiry.Should().BeCloseTo(expected, TimeSpan.FromSeconds(5));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        jwt.ValidTo.Should().BeCloseTo(expected, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateTokens_RefreshTokenExpiresIn5Days()
    {
        var before = DateTime.UtcNow;

        var tokens = CreateService().GenerateTokens(7, "alice@example.com");

        tokens.RefreshTokenExpiry.Should().BeCloseTo(before.AddDays(5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateTokens_AccessTokenContainsCorrectUserId()
    {
        var tokens = CreateService().GenerateTokens(7, "alice@example.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "7");
    }

    [Fact]
    public void GenerateTokens_AccessTokenContainsCorrectEmail()
    {
        var tokens = CreateService().GenerateTokens(7, "alice@example.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "alice@example.com");
    }
}
