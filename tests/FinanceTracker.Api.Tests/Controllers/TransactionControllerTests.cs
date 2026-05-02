using System.Net;
using System.Net.Http.Json;
using FinanceTracker.Api.Tests.TestInfrastructure;
using FinanceTracker.Application.DTOs.Transactions;
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

public class TransactionControllerTests : IDisposable
{
    private readonly Mock<ITransactionService> _transactionService = new(MockBehavior.Strict);
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionControllerTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ITransactionService>();
                services.AddScoped<ITransactionService>(_ => _transactionService.Object);

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

    private static TransactionDto SampleDto(int id = 1, int userId = 7) => new(
        id,
        userId,
        "Lunch",
        12.5m,
        "expense",
        new DateOnly(2026, 5, 1),
        false,
        DateTime.UtcNow,
        DateTime.UtcNow);

    [Fact]
    public async Task Create_WithValidData_Returns201WithDto()
    {
        const int userId = 7;
        var request = new CreateTransactionRequest("Lunch", 12.5m, "expense", new DateOnly(2026, 5, 1));
        var dto = SampleDto(userId: userId);
        _transactionService
            .Setup(s => s.CreateAsync(userId, It.Is<CreateTransactionRequest>(r => r.Title == request.Title), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var response = await CreateClient(userId).PostAsJsonAsync("/transaction", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TransactionDto>();
        body.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Create_WithNegativeAmount_Returns400()
    {
        const int userId = 7;
        var request = new CreateTransactionRequest("Lunch", -1m, "expense", null);
        _transactionService
            .Setup(s => s.CreateAsync(userId, It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Amount must be greater than zero."));

        var response = await CreateClient(userId).PostAsJsonAsync("/transaction", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var request = new CreateTransactionRequest("Lunch", 1m, "expense", null);

        var response = await CreateClient().PostAsJsonAsync("/transaction", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _transactionService.Verify(
            s => s.CreateAsync(It.IsAny<int>(), It.IsAny<CreateTransactionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WithValidData_Returns200WithDto()
    {
        const int userId = 7;
        var request = new UpdateTransactionRequest(1, "Updated", 20m, "expense", new DateOnly(2026, 5, 1));
        var dto = SampleDto(userId: userId) with { Title = "Updated", Amount = 20m };
        _transactionService
            .Setup(s => s.UpdateAsync(userId, It.Is<UpdateTransactionRequest>(r => r.TransactionId == 1 && r.Title == "Updated"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var response = await CreateClient(userId).PutAsJsonAsync("/transaction", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionDto>();
        body.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Update_WithOtherUsersTransaction_Returns404()
    {
        const int userId = 7;
        var request = new UpdateTransactionRequest(99, "X", 1m, "expense", null);
        _transactionService
            .Setup(s => s.UpdateAsync(userId, It.IsAny<UpdateTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Transaction not found."));

        var response = await CreateClient(userId).PutAsJsonAsync("/transaction", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Unauthenticated_Returns401()
    {
        var request = new UpdateTransactionRequest(1, "X", 1m, "expense", null);

        var response = await CreateClient().PutAsJsonAsync("/transaction", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _transactionService.Verify(
            s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateTransactionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTransactions_Returns200WithPagedResult()
    {
        const int userId = 7;
        var items = new[] { SampleDto(1, userId), SampleDto(2, userId) };
        var paged = new PagedResult<TransactionDto>(items, 2, 1, 20);
        _transactionService
            .Setup(s => s.GetPagedAsync(userId, It.IsAny<TransactionQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var response = await CreateClient(userId).GetAsync("/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TransactionDto>>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactions_WithFilters_PassesFiltersToService()
    {
        const int userId = 7;
        TransactionQueryParams? captured = null;
        _transactionService
            .Setup(s => s.GetPagedAsync(userId, It.IsAny<TransactionQueryParams>(), It.IsAny<CancellationToken>()))
            .Callback<int, TransactionQueryParams, CancellationToken>((_, q, _) => captured = q)
            .ReturnsAsync(new PagedResult<TransactionDto>(Array.Empty<TransactionDto>(), 0, 2, 10));

        var response = await CreateClient(userId).GetAsync(
            "/transactions?page=2&pageSize=10&sortBy=amount&sortOrder=asc&category=income&dateFrom=2026-01-01&dateTo=2026-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Page.Should().Be(2);
        captured.PageSize.Should().Be(10);
        captured.SortBy.Should().Be("amount");
        captured.SortOrder.Should().Be("asc");
        captured.Category.Should().Be("income");
        captured.DateFrom.Should().Be(new DateOnly(2026, 1, 1));
        captured.DateTo.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public async Task GetTransactions_Unauthenticated_Returns401()
    {
        var response = await CreateClient().GetAsync("/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _transactionService.Verify(
            s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<TransactionQueryParams>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Delete_WithValidId_Returns204()
    {
        const int userId = 7;
        const int transactionId = 1;
        _transactionService
            .Setup(s => s.SoftDeleteAsync(userId, transactionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await CreateClient(userId).DeleteAsync($"/transaction?id={transactionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _transactionService.Verify(s => s.SoftDeleteAsync(userId, transactionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithOtherUsersTransaction_Returns404()
    {
        const int userId = 7;
        const int transactionId = 99;
        _transactionService
            .Setup(s => s.SoftDeleteAsync(userId, transactionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Transaction not found."));

        var response = await CreateClient(userId).DeleteAsync($"/transaction?id={transactionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Unauthenticated_Returns401()
    {
        var response = await CreateClient().DeleteAsync("/transaction?id=1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _transactionService.Verify(
            s => s.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSummary_Returns200WithSummaryDto()
    {
        const int userId = 7;
        var summary = new TransactionSummaryDto(50m, 100m, 50m);
        _transactionService
            .Setup(s => s.GetSummaryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var response = await CreateClient(userId).GetAsync("/transactions/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionSummaryDto>();
        body.Should().BeEquivalentTo(summary);
    }

    [Fact]
    public async Task GetSummary_Unauthenticated_Returns401()
    {
        var response = await CreateClient().GetAsync("/transactions/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _transactionService.Verify(s => s.GetSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
