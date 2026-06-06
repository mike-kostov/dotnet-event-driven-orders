using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderProcessor;

var builder = WebApplication.CreateBuilder(args);

// Observability (lesson 10): structured JSON logs + OpenTelemetry traces (console).
builder.Logging.AddJsonConsole();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("order-processor"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddConsoleExporter());

// Persistence (lesson 5) + the Kafka consumer (lesson 4) + dead-letter (lesson 9).
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<DeadLetter>();
builder.Services.AddHostedService<ConsumerService>();

var app = builder.Build();

// Health endpoints (used to health-gate this service in later lessons).
app.MapGet("/healthz", () => Results.Ok("healthy")); // liveness: process is up
app.MapGet("/readyz", () => Results.Ok("ready"));    // readiness: deepened in the observability lesson

app.Run();
