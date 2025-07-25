namespace WalletBackend.Endpoints;

public static class TransactionEndpoints
{
    public static RouteGroupBuilder MapTransactionEndpoints(this WebApplication app)
    {
        var tx = app.MapGroup("/transactions").WithTags("Transactions").RequireAuthorization().RequireRateLimiting("fixed");

        tx.MapGet("/", async (
            WalletDbContext db,
            IMapper mapper,
            string? month,
            int? accountId,
            int? categoryId,
            int page = 1,
            int pageSize = 20,
            string? sortBy = "date",
            string? sortDir = "desc"
        ) =>
    {
        // base query includes navs so AutoMapper can read names
        IQueryable<Transaction> query = db.Transactions
                        .Include(t => t.Account)
                        .Include(t => t.Category)
                        .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(month) &&
            DateTime.TryParseExact(month, "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var monthStart))
        {
            var monthEnd = monthStart.AddMonths(1);
            query = query.Where(t => t.Date >= monthStart && t.Date < monthEnd);
        }

        //if(accountId.HasValue && accountId.Value > 0)
        if (accountId is int accId && accId > 0)
        {
            query = query.Where(t => t.AccountId == accId);
        }
        if (categoryId is int ctgId && ctgId > 0)
        {
            query = query.Where(t => t.CategoryId == ctgId);
        }

        // total before paging
        var total = await query.CountAsync();

        // sorting
        query = (sortBy?.ToLower(), sortDir?.ToLower()) switch
        {
            ("amount", "asc") => query.OrderBy(t => (double)t.Amount), // casting for sqlite
            ("amount", _) => query.OrderByDescending(t => (double)t.Amount),

            ("direction", "asc") => query.OrderBy(t => t.Direction),
            ("direction", _) => query.OrderByDescending(t => t.Direction),

            // default: date
            (_, "asc") => query.OrderBy(t => t.Date),
            _ => query.OrderByDescending(t => t.Date)
        };

        // paging
        var skip = page <= 0 ? 0 : (page - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync();

        var dtoList = mapper.Map<IEnumerable<TransactionReadDto>>(items);

        return Results.Ok(new PagedResult<TransactionReadDto>(
            Items: dtoList.ToList(),
            TotalCount: total,
            Page: page,
            PageSize: pageSize
        ));
    })
    .WithSummary("Get all transactions or only those within the given month (yyyy-MM)")
    .WithName("GetTransactions");

        tx.MapGet("/{id:int}", async (WalletDbContext db, IMapper mapper, int id) =>
        {
            var entity = await db.Transactions
                                .Include(t => t.Account)
                                .Include(t => t.Category)
                                .FirstOrDefaultAsync(t => t.Id == id);
            return entity is null ? Results.NotFound()
                             : Results.Ok(mapper.Map<TransactionReadDto>(entity));
        });
        tx.MapPost("/", async (WalletDbContext db, IMapper mapper, ILoggerFactory lf, TransactionCreateDto dto) =>
        {
            var log = lf.CreateLogger("Transactions");
            log.LogInformation("Creating transaction {@Dto}", dto);

            if (!await db.Accounts.AnyAsync(a => a.Id == dto.AccountId)) return Results.BadRequest("Account not found");
            if (!await db.Categories.AnyAsync(c => c.Id == dto.CategoryId)) return Results.BadRequest("Category not found");

            var entity = mapper.Map<Transaction>(dto);
            await db.Transactions.AddAsync(entity);
            await db.SaveChangesAsync();

            // reload with navs to return a full ReadDto
            entity = await db.Transactions
                                .Include(t => t.Account)
                                .Include(t => t.Category)
                                .FirstAsync(t => t.Id == entity.Id);
            return Results.Created($"/transactions/{entity.Id}", mapper.Map<TransactionReadDto>(entity));
        })
        .AddEndpointFilter<PositiveAmountFilter>();

        tx.MapPut("/{id:int}", async (WalletDbContext db, IMapper mapper, TransactionUpdateDto dto, int id) =>
        {
            var entity = await db.Transactions.FindAsync(id);
            if (entity is null) return Results.NotFound();

            mapper.Map(dto, entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .AddEndpointFilter<PositiveAmountFilter>();

        tx.MapDelete("/{id:int}", async (WalletDbContext db, int id) =>
        {
            var transaction = await db.Transactions.FindAsync(id);
            if (transaction is null) return Results.NotFound();
            db.Transactions.Remove(transaction);
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        return tx;
    }
}