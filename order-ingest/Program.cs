using OrderIngest.Contracts;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Liveness probe. Compose can health-gate on this.
app.MapGet("/healthz", () => Results.Ok("healthy"));

// POST /orders — accept a new order ("PLACE").
//
// order-ingest has NO database and does NOT decide whether a transition is
// legal (ADR-0011). It validates the request *shape* and returns 202 Accepted —
// "I took your request, I'll process it." It does not mean the order is confirmed.
app.MapPost("/orders", (PlaceOrderRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Customer) ||
        request.Items is null || request.Items.Count == 0 ||
        request.Items.Any(i => i.Quantity <= 0 || i.UnitPriceCents <= 0))
    {
        return Results.BadRequest(new { error = "customer and at least one valid item are required" });
    }

    var orderId = Guid.NewGuid().ToString();
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
