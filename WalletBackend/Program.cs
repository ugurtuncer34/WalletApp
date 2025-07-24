global using Microsoft.EntityFrameworkCore;
global using WalletBackend.Data;
global using WalletBackend.Models;
global using WalletBackend.Dto;
global using WalletBackend.Mapping;
using Microsoft.OpenApi.Models;
using AutoMapper;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using WalletBackend.Filters;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting up");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();   // replace default logging

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
    builder.Services.AddScoped<PositiveAmountFilter>();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<WalletDbContext>(
            name: "Database",
            tags: ["ready"]);
    builder.Services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(600); // poll every 10m
            options.AddHealthCheckEndpoint("wallet-api", "/health");  // dash reads own endpoint
        }).AddInMemoryStorage();
    builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429; // Too Many Requests

            options.AddFixedWindowLimiter("fixed", limiter =>
            {
                limiter.PermitLimit = 100; // max 100 requests
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0; // reject immediately
                limiter.AutoReplenishment = true;
            });
        });

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
    app.UseSerilogRequestLogging(); // logs HTTP method, path, status, timing
    app.UseCors("dev");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseExceptionHandler(a =>
    {
        a.Run(async ctx =>
        {
            var ex = ctx.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            Log.Error(ex, "Unhandled exception");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsync("Unexpected error");
        });
    });

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

    var tx = app.MapGroup("/transactions").WithTags("Transactions").RequireAuthorization().RequireRateLimiting("fixed");
    var ax = app.MapGroup("/accounts").WithTags("Accounts").RequireAuthorization();
    var cx = app.MapGroup("/categories").WithTags("Categories").RequireAuthorization();

    // Transaction Endpoints
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

    app.MapGet("/alive", () => Results.Ok("I’m alive")).AllowAnonymous();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    app.MapHealthChecksUI(options =>
    {
        options.UIPath = "/health-ui";
        options.ApiPath = "/health-json";
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}