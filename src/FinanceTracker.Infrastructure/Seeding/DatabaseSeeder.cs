using FinanceTracker.Application.Services;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;

namespace FinanceTracker.Infrastructure.Seeding;

public class DatabaseSeeder
{
    public const string DemoEmail = "demo@example.com";
    public const string DemoPassword = "demo1234";
    public const string DemoName = "Demo User";

    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(
        IUserRepository userRepository,
        ITransactionRepository transactionRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _userRepository.GetByEmailAsync(DemoEmail, ct);
        if (existing is not null)
        {
            return;
        }

        var user = new User
        {
            Name = DemoName,
            Email = DemoEmail,
            PasswordHash = _passwordHasher.Hash(DemoPassword),
        };
        await _userRepository.CreateAsync(user, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var samples = new (string Title, decimal Amount, string Category, int DayOffset)[]
        {
            ("Salary",          5000.00m, "income",   -1),
            ("Freelance",       1200.00m, "income",   -3),
            ("Refund",            45.00m, "income",   -7),
            ("Rent",            1500.00m, "expense",  -2),
            ("Groceries",        220.50m, "expense",  -2),
            ("Electricity bill", 110.00m, "expense",  -5),
            ("Internet",          60.00m, "expense",  -5),
            ("Gym",               40.00m, "expense",  -8),
            ("Restaurant",        75.30m, "expense", -10),
            ("Coffee",             4.50m, "expense", -12),
        };

        foreach (var s in samples)
        {
            await _transactionRepository.CreateAsync(new Transaction
            {
                UserId = user.UserId,
                Title = s.Title,
                Amount = s.Amount,
                Category = s.Category,
                Date = today.AddDays(s.DayOffset),
            }, ct);
        }
    }
}
