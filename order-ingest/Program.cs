using System.Text.Json;
using Confluent.Kafka;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderIngest.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Observability (lesson 10): structured JSON logs + OpenTelemetry traces (console).
builder.Logging.AddJsonConsole();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("order-ingest"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddConsoleExporter());

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

// POST /admin/replay (lesson 9) — operator-triggered recovery. Drains the
// dead-letter topic and republishes to 'orders' so the processor reprocesses
// them through the normal path (idempotency makes this safe). Bounded by a
// high-watermark snapshot so it doesn't chase messages re-dead-lettered now.
app.MapPost("/admin/replay", (IProducer<string, string> producer) =>
{
    using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
    {
        BootstrapServers = bootstrap,
        GroupId = "order-ingest-replay",
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest,
    }).Build();

    var tp = new TopicPartition("orders.DLT", 0); // DLT has 1 partition
    consumer.Assign(new TopicPartitionOffset(tp, Offset.Beginning));
    var end = consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(5)).High;

    var replayed = 0;
    while (replayed < end.Value)
    {
        var cr = consumer.Consume(TimeSpan.FromSeconds(2));
        if (cr is null) break;
        producer.Produce("orders", new Message<string, string> { Key = cr.Message.Key, Value = cr.Message.Value });
        replayed++;
    }
    producer.Flush(TimeSpan.FromSeconds(5));
    return Results.Ok(new { replayed });
});

app.Run();
