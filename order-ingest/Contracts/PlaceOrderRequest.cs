namespace OrderIngest.Contracts;

// The JSON body a client POSTs to /orders to place an order.
// These DTOs are OWNED by order-ingest (no shared project — see ADR-0002/0008).

public sealed record PlaceOrderRequest(
    string Customer,
    IReadOnlyList<OrderItem> Items);

public sealed record OrderItem(
    string Sku,
    int Quantity,
    int UnitPriceCents);

// What we return on success: the id assigned to the accepted order.
public sealed record PlaceOrderResponse(string OrderId);
