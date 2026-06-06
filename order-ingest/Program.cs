using OrderIngest.Contracts;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Liveness probe. Compose can health-gate on this. (given)
app.MapGet("/healthz", () => Results.Ok("healthy"));

// POST /orders — accept a new order ("PLACE").
//
// order-ingest has NO database and does NOT decide whether a transition is
// legal (ADR-0011). This lesson it only validates the request *shape* and
// returns 202 Accepted — "I took your request, I'll process it." It does not
// mean the order is confirmed. (Publishing to Kafka comes in lesson 3.)
app.MapPost("/orders", (PlaceOrderRequest request) =>
{
    // TODO(you) 2.1 — validate the request SHAPE. Return 400 if any of:
    //   • Customer is null or empty
    //   • Items is null or empty
    //   • any item has Quantity <= 0 or UnitPriceCents <= 0
    //   hint: return Results.BadRequest(new { error = "..." });

    // TODO(you) 2.2 — generate a new order id.
    //   hint: var orderId = Guid.NewGuid().ToString();

    // TODO(you) 2.3 — log that the order was accepted, so you can see it work.
    //   hint: app.Logger.LogInformation("Accepted order {OrderId}", orderId);

    // TODO(you) 2.4 — return 202 Accepted with the order id.
    //   hint: return Results.Accepted($"/orders/{orderId}", new PlaceOrderResponse(orderId));

    return Results.StatusCode(501); // placeholder — replace using the TODOs above
});

app.Run();
