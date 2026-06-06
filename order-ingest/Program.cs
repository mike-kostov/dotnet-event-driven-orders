using System.Text.Json;
using Confluent.Kafka;
using OrderIngest.Contracts;

var builder = WebApplication.CreateBuilder(args);

// --- Kafka producer (lesson 3) ---------------------------------------------
// One producer per process, configured from KAFKA_BOOTSTRAP (compose sets it to
// "kafka:9092"). Registered as a singleton so handlers can inject it.
var bootstrap = builder.Configuration["KAFKA_BOOTSTRAP"] ?? "kafka:9092";
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
    new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = bootstrap }).Build());

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok("healthy"));

// POST /orders — accept a PLACE and PRODUCE it to the "orders" topic.
app.MapPost("/orders", async (PlaceOrderRequest request, IProducer<string, string> producer) =>
{
    if (string.IsNullOrWhiteSpace(request.Customer) ||
        request.Items is null || request.Items.Count == 0 ||
        request.Items.Any(i => i.Quantity <= 0 || i.UnitPriceCents <= 0))
    {
        return Results.BadRequest(new { error = "customer and at least one valid item are required" });
    }

    var orderId = Guid.NewGuid().ToString();

    // Build the command, serialize to JSON, and produce keyed by orderId so all
    // commands for one order share a partition (and stay in order).
    var cmd = new OrderCommand(
        EventId: Guid.NewGuid().ToString(), OrderId: orderId, Type: "PLACE",
        IssuedAt: DateTimeOffset.UtcNow, Customer: request.Customer,
        Items: request.Items,
        TotalCents: request.Items.Sum(i => i.Quantity * i.UnitPriceCents));
    var json = JsonSerializer.Serialize(cmd);
    await producer.ProduceAsync("orders", new Message<string, string> { Key = orderId, Value = json });

    app.Logger.LogInformation("Accepted order {OrderId}", orderId);
    return Results.Accepted($"/orders/{orderId}", new PlaceOrderResponse(orderId));
});

// Transition commands. Stubs for now (no logic) — fleshed out in lesson 6.
app.MapPost("/orders/{id}/confirm",  (string id) => Results.Accepted());
app.MapPost("/orders/{id}/prepare",  (string id) => Results.Accepted());
app.MapPost("/orders/{id}/dispatch", (string id) => Results.Accepted());
app.MapPost("/orders/{id}/deliver",  (string id) => Results.Accepted());
app.MapPost("/orders/{id}/cancel",   (string id) => Results.Accepted());

app.Run();
