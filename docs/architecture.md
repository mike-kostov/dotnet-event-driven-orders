# Architecture

How `dotnet-event-driven-orders` fits together. For the *why* behind each choice,
see the ADRs in [`docs/adr/`](adr). This describes the **final system** — delivered
on the **`final`** branch, not on `main` (ADR-0012); it is built up incrementally
across the lessons, whose branches are per-lesson start states
(see [`docs/plans/`](plans)).

## Data flow

```
                POST /orders                          consume (group: order-processor)
  client ─────▶ order-ingest ──produce──▶  ( orders )  ─────────────▶  order-processor
  POST /orders/{id}/{transition}          3 partitions                   │
                  │  key = order_id                                      │ validate transition
                  │  (returns 202)                                       │ (state machine)
                  │                                          ┌───────────┴───────────┐
                  │                                          │                       │
                  │                                     legal? apply            illegal/poison
                  │                                          │                       │
                  ▼ POST /admin/replay                       ▼                       ▼
            (drain orders.DLT)                          Postgres                ( orders.DLT )
                  │  republish                      write model + order_view      1 partition
                  └────────────▶ ( orders )                  ▲
                                                             │ read
  client ── GET /orders[/{id}] ──────────────────────▶ order-query
```

- **Write path:** `order-ingest` validates request *shape*, builds an
  `OrderCommand`, and produces it to `orders` keyed by `order_id` (so all commands
  for one order share a partition and are processed in order). It has no database
  and does not check transition legality (ADR-0011).
- **Processing:** `order-processor` consumes, looks up the order's current state,
  runs the state machine, and — in one transaction — appends to the event log,
  updates the write model, and updates the read projection.
- **Read path:** `order-query` serves the denormalized `order_view` projection. No
  Kafka access.
- **Failure path:** business-invalid commands or exhausted transient retries are
  dead-lettered to `orders.DLT` (ADR-0007).

## The order lifecycle

```
PLACED → CONFIRMED → PREPARING → DISPATCHED → DELIVERED
   └──────────────┴── CANCELLED        (CANCEL allowed only before DISPATCH)
```

Transitions are **client-driven** (ADR-0011): each is a command sent to
`order-ingest`, which emits it without checking legality (it has no DB).
`order-processor` validates against persisted state and dead-letters illegal
commands. The state machine is pure domain logic in `order-processor`, unit-tested
as a legal/illegal matrix.

## Services

Three isolated services, no shared code (ADR-0002), each its own project +
`Dockerfile` + image, built and run as independent containers (ADR-0013).

| Service | HTTP | Kafka | Postgres |
|---|---|---|---|
| **order-ingest** | `POST /orders`, `POST /orders/{id}/{confirm,prepare,dispatch,deliver,cancel}`, `POST /admin/replay` | write `orders`; read `orders.DLT` (replay only) | ❌ |
| **order-processor** | `/healthz`, `/readyz` only | read `orders`; write `orders.DLT` | ✅ read+write |
| **order-query** | `GET /orders`, `GET /orders/{id}` | ❌ | ✅ read |

HTTP surfaces use ASP.NET Core Minimal APIs (ADR-0004).

## Topics

| Topic | Partitions | Key | Producers | Consumers |
|---|---|---|---|---|
| `orders` | 3 | `order_id` | `order-ingest`; `order-ingest` (replay) | `order-processor` (group `order-processor`) |
| `orders.DLT` | 1 | original | `order-processor` | `order-ingest` (group `order-ingest-replay`, replay only) |

Topic auto-creation is disabled; a one-shot `topic-init` container creates them
with explicit partition counts. The client is `Confluent.Kafka` used directly,
with manual offset commit (ADR-0003, ADR-0006).

## The message contract

JSON (ADR-0008), each service owning its own DTOs. `OrderCommand`:

```json
{
  "event_id": "0b5d…",          // unique; the idempotency key
  "order_id": "a17f…",          // partition key
  "type": "PLACE",              // PLACE|CONFIRM|PREPARE|DISPATCH|DELIVER|CANCEL
  "issued_at": "2026-06-05T10:00:00Z",
  "items": [                    // PLACE only
    { "sku": "MARGHERITA", "qty": 1, "unit_price_cents": 1200 }
  ],
  "total_cents": 1200,          // PLACE only
  "customer": "alice@example.com" // PLACE only
}
```

Serialized with `System.Text.Json`. The contract is documented prose + example,
not a schema — drift is caught by integration tests across the two services.

## Storage (CQRS in Postgres, ADR-0005)

Write model (source of truth), accessed via Dapper + hand-written SQL (ADR-0009):

- `orders` — current state per order.
- `order_items` — line items, written on `PLACE`.
- `order_events` — append-only log; `event_id` is the **primary key**, making it
  the idempotency anchor (`INSERT … ON CONFLICT DO NOTHING`).

Read model:

- `order_view` — denormalized projection (items embedded as JSONB), maintained by
  `order-processor` and served by `order-query`. Eventually consistent with the
  write model.

Schema lives in `db/migrations/` as plain `.sql`, applied by the DbUp one-shot
`migrate` container (ADR-0010).

## Delivery semantics (ADR-0006)

At-least-once. `order-processor` commits Kafka offsets **only after** the DB write
(or dead-letter) succeeds, and all writes are idempotent on `event_id`. The
combination yields effectively-once *effects* on the database — redelivery after a
crash, and replay, are both safe.

## Failure handling & replay (ADR-0007)

- Transient errors → bounded in-process retry with backoff.
- Permanent errors or exhausted retries → publish to `orders.DLT` with headers
  (`x-error`, `x-attempts`, `x-original-topic/partition/offset`, `x-failed-at`),
  then commit the offset so the partition keeps moving.
- Replay (`POST /admin/replay`) drains `orders.DLT` back into `orders`. It's
  operator-triggered — run it after fixing the root cause; idempotency makes it safe.

## Runtime topology

`docker-compose.yml` (ADR-0013) wires: `kafka` (KRaft, single node) and `postgres`
(both health-gated), the one-shot `topic-init` and `migrate` (DbUp) containers,
then the three services — each gated on its dependencies (`depends_on` with
`service_healthy` / `service_completed_successfully`) and health-checked via
`/healthz`. `docker compose up` is the single entry point.
