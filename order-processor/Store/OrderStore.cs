using System.Text.Json;
using Dapper;
using Npgsql;
using OrderProcessor.Contracts;

namespace OrderProcessor;

// Persists orders with Dapper + hand-written SQL (ADR-0009). CQRS write model
// (orders/order_items/order_events) + the read projection (order_view), ADR-0005.
public sealed class OrderStore
{
    private readonly string _connString;

    public OrderStore(IConfiguration config) =>
        _connString = config["POSTGRES_CONN"]
            ?? "Host=postgres;Username=orders;Password=orders;Database=orders";

    // The SQL is hand-written and visible — that's the point of using Dapper.
    private const string InsertEvent = @"
        INSERT INTO order_events (event_id, order_id, type, issued_at)
        VALUES (@EventId, @OrderId, @Type, @IssuedAt);";

    private const string UpsertOrder = @"
        INSERT INTO orders (order_id, state, customer, total_cents)
        VALUES (@OrderId, 'PLACED', @Customer, @TotalCents)
        ON CONFLICT (order_id) DO NOTHING;";

    private const string InsertItem = @"
        INSERT INTO order_items (order_id, sku, quantity, unit_price_cents)
        VALUES (@OrderId, @Sku, @Quantity, @UnitPriceCents);";

    private const string UpsertView = @"
        INSERT INTO order_view (order_id, state, customer, total_cents, items)
        VALUES (@OrderId, 'PLACED', @Customer, @TotalCents, @Items::jsonb)
        ON CONFLICT (order_id) DO UPDATE
            SET state = EXCLUDED.state, customer = EXCLUDED.customer,
                total_cents = EXCLUDED.total_cents, items = EXCLUDED.items,
                updated_at = now();";

    public async Task SavePlacedOrderAsync(OrderCommand cmd)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        // One transaction: event log + write model + projection move together.
        await using var tx = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync(InsertEvent, new { cmd.EventId, cmd.OrderId, cmd.Type, cmd.IssuedAt }, tx);
        await conn.ExecuteAsync(UpsertOrder, new { cmd.OrderId, cmd.Customer, cmd.TotalCents }, tx);
        foreach (var i in cmd.Items ?? new List<OrderItem>())
            await conn.ExecuteAsync(InsertItem, new { cmd.OrderId, i.Sku, i.Quantity, i.UnitPriceCents }, tx);
        var itemsJson = JsonSerializer.Serialize(cmd.Items ?? new List<OrderItem>());
        await conn.ExecuteAsync(UpsertView, new { cmd.OrderId, cmd.Customer, cmd.TotalCents, Items = itemsJson }, tx);
        await tx.CommitAsync();
    }
}
