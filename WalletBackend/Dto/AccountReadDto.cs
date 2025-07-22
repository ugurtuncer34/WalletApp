namespace WalletBackend.Dto;

public record AccountReadDto
(
    int Id,
    string Name,
    string Currency
);