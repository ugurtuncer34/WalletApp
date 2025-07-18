global using Microsoft.EntityFrameworkCore;
global using WalletBackend.Data;
global using WalletBackend.Models;
global using WalletBackend.Dto;
// global using WalletBackend.Helpers;
global using WalletBackend.Mapping;
using Microsoft.OpenApi.Models;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Wallet") ?? "Data Source=Wallet.db";
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddDbContext<WalletDbContext>(options => options.UseInMemoryDatabase("WalletDb"));
builder.Services.AddSqlite<WalletDbContext>(connectionString);
//builder.Services.AddDbContext<WalletDbContext>(opt => opt.UseSqlite(connectionString));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WalletBackend", Description = "Personal Wallet APIs", Version = "v1" });
});
builder.Services.AddAutoMapper(typeof(TransactionProfile));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    if (!db.Accounts.Any())
    {
        var cash = new Account { Name = "Cash", Currency = Currency.TRY };
        db.Accounts.Add(cash);
        db.SaveChanges();
    }
    if (!db.Categories.Any())
    {
        var fun = new Category { Name = "Fun" };
        db.Categories.Add(fun);
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WalletBackend API V1");
    });
}

app.MapGet("/", () => "Hello World!");

var tx = app.MapGroup("/transactions").WithTags("Transactions");
var ax = app.MapGroup("/accounts").WithTags("Accounts");
var cx = app.MapGroup("/categories").WithTags("Categories");

// Transaction Endpoints
tx.MapGet("/", async (WalletDbContext db, IMapper mapper) =>
        {
            var list = await db.Transactions
                            .Include(t => t.Account)
                            .Include(t => t.Category)
                            .ToListAsync();
            return Results.Ok(mapper.Map<IEnumerable<TransactionReadDto>>(list));
        });
tx.MapGet("/{id:int}", async (WalletDbContext db, IMapper mapper, int id) =>
{
    var entity = await db.Transactions
                        .Include(t => t.Account)
                        .Include(t => t.Category)
                        .FirstOrDefaultAsync(t => t.Id == id);
    return entity is null ? Results.NotFound()
                     : Results.Ok(mapper.Map<TransactionReadDto>(entity));
});
tx.MapPost("/", async (WalletDbContext db, IMapper mapper, TransactionCreateDto dto) =>
{
    if (dto.Amount <= 0) return Results.BadRequest("Amount must be positive");
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
});
tx.MapPut("/{id:int}", async (WalletDbContext db, IMapper mapper, TransactionUpdateDto dto, int id) =>
{
    var entity = await db.Transactions.FindAsync(id);
    if (entity is null) return Results.NotFound();

    mapper.Map(dto, entity);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
tx.MapDelete("/{id:int}", async (WalletDbContext db, int id) =>
{
    var transaction = await db.Transactions.FindAsync(id);
    if (transaction is null) return Results.NotFound();
    db.Transactions.Remove(transaction);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Account Endpoints
ax.MapGet("/", async (WalletDbContext db) => await db.Accounts.ToListAsync());
ax.MapGet("/{id:int}", async (WalletDbContext db, int id) => await db.Accounts.FindAsync(id));
ax.MapPost("/", async (WalletDbContext db, Account account) =>
{
    if (string.IsNullOrWhiteSpace(account.Name)) return Results.BadRequest("Account must be named");
    await db.Accounts.AddAsync(account);
    await db.SaveChangesAsync();
    return Results.Created($"/accounts/{account.Id}", account);
});
ax.MapPut("/{id:int}", async (WalletDbContext db, Account updatedAccount, int id) =>
{
    var account = await db.Accounts.FindAsync(id);
    if (account is null) return Results.NotFound();
    account.Name = updatedAccount.Name;
    account.Currency = updatedAccount.Currency;
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

// Category Endpoints
cx.MapGet("/", async (WalletDbContext db) => await db.Categories.ToListAsync());
cx.MapGet("/{id:int}", async (WalletDbContext db, int id) => await db.Categories.FindAsync(id));
cx.MapPost("/", async (WalletDbContext db, Category category) =>
{
    if (string.IsNullOrWhiteSpace(category.Name)) return Results.BadRequest("Category must be named");
    await db.Categories.AddAsync(category);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{category.Id}", category);
});
cx.MapPut("/{id:int}", async (WalletDbContext db, Category updatedCategory, int id) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();
    category.Name = updatedCategory.Name;
    await db.SaveChangesAsync();
    return Results.NoContent();
});
cx.MapDelete("/{id:int}", async (WalletDbContext db, int id) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();
    db.Categories.Remove(category);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.Run();
