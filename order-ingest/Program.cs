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

// Transition commands (lesson 6). Each produces an OrderCommand of the right
// type, keyed by order_id, and returns 202. order-ingest does NOT check legality
// (it has no DB) — order-processor validates against persisted state (ADR-0011).
async Task<IResult> Transition(IProducer<string, string> producer, string orderId, string type)
{
    var cmd = new OrderCommand(
        EventId: Guid.NewGuid().ToString(), OrderId: orderId, Type: type,
        IssuedAt: DateTimeOffset.UtcNow, Customer: null, Items: null, TotalCents: null);
    await producer.ProduceAsync("orders",
        new Message<string, string> { Key = orderId, Value = JsonSerializer.Serialize(cmd) });
    return Results.Accepted($"/orders/{orderId}");
}

app.MapPost("/orders/{id}/confirm",  (string id, IProducer<string, string> p) => Transition(p, id, "CONFIRM"));
app.MapPost("/orders/{id}/prepare",  (string id, IProducer<string, string> p) => Transition(p, id, "PREPARE"));
app.MapPost("/orders/{id}/dispatch", (string id, IProducer<string, string> p) => Transition(p, id, "DISPATCH"));
app.MapPost("/orders/{id}/deliver",  (string id, IProducer<string, string> p) => Transition(p, id, "DELIVER"));
app.MapPost("/orders/{id}/cancel",   (string id, IProducer<string, string> p) => Transition(p, id, "CANCEL"));

app.Run();
