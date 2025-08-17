using Confluent.Kafka;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

var broker = Environment.GetEnvironmentVariable("KAFKA_BROKER") ?? "localhost:9092";

// Keep last 20 events
var events = new ConcurrentQueue<string>();

// Kafka consumer in background
_ = Task.Run(async () =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = broker,
        GroupId = "notification-service",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };

    // short backoff while broker warms up
    for (var i = 0; i < 10; i++)
    {
        try
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("orders");
            Console.WriteLine($"📩 NotificationService listening to 'orders' on {broker}...");
            while (true)
            {
                var cr = consumer.Consume(); // blocks until message
                var msg = $"OrderEvent: {cr.Message.Value}";
                Console.WriteLine($"[Kafka] {msg}");
                events.Enqueue(msg);
                while (events.Count > 20 && events.TryDequeue(out _)) { }
            }
        }
        catch (KafkaException ex)
        {
            Console.WriteLine($"Kafka not ready ({ex.Error.Reason}). Retry in 3s...");
            await Task.Delay(3000);
        }
    }
});

app.UseCors("AllowReact");

// API endpoint for React
app.MapGet("/events", () => events.ToArray());

app.MapGet("/", () => "Notification Service running...");

app.Run("http://0.0.0.0:7301");
