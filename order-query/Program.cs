using System.Text.Json.Nodes;
using Dapper;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderQuery.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Observability (lesson 10): structured JSON logs + OpenTelemetry traces (console).
builder.Logging.AddJsonConsole();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("order-query"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddConsoleExporter());

// Read-only Postgres access. NO Kafka client here at all — order-query is the
// read side and must stay isolated from the write path (ADR-0002).
var connString = builder.Configuration["POSTGRES_CONN"]
    ?? "Host=postgres;Username=orders;Password=orders;Database=orders";
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connString).Build());

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok("healthy"));

// GET /orders/{id} — one order from the read projection (404 if not found).
app.MapGet("/orders/{id}", async (string id, NpgsqlDataSource db) =>
{
    const string sql = @"SELECT order_id AS OrderId, state AS State, customer AS Customer,
                                total_cents AS TotalCents, items::text AS Items, updated_at AS UpdatedAt
                         FROM order_view WHERE order_id = @id;";

    await using var conn = await db.OpenConnectionAsync();
    var row = await conn.QuerySingleOrDefaultAsync<OrderRow>(sql, new { id });
    return row is null ? Results.NotFound() : Results.Ok(ToResponse(row));
});

// GET /orders?status=&limit=&offset= — a page of orders, newest first.
app.MapGet("/orders", async (NpgsqlDataSource db, string? status, int limit = 20, int offset = 0) =>
{
    const string sql = @"SELECT order_id AS OrderId, state AS State, customer AS Customer,
                                total_cents AS TotalCents, items::text AS Items, updated_at AS UpdatedAt
                         FROM order_view
                         WHERE (@status IS NULL OR state = @status)
                         ORDER BY updated_at DESC
                         LIMIT @limit OFFSET @offset;";

    await using var conn = await db.OpenConnectionAsync();
    var rows = await conn.QueryAsync<OrderRow>(sql,
        new { status, limit = Math.Clamp(limit, 1, 100), offset = Math.Max(offset, 0) });
    return Results.Ok(rows.Select(ToResponse));
});

app.Run();

// Maps a projection row to a response, parsing the items JSON so it nests as a
// real array instead of a quoted string. (Given — not the focus of this lesson.)
static object ToResponse(OrderRow r) => new
{
    orderId = r.OrderId,
    state = r.State,
    customer = r.Customer,
    totalCents = r.TotalCents,
    items = JsonNode.Parse(r.Items),
    updatedAt = r.UpdatedAt,
};
