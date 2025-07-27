using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WalletBackend.Data;
using WalletBackend.Models;

namespace WalletBackend.Tests;

public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // --- switch to in-memory provider as before ---
            services.RemoveAll<DbContextOptions<WalletDbContext>>();
            services.AddDbContext<WalletDbContext>(o => o.UseInMemoryDatabase("WalletTestsDb"));

            // --- add fake auth ---
            services.AddAuthentication("Fake")
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Fake", _ => { });

            // make it the default
            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = "Fake";
                o.DefaultChallengeScheme = "Fake";
            });

            // seed minimal data …
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
            db.Database.EnsureCreated();
            if (!db.Accounts.Any())
            {
                db.Accounts.Add(new Account { Name = "Cash", Currency = Currency.TRY });
                db.Categories.Add(new Category { Name = "Misc" });
                db.SaveChanges();
            }
        });
    }
}