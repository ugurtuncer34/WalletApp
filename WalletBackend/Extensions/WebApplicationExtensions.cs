namespace WalletBackend.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication SeedData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        if (db.Database.IsRelational())
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();

        if (!db.Accounts.Any())
        {
            db.Accounts.Add(new Account { Name = "Cash", Currency = Currency.TRY });
            db.SaveChanges();
        }

        if (!db.Categories.Any())
        {
            db.Categories.Add(new Category { Name = "Fun" });
            db.SaveChanges();
        }

        if (!db.Users.Any())
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!");
            db.Users.Add(new User
            {
                Email = "test@wallet.dev",
                PasswordHash = hash,
                Role = "User"
            });
            db.SaveChanges();
        }

        return app;
    }
}