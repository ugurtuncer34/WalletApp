namespace WalletBackend.Dto;

public record TransactionReadDto (
    int Id,
    DateTime Date,
    decimal Amount,
    string Direction,
    int AccountId,
    string AccountName,
    int CategoryId,
    string CategoryName
);