namespace OrderQuery.Contracts;

// A row read from the order_view projection. Items is kept as raw JSON text
// (selected via items::text) and parsed into the response so it nests properly.
//
// Plain get/set properties (not a positional record) so Dapper maps result
// columns by name — forgiving about nullability and the timestamptz->DateTime
// mapping Npgsql uses.
public sealed class OrderRow
{
    public string OrderId { get; set; } = "";
    public string State { get; set; } = "";
    public string? Customer { get; set; }
    public int? TotalCents { get; set; }
    public string Items { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}
