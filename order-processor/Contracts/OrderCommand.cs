namespace OrderProcessor.Contracts;

// order-processor's OWN copy of the message shape (no shared project — ADR-0002).
// It must match the JSON that order-ingest produces.
public sealed record OrderCommand(
    string EventId,
    string OrderId,
    string Type,
    DateTimeOffset IssuedAt,
    string? Customer,
    IReadOnlyList<OrderItem>? Items,
    int? TotalCents);

public sealed record OrderItem(
    string Sku,
    int Quantity,
    int UnitPriceCents);
