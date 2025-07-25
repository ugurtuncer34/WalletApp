global using Microsoft.EntityFrameworkCore;
global using WalletBackend.Data;
global using WalletBackend.Models;
global using WalletBackend.Dto;
global using WalletBackend.Mapping;
global using Microsoft.OpenApi.Models;
global using AutoMapper;
global using System.Globalization;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.IdentityModel.Tokens;
global using System.Text;
global using System.Security.Claims;
global using System.IdentityModel.Tokens.Jwt;
global using WalletBackend.Filters;
global using WalletBackend.Extensions;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using WalletBackend.Endpoints;

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
    builder.Services.AddHealthChecksWithUI();
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

    var app = builder.Build().SeedData();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "WalletBackend API V1");
        });
    }

    app.UseSerilogRequestLogging() // logs HTTP method, path, status, timing
       .UseCors("dev")
       .UseRateLimiter()
       .UseAuthentication()
       .UseAuthorization()
       .UseExceptionHandler(a =>
    {
        a.Run(async ctx =>
        {
            var ex = ctx.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            Log.Error(ex, "Unhandled exception");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsync("Unexpected error");
        });
    });

    app.MapAuthEndpoints();
    app.MapTransactionEndpoints();
    app.MapAccountEndpoints();
    app.MapCategoryEndpoints();
    app.MapReportEndpoints();
    app.MapHealthEndpoints();

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