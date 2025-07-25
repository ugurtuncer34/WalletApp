namespace WalletBackend.Endpoints;

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this WebApplication app)
    {
        var ax = app.MapGroup("/accounts").WithTags("Accounts").RequireAuthorization();

        ax.MapGet("/", async (WalletDbContext db, IMapper mapper) =>
    {
        var list = await db.Accounts.ToListAsync();
        return Results.Ok(mapper.Map<IEnumerable<AccountReadDto>>(list));
    });
        ax.MapGet("/{id:int}", async (WalletDbContext db, IMapper mapper, int id) =>
        {
            var entity = await db.Accounts.FindAsync(id);
            return entity is null
                ? Results.NotFound()
                : Results.Ok(mapper.Map<AccountReadDto>(entity));
        });
        ax.MapGet("/{id:int}/balance", async (WalletDbContext db, int id) =>
        {
            var accountExists = await db.Accounts.AnyAsync(a => a.Id == id);
            if (!accountExists) return Results.NotFound();

            var balance = await db.Transactions
                                    .Where(t => t.AccountId == id)
                                    .SumAsync(t => t.Direction == TransactionDirection.Income
                                                    ? t.Amount
                                                    : -t.Amount);
            return Results.Ok(new BalanceDto(id, balance));
        });
        ax.MapPost("/", async (WalletDbContext db, IMapper mapper, AccountCreateDto dto) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest("Account must be named");

            var entity = mapper.Map<Account>(dto);
            db.Accounts.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/accounts/{entity.Id}",
                mapper.Map<AccountReadDto>(entity));
        });
        ax.MapPut("/{id:int}", async (WalletDbContext db, IMapper mapper, int id, AccountUpdateDto dto) =>
        {
            var entity = await db.Accounts.FindAsync(id);
            if (entity is null) return Results.NotFound();

            mapper.Map(dto, entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        ax.MapDelete("/{id:int}", async (WalletDbContext db, int id) =>
        {
            var account = await db.Accounts.FindAsync(id);
            if (account is null) return Results.NotFound();
            db.Accounts.Remove(account);
            await db.SaveChangesAsync();
            return Results.Ok();
        });
        return ax;
    }
}