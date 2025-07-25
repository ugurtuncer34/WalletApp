namespace WalletBackend.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this WebApplication app)
    {
        var cx = app.MapGroup("/categories").WithTags("Categories").RequireAuthorization();

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

        return cx;
    }
}