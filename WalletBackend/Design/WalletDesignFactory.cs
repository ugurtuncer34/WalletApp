namespace WalletBackend.Design;

using Microsoft.EntityFrameworkCore.Design;

public class WalletDesignFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<WalletDbContext>()
            .UseSqlite("Data Source=Wallet.db")
            .Options;

        return new WalletDbContext(opts);
    }
}