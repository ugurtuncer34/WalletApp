using System.ComponentModel.DataAnnotations;

namespace WalletBackend.Dto;

public record TransactionCreateDto(
    [property: Required]
    [property: DataType(DataType.Date, ErrorMessage = "Date is required.")]
    DateTime Date,

    [property: Required]
    [property: Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    decimal Amount,

    [property: Required]
    [property: EnumDataType(typeof(TransactionDirection), ErrorMessage = "Direction must be a valid TransactionDirection.")]
    TransactionDirection Direction,

    [property: Required]
    [property: Range(1, int.MaxValue, ErrorMessage = "AccountId is required.")]
    int AccountId,

    [property: Required]
    [property: Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    int CategoryId
);