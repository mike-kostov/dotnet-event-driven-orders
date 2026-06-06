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

    // event_id is the idempotency anchor (ADR-0006): ON CONFLICT DO NOTHING makes
    // a redelivered/replayed event a no-op. ExecuteAsync returns 0 rows on conflict.
    private const string InsertEvent = @"
        INSERT INTO order_events (event_id, order_id, type, issued_at)
        VALUES (@EventId, @OrderId, @Type, @IssuedAt)
        ON CONFLICT (event_id) DO NOTHING;";

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

    private const string SelectState = @"SELECT state FROM orders WHERE order_id = @OrderId;";

    private const string UpdateOrderState = @"
        UPDATE orders SET state = @State, updated_at = now() WHERE order_id = @OrderId;";

    private const string UpdateViewState = @"
        UPDATE order_view SET state = @State, updated_at = now() WHERE order_id = @OrderId;";

    public async Task SavePlacedOrderAsync(OrderCommand cmd)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // Idempotency (ADR-0006): ON CONFLICT makes InsertEvent affect 0 rows on a
        // duplicate event_id — skip the rest so a redelivery doesn't double-write.
        var inserted = await conn.ExecuteAsync(InsertEvent, new { cmd.EventId, cmd.OrderId, cmd.Type, cmd.IssuedAt }, tx);
        if (inserted == 0) { await tx.CommitAsync(); return; }

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

        // Same idempotency guard: a redelivered transition is a no-op.
        var inserted = await conn.ExecuteAsync(InsertEvent, new { cmd.EventId, cmd.OrderId, cmd.Type, cmd.IssuedAt }, tx);
        if (inserted == 0) { await tx.CommitAsync(); return; }

        await conn.ExecuteAsync(UpdateOrderState, new { cmd.OrderId, State = newState }, tx);
        await conn.ExecuteAsync(UpdateViewState, new { cmd.OrderId, State = newState }, tx);
        await tx.CommitAsync();
    }
}
