namespace WalletBackend.Models;

public enum TransactionDirection
{
    Income = 1,
    Expense = 2
}

public class Transaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public TransactionDirection Direction { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!; // Navigation FK
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!; // Navigation FK
}
