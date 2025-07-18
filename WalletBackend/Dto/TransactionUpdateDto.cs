namespace WalletBackend.Dto;

public record TransactionUpdateDto
(
    DateTime Date,
    decimal Amount,
    TransactionDirection Direction,
    int AccountId,
    int CategoryId
);