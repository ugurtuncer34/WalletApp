global using Microsoft.EntityFrameworkCore;
global using WalletBackend.Data;
global using WalletBackend.Models;
global using WalletBackend.Dto;
// global using WalletBackend.Helpers;
global using WalletBackend.Mapping;
using Microsoft.OpenApi.Models;
using AutoMapper;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);

var connectionString = builder.Configuration.GetConnectionString("Wallet") ?? "Data Source=Wallet.db";

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddDbContext<WalletDbContext>(options => options.UseInMemoryDatabase("WalletDb"));
//builder.Services.AddDbContext<WalletDbContext>(opt => opt.UseSqlite(connectionString));
builder.Services.AddSqlite<WalletDbContext>(connectionString);
builder.Services.AddCors(opt => opt.AddPolicy("dev",
    p => p.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WalletBackend", Description = "Personal Wallet APIs", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Type **Bearer {your JWT}** into the text box below.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {jwtScheme, Array.Empty<string>()}
    });
});
builder.Services.AddAutoMapper(typeof(TransactionProfile));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(); // policies later if needed

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
    if (!db.Users.Any())
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!");
        db.Users.Add(new User { Email = "test@wallet.dev", PasswordHash = hash, Role = "User" });
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
app.UseAuthentication();
app.UseAuthorization();

//////////// AUTH /////////////
var auth = app.MapGroup("/auth").AllowAnonymous().WithTags("Auth");

auth.MapPost("/register", async (WalletDbContext db, RegisterDto dto) =>
{
    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
        return Results.BadRequest("Email already exists");

    var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    var user = new User { Email = dto.Email, PasswordHash = hash };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new { user.Id, user.Email });
});

auth.MapPost("/login", async (WalletDbContext db, LoginDto dto) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        return Results.Unauthorized();

    // Build claims
    var claims = new[]{
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var key = new SymmetricSecurityKey(keyBytes);
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!));

    var token = new JwtSecurityToken(
        issuer: jwt["Issuer"],
        audience: jwt["Audience"],
        claims: claims,
        expires: expires,
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new AuthResponseDto(tokenString, expires));
});
//////////// AUTH /////////////

app.MapGet("/", () => "go /swagger");

var tx = app.MapGroup("/transactions").WithTags("Transactions").RequireAuthorization();
var ax = app.MapGroup("/accounts").WithTags("Accounts").RequireAuthorization();
var cx = app.MapGroup("/categories").WithTags("Categories").RequireAuthorization();

// Transaction Endpoints
tx.MapGet("/", async (WalletDbContext db, IMapper mapper, string? month, int? accountId, int? categoryId) =>
{
    // base query includes navs so AutoMapper can read names
    IQueryable<Transaction> query = db.Transactions
                    .Include(t => t.Account)
                    .Include(t => t.Category);

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

    query = query.OrderByDescending(t => t.Date);

    var list = await query.ToListAsync();
    return Results.Ok(mapper.Map<IEnumerable<TransactionReadDto>>(list));
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

app.UseCors("dev");
app.Run();
