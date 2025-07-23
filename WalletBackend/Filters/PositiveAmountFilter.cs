namespace WalletBackend.Filters;

public class PositiveAmountFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        // Try to find a DTO that has Amount
        var dto = context.Arguments.FirstOrDefault(a =>
            a is TransactionCreateDto || a is TransactionUpdateDto);

        if (dto is TransactionCreateDto c && c.Amount <= 0)
            return Results.BadRequest("Amount must be positive");

        if (dto is TransactionUpdateDto u && u.Amount <= 0)
            return Results.BadRequest("Amount must be positive");

        // Everything ok → continue pipeline
        return await next(context);
    }
}