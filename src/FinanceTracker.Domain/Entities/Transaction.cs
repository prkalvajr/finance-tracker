namespace FinanceTracker.Domain.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public required string Title { get; set; }
    public decimal Amount { get; set; }
    public required string Category { get; set; }
    public DateOnly Date { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
