namespace WalletBackend.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/reports/category-expenses", async (WalletDbContext db, string? month) =>
        {
            if (!DateTime.TryParseExact(month ?? string.Empty, "yyyy-MM",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
            {
                return Results.BadRequest("Invalid month format (yyyy-MM)");
            }

            var end = start.AddMonths(1);
            var data = await db.Transactions
                .Where(t => t.Date >= start && t.Date < end)
                .GroupBy(t => t.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(t =>
                    t.Direction == TransactionDirection.Income ? t.Amount : -t.Amount)
                })
                .ToListAsync();

            return Results.Ok(data);
        })
        .WithName("GetCategoryExpenses")
        .WithTags("Reports");

    }
}