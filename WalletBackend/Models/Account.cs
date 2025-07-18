namespace WalletBackend.Models;

public enum Currency
{
    TRY = 1,
    EUR = 2,
    USD = 3
}

public class Account
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Currency Currency { get; set; }
}