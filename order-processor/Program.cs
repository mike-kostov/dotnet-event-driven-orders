using OrderProcessor;

var builder = WebApplication.CreateBuilder(args);

// Persistence (lesson 5) + the Kafka consumer (lesson 4).
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddHostedService<ConsumerService>();

var app = builder.Build();

// Health endpoints (used to health-gate this service in later lessons).
app.MapGet("/healthz", () => Results.Ok("healthy")); // liveness: process is up
app.MapGet("/readyz", () => Results.Ok("ready"));    // readiness: deepened in the observability lesson

app.Run();
