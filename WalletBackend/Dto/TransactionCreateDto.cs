using WalletBackend.Models;

namespace WalletBackend.Dto;

public record TransactionCreateDto (
    DateTime Date,
    decimal Amount,
    TransactionDirection Direction,
    int AccountId,
    int CategoryId
);