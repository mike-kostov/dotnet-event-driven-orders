namespace OrderIngest.Contracts;

// The message order-ingest publishes to Kafka. JSON on the wire (ADR-0008),
// owned by this service. For now only PLACE is produced; transitions come later.
public sealed record OrderCommand(
    string EventId,                      // unique id for THIS command — the idempotency key (used in later lessons)
    string OrderId,                      // the Kafka partition key
    string Type,                         // "PLACE"
    DateTimeOffset IssuedAt,
    string? Customer,                    // PLACE only
    IReadOnlyList<OrderItem>? Items,     // PLACE only
    int? TotalCents);                    // PLACE only
