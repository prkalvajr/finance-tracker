using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace FinanceTracker.Application.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _repoMock = new();

    private TransactionService CreateService() => new(_repoMock.Object);

    [Fact]
    public async Task Create_WithValidData_ReturnsTransactionDto()
    {
        var userId = 7;
        var date = new DateOnly(2026, 5, 1);
        var request = new CreateTransactionRequest("Coffee", 4.50m, "expense", date);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        var before = DateTime.UtcNow;

        var result = await CreateService().CreateAsync(userId, request);

        result.TransactionId.Should().Be(42);
        result.UserId.Should().Be(userId);
        result.Title.Should().Be("Coffee");
        result.Amount.Should().Be(4.50m);
        result.Category.Should().Be("expense");
        result.Date.Should().Be(date);
        result.Deleted.Should().BeFalse();
        result.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        result.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);

        _repoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t =>
                t.UserId == userId &&
                t.Title == "Coffee" &&
                t.Amount == 4.50m &&
                t.Category == "expense" &&
                t.Date == date &&
                !t.Deleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithNegativeAmount_ThrowsValidationException()
    {
        var request = new CreateTransactionRequest("Coffee", -1m, "expense", null);

        var act = () => CreateService().CreateAsync(7, request);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithZeroAmount_ThrowsValidationException()
    {
        var request = new CreateTransactionRequest("Coffee", 0m, "expense", null);

        var act = () => CreateService().CreateAsync(7, request);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithEmptyTitle_ThrowsValidationException(string title)
    {
        var request = new CreateTransactionRequest(title, 4.50m, "expense", null);

        var act = () => CreateService().CreateAsync(7, request);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("food")]
    [InlineData("Income")]
    [InlineData("EXPENSE")]
    public async Task Create_WithInvalidCategory_ThrowsValidationException(string category)
    {
        var request = new CreateTransactionRequest("Coffee", 4.50m, category, null);

        var act = () => CreateService().CreateAsync(7, request);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithNoDate_DefaultsToToday()
    {
        var request = new CreateTransactionRequest("Coffee", 4.50m, "expense", Date: null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await CreateService().CreateAsync(7, request);

        result.Date.Should().Be(today);
        _repoMock.Verify(r => r.CreateAsync(
            It.Is<Transaction>(t => t.Date == today),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedDto()
    {
        var userId = 7;
        var existing = new Transaction
        {
            TransactionId = 42,
            UserId = userId,
            Title = "Old",
            Amount = 1m,
            Category = "income",
            Date = new DateOnly(2026, 1, 1),
            Deleted = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        _repoMock.Setup(r => r.GetByIdAsync(existing.TransactionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var request = new UpdateTransactionRequest(existing.TransactionId, "New title", 9.99m, "expense", new DateOnly(2026, 5, 1));
        var before = DateTime.UtcNow;

        var result = await CreateService().UpdateAsync(userId, request);

        result.TransactionId.Should().Be(42);
        result.Title.Should().Be("New title");
        result.Amount.Should().Be(9.99m);
        result.Category.Should().Be("expense");
        result.Date.Should().Be(new DateOnly(2026, 5, 1));
        result.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<Transaction>(t =>
                t.TransactionId == 42 &&
                t.Title == "New title" &&
                t.Amount == 9.99m &&
                t.Category == "expense" &&
                t.Date == new DateOnly(2026, 5, 1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithTransactionBelongingToOtherUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(42, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        var request = new UpdateTransactionRequest(42, "Coffee", 4.50m, "expense", null);

        var act = () => CreateService().UpdateAsync(7, request);

        await act.Should().ThrowAsync<NotFoundException>();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithNegativeAmount_ThrowsValidationException()
    {
        var request = new UpdateTransactionRequest(42, "Coffee", -1m, "expense", null);

        var act = () => CreateService().UpdateAsync(7, request);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithInvalidCategory_ThrowsValidationException()
    {
        var request = new UpdateTransactionRequest(42, "Coffee", 4.50m, "food", null);

        var act = () => CreateService().UpdateAsync(7, request);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Transaction MakeTx(int id, int userId = 7, string category = "expense") => new()
    {
        TransactionId = id,
        UserId = userId,
        Title = $"Tx{id}",
        Amount = 1m,
        Category = category,
        Date = new DateOnly(2026, 5, 1),
        Deleted = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetPaged_ReturnsCorrectPageAndTotalCount()
    {
        var query = new TransactionQueryParams(Page: 2, PageSize: 3);
        IReadOnlyList<Transaction> items = new[] { MakeTx(4), MakeTx(5), MakeTx(6) };
        _repoMock.Setup(r => r.GetPagedAsync(7, 2, 3, "date", "desc", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 10));

        var result = await CreateService().GetPagedAsync(7, query);

        result.Items.Should().HaveCount(3);
        result.Items.Select(i => i.TransactionId).Should().Equal(4, 5, 6);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.TotalPages.Should().Be(4); // ceil(10/3)
    }

    [Fact]
    public async Task GetPaged_WithCategoryFilter_PassesFilterToRepository()
    {
        var query = new TransactionQueryParams(Category: "income");
        _repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Transaction>(), 0));

        await CreateService().GetPagedAsync(7, query);

        _repoMock.Verify(r => r.GetPagedAsync(
            7, query.Page, query.PageSize, query.SortBy, query.SortOrder,
            "income", null, null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_WithDateRange_PassesDateRangeToRepository()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 12, 31);
        var query = new TransactionQueryParams(DateFrom: from, DateTo: to);
        _repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Transaction>(), 0));

        await CreateService().GetPagedAsync(7, query);

        _repoMock.Verify(r => r.GetPagedAsync(
            7, query.Page, query.PageSize, query.SortBy, query.SortOrder,
            null, from, to,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSummary_ReturnsNetBalanceTotalIncomeTotalExpense()
    {
        _repoMock.Setup(r => r.GetSummaryAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetBalance: 250m, TotalIncome: 1000m, TotalExpense: 750m));

        var result = await CreateService().GetSummaryAsync(7);

        result.Should().Be(new TransactionSummaryDto(250m, 1000m, 750m));
    }

    [Fact]
    public async Task GetSummary_WithNoTransactions_ReturnsZeros()
    {
        _repoMock.Setup(r => r.GetSummaryAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NetBalance: 0m, TotalIncome: 0m, TotalExpense: 0m));

        var result = await CreateService().GetSummaryAsync(7);

        result.Should().Be(new TransactionSummaryDto(0m, 0m, 0m));
    }

    [Fact]
    public async Task SoftDelete_WithValidId_CallsRepository()
    {
        _repoMock.Setup(r => r.GetByIdAsync(42, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTx(42));

        await CreateService().SoftDeleteAsync(7, 42);

        _repoMock.Verify(r => r.SoftDeleteAsync(42, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDelete_WithTransactionBelongingToOtherUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(42, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var act = () => CreateService().SoftDeleteAsync(7, 42);

        await act.Should().ThrowAsync<NotFoundException>();
        _repoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
