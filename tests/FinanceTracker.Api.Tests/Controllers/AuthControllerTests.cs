using System.Net;
using System.Net.Http.Json;
using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FinanceTracker.Api.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly Mock<IAuthService> _authService = new(MockBehavior.Strict);
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthService>();
                services.AddScoped<IAuthService>(_ => _authService.Object);
            });
        });
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
    });

    private static AuthTokens NewTokens(string access = "access-jwt", string refresh = "refresh-token") => new(
        access,
        refresh,
        DateTime.UtcNow.AddMinutes(15),
        DateTime.UtcNow.AddDays(5));

    private static IList<string> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.ToList()
            : new List<string>();

    [Fact]
    public async Task Register_WithValidData_Returns201AndSetsCookies()
    {
        var request = new RegisterRequest("Jane", "jane@example.com", "password123");
        var tokens = NewTokens();
        _authService
            .Setup(s => s.RegisterAsync(It.Is<RegisterRequest>(r => r.Email == request.Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        var response = await CreateClient().PostAsJsonAsync("/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var setCookies = SetCookies(response);
        setCookies.Should().Contain(c => c.StartsWith("access_token=access-jwt"));
        setCookies.Should().Contain(c => c.StartsWith("refresh_token=refresh-token"));
        setCookies.Should().AllSatisfy(c =>
        {
            c.Should().Contain("httponly", Exactly.Once());
        });
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var request = new RegisterRequest("Jane", "dup@example.com", "password123");
        _authService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Email is already in use."));

        var response = await CreateClient().PostAsJsonAsync("/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        SetCookies(response).Should().BeEmpty();
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400()
    {
        var response = await CreateClient().PostAsJsonAsync("/register", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _authService.Verify(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndSetsCookies()
    {
        var request = new LoginRequest("jane@example.com", "password123");
        var tokens = NewTokens("login-access", "login-refresh");
        _authService
            .Setup(s => s.LoginAsync(It.Is<LoginRequest>(r => r.Email == request.Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        var response = await CreateClient().PostAsJsonAsync("/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var setCookies = SetCookies(response);
        setCookies.Should().Contain(c => c.StartsWith("access_token=login-access"));
        setCookies.Should().Contain(c => c.StartsWith("refresh_token=login-refresh"));
    }

    [Fact]
    public async Task Login_WithBadCredentials_Returns401()
    {
        var request = new LoginRequest("jane@example.com", "wrong");
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Invalid credentials."));

        var response = await CreateClient().PostAsJsonAsync("/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        SetCookies(response).Should().BeEmpty();
    }

    [Fact]
    public async Task Refresh_WithValidCookie_Returns200AndRotatesCookies()
    {
        var existingRefresh = "old-refresh";
        var newTokens = NewTokens("new-access", "new-refresh");
        _authService
            .Setup(s => s.RefreshAsync(existingRefresh, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newTokens);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/refresh");
        httpRequest.Headers.Add("Cookie", $"refresh_token={existingRefresh}");

        var response = await CreateClient().SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var setCookies = SetCookies(response);
        setCookies.Should().Contain(c => c.StartsWith("access_token=new-access"));
        setCookies.Should().Contain(c => c.StartsWith("refresh_token=new-refresh"));
    }

    [Fact]
    public async Task Refresh_WithMissingCookie_Returns401()
    {
        var response = await CreateClient().PostAsync("/refresh", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _authService.Verify(s => s.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithValidCookie_Returns200AndClearsCookies()
    {
        var refreshToken = "active-refresh";
        _authService
            .Setup(s => s.LogoutAsync(refreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/logout");
        httpRequest.Headers.Add("Cookie", $"refresh_token={refreshToken}");

        var response = await CreateClient().SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _authService.Verify(s => s.LogoutAsync(refreshToken, It.IsAny<CancellationToken>()), Times.Once);

        var setCookies = SetCookies(response);
        setCookies.Should().Contain(c => c.StartsWith("access_token=") && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        setCookies.Should().Contain(c => c.StartsWith("refresh_token=") && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }
}
