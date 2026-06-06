using OrderProcessor;

var builder = WebApplication.CreateBuilder(args);

// Run the Kafka consumer as a background service for the lifetime of the app.
builder.Services.AddHostedService<ConsumerService>();

var app = builder.Build();

// Health endpoints (used to health-gate this service in later lessons).
app.MapGet("/healthz", () => Results.Ok("healthy")); // liveness: process is up
app.MapGet("/readyz", () => Results.Ok("ready"));    // readiness: deepened in the observability lesson

app.Run();
