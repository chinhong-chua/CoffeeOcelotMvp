using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CatalogDb>(o => o.UseSqlite("Data Source=catalog.db"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "catalog" }));

app.MapGet("/catalog/items", async (CatalogDb db) =>
    await db.Items.AsNoTracking().ToListAsync());

app.MapPost("/catalog/items", async (CatalogDb db, CatalogItem item) =>
{
    db.Items.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/catalog/items/{item.Id}", item);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDb>();
    db.Database.EnsureCreated();
    if (!db.Items.Any())
    {
        db.Items.AddRange(
            new CatalogItem { Name = "Espresso", Price = 3.0m },
            new CatalogItem { Name = "Latte", Price = 4.5m }
        );
        db.SaveChanges();
    }
}

app.Run();

record CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
}

class CatalogDb : DbContext
{
    public CatalogDb(DbContextOptions<CatalogDb> options) : base(options) { }
    public DbSet<CatalogItem> Items => Set<CatalogItem>();
}
