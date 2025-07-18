using WalletBackend.Dto;
using WalletBackend.Models;

namespace WalletBackend.Helpers;

public static class TransactionMapper
{
    public static TransactionReadDto ToReadDto(this Transaction t)
        => new(
            t.Id,
            t.Date,
            t.Amount,
            t.Direction.ToString(),
            t.AccountId,
            t.Account.Name,
            t.CategoryId,
            t.Category.Name);

    public static Transaction ToEntity(this TransactionCreateDto dto)
        => new()
        {
            Date = dto.Date,
            Amount = dto.Amount,
            Direction = dto.Direction,
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId
        };
}