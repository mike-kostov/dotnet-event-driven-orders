# Lesson 05 — Persistence & CQRS write model

> **You are on** `lesson/05-persistence-cqrs`. Lesson 4 is complete here
> (order-processor consumes + logs). Now you give it a **memory**: persist PLACE
> commands to Postgres. Fill in the `TODO(you)` and check against
> `lesson/06-state-machine-transitions`.

---

## 1. Why this lesson exists

So far consumed messages vanish into the log. Now order-processor records them
durably. We use **CQRS** (ADR-0005): a normalized **write model** that's the
source of truth (`orders`, `order_items`, `order_events`), plus a denormalized
**read projection** (`order_view`) shaped for fast queries (order-query reads it
in lesson 7). We write the SQL by hand with **Dapper** (ADR-0009) and manage the
schema as plain `.sql` scripts run by **DbUp** (ADR-0010) — nothing about the
database is hidden.

`order_events.event_id` is the primary key — the **idempotency anchor** lesson 9
will lean on.

---

## 2. Concepts

- **Migration** — a versioned change to the schema. DbUp runs `db/migrations/*.sql`
  in order and records which ran (re-running is a no-op). It's the one-shot
  `migrate` container; the schema exists before order-processor starts.
- **Dapper** — a thin layer over ADO.NET: you write SQL, it maps parameters and
  rows. The SQL stays visible (read `OrderStore.cs`).
- **Transaction** — all-or-nothing. We write the event, the order, its items, and
  the projection **together**; if any fails, none apply.
- **CQRS** — write model (correct, normalized) vs read projection (fast,
  denormalized). They're **eventually consistent**.

Read `db/migrations/0001_init.sql` (the schema) and `order-processor/Store/OrderStore.cs`
(the SQL) before you start.

---

## 3. Do this — persist on PLACE  ← main task

Two `TODO(you)` markers:

**5.1 — the transaction** (`order-processor/Store/OrderStore.cs`, `SavePlacedOrderAsync`).
The SQL constants are written for you; wire them into **one transaction** with
Dapper (`BeginTransactionAsync` → `ExecuteAsync` each, passing the tx → `CommitAsync`).
The exact lines are in the TODO. Remove the `await Task.CompletedTask;` placeholder.

**5.2 — call it from the consumer** (`order-processor/ConsumerService.cs`).
For a PLACE command, persist before logging:
```csharp
if (cmd is { Type: "PLACE" })
    await _store.SavePlacedOrderAsync(cmd);
```

---

## 4. Run it and inspect the database

```bash
cp .env.example .env            # if needed
make up                         # infra → migrate (creates schema) → services
```

Place an order, then look in Postgres:

```bash
curl -s -o /dev/null -w '%{http_code}\n' -X POST localhost:8080/orders \
  -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[{"sku":"MARGHERITA","quantity":2,"unitPriceCents":1200}]}'

make psql
```

At the `psql` prompt:
```sql
SELECT order_id, state, total_cents FROM orders;
SELECT type FROM order_events;
SELECT order_id, jsonb_array_length(items) AS item_count FROM order_view;
\q
```

You should see one row in each — written atomically. 🎉

---

## 5. Your turn — prove the transaction is atomic

Add a second migration to see DbUp apply only the new script:

1. Create `db/migrations/0002_add_index.sql`:
   ```sql
   CREATE INDEX IF NOT EXISTS idx_order_events_order_id ON order_events(order_id);
   ```
2. `make migrate` — DbUp runs **only** the new script (0001 is skipped).
3. `make psql` → `\di` shows the new index.

---

## 6. You're done when

- [ ] `make up` runs `migrate` and the four tables exist (`\dt` in `make psql`).
- [ ] A PLACE order writes one row each into `orders`, `order_events`,
      `order_items`, and `order_view` — in a single transaction.
- [ ] `make migrate` applies a new script without re-running old ones.
- [ ] You can explain the write-model vs read-projection split.

Check your work:

```bash
git diff lesson/06-state-machine-transitions -- order-processor
```

---

## 7. Next

In **lesson 06** you add the **state machine** and the transition commands
(confirm → … → deliver), validating each against persisted state. Check out
`lesson/06-state-machine-transitions`.

<!-- NAV:START -->

---

## 🧭 Navigate

| ◀ Previous | 🔑 Solution to this lesson | Next ▶ |
|:---|:---:|---:|
| [Lesson 04 — Consumer](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/lesson/04-kafka-consumer) | [view the diff](https://github.com/mike-kostov/dotnet-event-driven-orders/compare/lesson/05-persistence-cqrs...lesson/06-state-machine-transitions) | [Lesson 06 — State machine](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/lesson/06-state-machine-transitions) |

Or from the terminal: `make prev` · `make solution` · `make next` · `make goto LESSON=5`
*(`next`/`prev`/`goto` switch branches — commit or stash your edits first; they never discard your work.)*

<!-- NAV:END -->
