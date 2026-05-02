using System.Net;
using System.Net.Http.Json;
using FinanceTracker.Api.Tests.TestInfrastructure;
using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FinanceTracker.Api.Tests.Controllers;

public class UserControllerTests : IDisposable
{
    private readonly Mock<IUserService> _userService = new(MockBehavior.Strict);
    private readonly WebApplicationFactory<Program> _factory;

    public UserControllerTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserService>();
                services.AddScoped<IUserService>(_ => _userService.Object);

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                });

                services
                    .AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient(int? userId = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.Value.ToString());
        }
        return client;
    }

    [Fact]
    public async Task GetUser_Authenticated_Returns200WithUserDto()
    {
        const int userId = 42;
        var dto = new UserDto(userId, "Jane", "jane@example.com");
        _userService
            .Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var response = await CreateClient(userId).GetAsync("/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>();
        body.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetUser_Unauthenticated_Returns401()
    {
        var response = await CreateClient().GetAsync("/user");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _userService.Verify(s => s.GetCurrentUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_Returns200WithUpdatedDto()
    {
        const int userId = 7;
        var request = new UpdateUserRequest("New Name", "new@example.com", null, null);
        var dto = new UserDto(userId, "New Name", "new@example.com");
        _userService
            .Setup(s => s.UpdateUserAsync(userId, It.Is<UpdateUserRequest>(r => r.Email == request.Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var response = await CreateClient(userId).PutAsJsonAsync("/update", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>();
        body.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task UpdateUser_WithDuplicateEmail_Returns409()
    {
        const int userId = 7;
        var request = new UpdateUserRequest(null, "taken@example.com", null, null);
        _userService
            .Setup(s => s.UpdateUserAsync(userId, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Email is already in use."));

        var response = await CreateClient(userId).PutAsJsonAsync("/update", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_Unauthenticated_Returns401()
    {
        var request = new UpdateUserRequest("X", null, null, null);

        var response = await CreateClient().PutAsJsonAsync("/update", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _userService.Verify(s => s.UpdateUserAsync(It.IsAny<int>(), It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
