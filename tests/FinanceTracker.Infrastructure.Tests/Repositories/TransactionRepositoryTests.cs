using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Repositories;
using FluentAssertions;

namespace FinanceTracker.Infrastructure.Tests.Repositories;

[Collection("Database")]
public class TransactionRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly TransactionRepository _repository;
    private readonly UserRepository _userRepository;

    public TransactionRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new TransactionRepository(_fixture.ConnectionFactory);
        _userRepository = new UserRepository(_fixture.ConnectionFactory);
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<int> CreateUserAsync(string email = "u@example.com")
    {
        var user = new User { Name = "User", Email = email, PasswordHash = "h" };
        return await _userRepository.CreateAsync(user);
    }

    private static Transaction NewTx(int userId, string title = "Lunch", decimal amount = 12.50m,
        string category = "expense", DateOnly? date = null) => new()
    {
        UserId = userId,
        Title = title,
        Amount = amount,
        Category = category,
        Date = date ?? new DateOnly(2025, 1, 15),
    };

    [Fact]
    public async Task CreateAsync_InsertsTransactionAndReturnsId()
    {
        var userId = await CreateUserAsync();
        var tx = NewTx(userId);

        var id = await _repository.CreateAsync(tx);

        id.Should().BeGreaterThan(0);
        tx.TransactionId.Should().Be(id);
        tx.CreatedAt.Should().NotBe(default);
        tx.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsTransaction()
    {
        var userId = await CreateUserAsync();
        var tx = NewTx(userId, title: "Rent", amount: 1000m, category: "expense");
        await _repository.CreateAsync(tx);

        var found = await _repository.GetByIdAsync(tx.TransactionId, userId);

        found.Should().NotBeNull();
        found!.Title.Should().Be("Rent");
        found.Amount.Should().Be(1000m);
        found.Category.Should().Be("expense");
        found.UserId.Should().Be(userId);
        found.Deleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var userId = await CreateUserAsync();

        var found = await _repository.GetByIdAsync(999_999, userId);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        var userId = await CreateUserAsync();
        for (var i = 1; i <= 25; i++)
        {
            await _repository.CreateAsync(NewTx(userId, title: $"T{i}", date: new DateOnly(2025, 1, i)));
        }

        var (items, total) = await _repository.GetPagedAsync(userId, page: 2, pageSize: 10,
            sortBy: "date", sortOrder: "desc",
            category: null, dateFrom: null, dateTo: null);

        total.Should().Be(25);
        items.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesSoftDeletedRecords()
    {
        var userId = await CreateUserAsync();
        var keep = NewTx(userId, title: "Keep");
        var drop = NewTx(userId, title: "Drop");
        await _repository.CreateAsync(keep);
        await _repository.CreateAsync(drop);
        await _repository.SoftDeleteAsync(drop.TransactionId, userId);

        var (items, total) = await _repository.GetPagedAsync(userId, 1, 50, "date", "desc", null, null, null);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Title.Should().Be("Keep");
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByCategory()
    {
        var userId = await CreateUserAsync();
        await _repository.CreateAsync(NewTx(userId, title: "Salary", category: "income"));
        await _repository.CreateAsync(NewTx(userId, title: "Lunch", category: "expense"));
        await _repository.CreateAsync(NewTx(userId, title: "Bonus", category: "income"));

        var (items, total) = await _repository.GetPagedAsync(userId, 1, 50, "date", "desc",
            category: "income", dateFrom: null, dateTo: null);

        total.Should().Be(2);
        items.Should().OnlyContain(t => t.Category == "income");
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByDateRange()
    {
        var userId = await CreateUserAsync();
        await _repository.CreateAsync(NewTx(userId, title: "Old", date: new DateOnly(2024, 12, 1)));
        await _repository.CreateAsync(NewTx(userId, title: "Mid", date: new DateOnly(2025, 1, 15)));
        await _repository.CreateAsync(NewTx(userId, title: "New", date: new DateOnly(2025, 3, 1)));

        var (items, total) = await _repository.GetPagedAsync(userId, 1, 50, "date", "desc", null,
            dateFrom: new DateOnly(2025, 1, 1),
            dateTo: new DateOnly(2025, 2, 1));

        total.Should().Be(1);
        items.Single().Title.Should().Be("Mid");
    }

    [Fact]
    public async Task GetPagedAsync_SortsByDateDescendingByDefault()
    {
        var userId = await CreateUserAsync();
        await _repository.CreateAsync(NewTx(userId, title: "Old", date: new DateOnly(2025, 1, 1)));
        await _repository.CreateAsync(NewTx(userId, title: "Mid", date: new DateOnly(2025, 1, 5)));
        await _repository.CreateAsync(NewTx(userId, title: "New", date: new DateOnly(2025, 1, 10)));

        var (items, _) = await _repository.GetPagedAsync(userId, 1, 50, "date", "desc", null, null, null);

        items.Select(t => t.Title).Should().Equal("New", "Mid", "Old");
    }

    [Fact]
    public async Task GetPagedAsync_OnlyReturnsTransactionsForRequestedUser()
    {
        var userA = await CreateUserAsync("a@example.com");
        var userB = await CreateUserAsync("b@example.com");
        await _repository.CreateAsync(NewTx(userA, title: "A1"));
        await _repository.CreateAsync(NewTx(userA, title: "A2"));
        await _repository.CreateAsync(NewTx(userB, title: "B1"));

        var (items, total) = await _repository.GetPagedAsync(userA, 1, 50, "date", "desc", null, null, null);

        total.Should().Be(2);
        items.Should().OnlyContain(t => t.UserId == userA);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangedFields()
    {
        var userId = await CreateUserAsync();
        var tx = NewTx(userId, title: "Old", amount: 10m, category: "expense");
        await _repository.CreateAsync(tx);

        tx.Title = "Updated";
        tx.Amount = 99.99m;
        tx.Category = "income";
        tx.Date = new DateOnly(2025, 6, 1);
        await _repository.UpdateAsync(tx);

        var reloaded = await _repository.GetByIdAsync(tx.TransactionId, userId);
        reloaded!.Title.Should().Be("Updated");
        reloaded.Amount.Should().Be(99.99m);
        reloaded.Category.Should().Be("income");
        reloaded.Date.Should().Be(new DateOnly(2025, 6, 1));
    }

    [Fact]
    public async Task SoftDeleteAsync_SetsDeletedFlag()
    {
        var userId = await CreateUserAsync();
        var tx = NewTx(userId);
        await _repository.CreateAsync(tx);

        await _repository.SoftDeleteAsync(tx.TransactionId, userId);

        var found = await _repository.GetByIdAsync(tx.TransactionId, userId);
        found.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_DoesNotPhysicallyRemoveRow()
    {
        var userId = await CreateUserAsync();
        var tx = NewTx(userId);
        await _repository.CreateAsync(tx);

        await _repository.SoftDeleteAsync(tx.TransactionId, userId);

        await using var connection = await _fixture.ConnectionFactory.CreateOpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT deleted FROM transactions WHERE transaction_id = @id;";
        var p = cmd.CreateParameter();
        p.ParameterName = "id";
        p.Value = tx.TransactionId;
        cmd.Parameters.Add(p);
        var deleted = await cmd.ExecuteScalarAsync();
        deleted.Should().Be(true);
    }

    [Fact]
    public async Task GetSummaryAsync_CalculatesCorrectTotals()
    {
        var userId = await CreateUserAsync();
        await _repository.CreateAsync(NewTx(userId, amount: 1000m, category: "income"));
        await _repository.CreateAsync(NewTx(userId, amount: 250.50m, category: "expense"));
        await _repository.CreateAsync(NewTx(userId, amount: 100m, category: "expense"));
        var deleted = NewTx(userId, amount: 9999m, category: "income");
        await _repository.CreateAsync(deleted);
        await _repository.SoftDeleteAsync(deleted.TransactionId, userId);

        var (net, income, expense) = await _repository.GetSummaryAsync(userId);

        income.Should().Be(1000m);
        expense.Should().Be(350.50m);
        net.Should().Be(649.50m);
    }
}
