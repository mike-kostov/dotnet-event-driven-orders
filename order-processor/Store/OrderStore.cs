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

    // --- transitions (lesson 6) ---
    private const string SelectState = @"SELECT state FROM orders WHERE order_id = @OrderId;";

    private const string UpdateOrderState = @"
        UPDATE orders SET state = @State, updated_at = now() WHERE order_id = @OrderId;";

    private const string UpdateViewState = @"
        UPDATE order_view SET state = @State, updated_at = now() WHERE order_id = @OrderId;";

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

    // Current persisted state, or null if the order doesn't exist yet.
    public async Task<string?> LoadStateAsync(string orderId)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<string?>(SelectState, new { OrderId = orderId });
    }

    // Apply a legal transition: append the event and update both state copies, atomically.
    public async Task ApplyTransitionAsync(OrderCommand cmd, string newState)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync(InsertEvent, new { cmd.EventId, cmd.OrderId, cmd.Type, cmd.IssuedAt }, tx);
        await conn.ExecuteAsync(UpdateOrderState, new { cmd.OrderId, State = newState }, tx);
        await conn.ExecuteAsync(UpdateViewState, new { cmd.OrderId, State = newState }, tx);
        await tx.CommitAsync();
    }
}
