using Microsoft.EntityFrameworkCore;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<OrderDb>(o => o.UseSqlite("Data Source=orders.db"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// 🔑 Kafka broker from env, default to localhost for dev outside Docker
var broker = Environment.GetEnvironmentVariable("KAFKA_BROKER") ?? "localhost:9092";

// Singleton producer (reuse connection)
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var cfg = new ProducerConfig
    {
        BootstrapServers = broker,
        Acks = Acks.Leader,
        MessageTimeoutMs = 5000,
        SocketTimeoutMs = 5000,
        MetadataMaxAgeMs = 30000
    };
    return new ProducerBuilder<string, string>(cfg).Build();
});

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "orders", broker }));

app.MapGet("/orders", async (OrderDb db) =>
    await db.Orders.AsNoTracking().OrderByDescending(o => o.Id).ToListAsync());

app.MapPost("/orders", async (OrderDb db, IProducer<string, string> producer, CreateOrder req) =>
{
    var order = new Order { ItemName = req.ItemName, Quantity = req.Quantity, Total = req.Total, CreatedUtc = DateTime.UtcNow };
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    // Publish to Kafka, but don't fail the API if broker is unreachable
    try
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(order);
        var msg = new Message<string, string> { Key = order.Id.ToString(), Value = payload };
        var dr = await producer.ProduceAsync("orders", msg);
        app.Logger.LogInformation("Published OrderCreated to Kafka at {TopicPartitionOffset}", dr.TopicPartitionOffset);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Kafka publish failed. Order {OrderId} saved to DB; continuing.", order.Id);
        // Optionally add an outbox, retry, etc. later
    }

    return Results.Created($"/orders/{order.Id}", order);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDb>();
    db.Database.EnsureCreated();
}

app.Run();

record CreateOrder(string ItemName, int Quantity, decimal Total);

class Order
{
    public int Id { get; set; }
    public string ItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedUtc { get; set; }
}

class OrderDb : DbContext
{
    public OrderDb(DbContextOptions<OrderDb> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
}
