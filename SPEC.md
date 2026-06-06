# Spec: dotnet-event-driven-orders

An event-driven microservices **tutorial** in .NET: an order flows in over HTTP,
is published to Kafka, processed asynchronously through a state machine, and
persisted to PostgreSQL for querying. It mirrors the educational topology of
[`go-event-driven-orders`](../../golang/go-event-driven-orders) — three isolated
services around Kafka + Postgres, CQRS, idempotency, dead-lettering, replay —
but is delivered as a **lesson-branch tutorial** for people new to backend
(including curious front-end developers).

> Status: **Phase 1 (Specify) — APPROVED**. Open questions resolved (see
> Resolved Decisions). Ready to proceed to ADRs + architecture, then per-lesson Plans.

This `SPEC.md` specifies the **final system** — delivered on the **`final`**
branch, not on `main`. `main` holds the **foundation** (docs) only; the system is
built up across the lessons, whose branches are per-lesson *start states*
(ADR-0012). The incremental teaching is planned per-lesson in
`docs/plans/NN-lesson.md` (see [AGENTS.md → Docs structure](./AGENTS.md)).

---

## Assumptions

These are the .NET translation choices I'm **assuming** where we did not
explicitly decide. Correct any now or I'll proceed with them into the ADRs:

1. **.NET 9** (current LTS-adjacent SDK), one target framework across all services.
2. **HTTP:** ASP.NET Core **Minimal APIs** (`WebApplication.CreateBuilder`), no
   MVC controllers — matches the user's `phnotificationsapi` production service.
3. **Kafka client:** **`Confluent.Kafka`** directly, no bus abstraction (no
   MassTransit/NServiceBus) — matches `phnotificationsproducer/consumer`.
4. **Message contract:** **JSON** messages (not protobuf). Each service owns its
   own message DTOs; no shared C# project between services (isolation mirrors the
   Go repo's "no shared code"). The contract is documented in `docs/architecture.md`.
5. **Postgres access:** **Dapper** over **`Npgsql`**, with hand-written SQL kept
   visible (teaching goal: show the SQL, like the Go repo's `sqlc` — not hide it
   behind EF Core). *(Confirmed.)*
6. **Migrations:** **DbUp** — plain `.sql` scripts in `db/migrations/` run in order
   by a small console runner, which becomes the one-shot `migrate` container in
   compose. Keeps SQL visible (consistent with Dapper) and needs no migration
   tooling on the host. *(Confirmed — chosen over FluentMigrator, which hides SQL
   behind a C# DSL, and over a generic psql container, for being idiomatic .NET + KISS.)*
7. **Kafka:** Apache Kafka in **KRaft mode** (no Zookeeper), single broker; topic
   auto-creation disabled, a one-shot `topic-init` container creates topics with
   explicit partition counts.
8. **PostgreSQL 16.**
9. **Testing:** **xUnit** for units; **Testcontainers for .NET** for integration
   tests against real Kafka + Postgres — mirrors the Go repo's `testcontainers-go`
   and the user's existing `*.IntegrationTests` projects.
10. **Logging:** built-in `Microsoft.Extensions.Logging` with structured JSON
    console output (stdlib equivalent of the Go repo's `slog`).
11. **Config:** environment variables bound to typed options classes per service.
12. **Ports:** order-ingest HTTP `:8080`, order-query HTTP `:8081`,
    Postgres `:5432`, Kafka `:9092` — same as the Go repo.
13. **Service names:** `order-ingest` / `order-processor` / `order-query` (same).
14. **No .NET Aspire AppHost** in the tutorial — compose is the orchestrator, to
    keep the moving parts visible for beginners (the user's prod services use an
    AppHost; we deliberately don't, for teaching).

---

## Objective

**What:** Three independently deployable .NET services around Kafka + Postgres,
modelling the lifecycle of a food-delivery **Order** — built up across ~10 lessons.

**Why:** A teaching repo that takes someone new to backend from "what is a
container" to a running, resilient, event-driven system, learning the senior
parts deliberately: idempotency, dead-lettering, eventual consistency, network
isolation, CQRS.

**Users:** Learners new to backend (incl. front-end developers). The "end user"
of the running system is whoever drives the HTTP API / `curl`.

**Success looks like:** on the **`final`** branch, `docker compose up` → one
command → all services healthy → an order can be created, observed transitioning
through states, and the resilience properties (no loss under crash, poison
quarantined, safe replay) hold. `main` holds the foundation/docs only. Each
`lesson/NN-*` branch is the **start state** of that lesson (prior lessons complete
+ this lesson's `LESSON.md` + stubs to fill), and builds and runs.

### The Order lifecycle (state machine)

```
PLACED → CONFIRMED → PREPARING → DISPATCHED → DELIVERED
   └──────────────┴── CANCELLED        (CANCEL allowed only before DISPATCH)
```

Transitions are **client-driven**: a client sends one command per transition to
`order-ingest`, which emits it to Kafka. `order-processor` validates each command
against the order's *persisted* state, applies legal transitions, and
**dead-letters illegal ones** (e.g. DELIVER before DISPATCH).

### Topology (who talks to what)

| Service | HTTP | Kafka | Postgres |
|---|---|---|---|
| **order-ingest** | in: `POST /orders` (place), `POST /orders/{id}/{confirm,prepare,dispatch,deliver,cancel}`, `POST /admin/replay` | write `orders` (key=`order_id`, 3 partitions); read `orders.DLT` (replay only) | ❌ |
| **order-processor** | `/healthz`, `/readyz` only | read `orders`; write `orders.DLT` | ✅ read+write |
| **order-query** | in: `GET /orders`, `GET /orders/{id}` | ❌ | ✅ read |

Isolation is a teaching point: `order-query` never touches Kafka; `order-ingest`
never touches Postgres.

### Key mechanics

- **Commands & transitions:** lifecycle is command-driven. `order-ingest` emits an
  `OrderCommand` (PLACE on `POST /orders`; the rest on per-id endpoints) **without
  checking legality** (it has no DB). Transition endpoints return `202`; legality
  is decided asynchronously by `order-processor`.
- **CQRS in Postgres:** normalized write tables (`orders`, `order_items`,
  `order_events`) are the source of truth; a denormalized read projection
  (`order_view`) is maintained by `order-processor` and read by `order-query`.
  Read/write are eventually consistent.
- **Delivery semantics:** at-least-once. `order-processor` commits Kafka offsets
  **after** the Postgres write succeeds (manual commit).
- **Idempotency:** writes are idempotent via `INSERT ... ON CONFLICT DO NOTHING`
  on `event_id`, so redelivery and replay are safe.
- **Dead-lettering:** permanent (business-invalid) errors or exhausted transient
  retries route the message to `orders.DLT` with diagnostic headers, then commit
  the offset so a poison message never blocks its partition.
- **Replay:** operator-triggered. `POST /admin/replay` drains `orders.DLT` and
  republishes to `orders`; idempotency makes reprocessing safe.

---

## Tech Stack

- **Language / runtime:** C# / .NET 9
- **HTTP:** ASP.NET Core Minimal APIs
- **Messaging:** Apache Kafka (KRaft), client `Confluent.Kafka`
- **Storage:** PostgreSQL 16; Dapper over `Npgsql` (hand-written SQL)
- **Contract:** JSON message DTOs, owned per-service (no shared project)
- **Migrations:** DbUp (plain `.sql` scripts via a one-shot console container)
- **Logging:** `Microsoft.Extensions.Logging`, structured JSON
- **Testing:** xUnit + Testcontainers for .NET
- **Orchestration:** Docker + docker-compose
- **Tooling:** GNU Make as the command front-end

---

## Commands

```
# Lifecycle
make up           # docker compose up --build  (kafka, postgres, topic-init, migrate, the 3 services)
make down         # docker compose down -v
make logs         # tail compose logs
make ps           # compose ps

# Build / quality
make build        # docker compose build  (or dotnet build per service)
make test         # dotnet test  (units + Testcontainers integration)
make format       # dotnet format

# Demo / evidence
make seed         # create a few orders via POST /orders and drive transitions
make replay       # POST /admin/replay → drain orders.DLT back into orders
make lag          # show Kafka consumer-group lag
```

*(Exact targets are finalized in lesson 1's plan; this is the intended shape.)*

---

## Project Structure

```
dotnet-event-driven-orders/
├── README.md                 thesis, diagram, curl walkthrough, lesson index
├── AGENTS.md                 router + non-negotiables + docs-structure rule
├── SPEC.md                   this file
├── Makefile
├── docker-compose.yml        kafka, postgres, topic-init, migrate, 3 services
├── .env.example              committed; real .env is gitignored
│
├── db/
│   └── migrations/           *.sql applied by the one-shot migrate container
│
├── docs/
│   ├── architecture.md       diagram + data flow + topic/table design + JSON contract
│   ├── adr/                  ADRs 0001..N (one per real decision)
│   ├── plans/                NN-lesson.md (one plan per lesson)
│   └── ideas/                idea-refine one-pagers
│
├── order-ingest/             SERVICE 1 — own .csproj + Dockerfile + image
│   ├── OrderIngest.csproj     Minimal API, Kafka producer, own JSON DTOs
│   └── tests/                 unit + integration tests for this service
├── order-processor/          SERVICE 2 — own .csproj + Dockerfile + image
│   ├── OrderProcessor.csproj  Kafka consumer, state machine, Postgres (Dapper)
│   └── tests/                 state-machine units + Testcontainers integration
├── order-query/              SERVICE 3 — own .csproj + Dockerfile + image
│   ├── OrderQuery.csproj      Minimal API, Postgres read (NO Kafka)
│   └── tests/
└── tools/
    └── migrate/              DbUp console runner → one-shot compose container

# Vendored verbatim from addyosmani/agent-skills (see ATTRIBUTION.md):
#   skills/  references/  agents/
```

**Fully separate per-service folders** (confirmed): each service is its own
project with its own `Dockerfile` and image, no shared `.csproj` and no spanning
`.sln` — they are built and run as independent containers. This mirrors the Go
repo's per-service `go.mod` isolation. Tests live beside each service.

---

## Code Style

Idiomatic modern C#: nullable reference types on, file-scoped namespaces, `async`
all the way through I/O, `CancellationToken` threaded through, structured logging,
graceful shutdown via `IHostApplicationLifetime`. Example — the order-processor's
consume loop sketch (at-least-once, commit after handling):

```csharp
public async Task RunAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var result = _consumer.Consume(ct); // does not commit

        try
        {
            await _handler.HandleAsync(result.Message, ct);
        }
        catch (Exception ex)
        {
            // permanent/exhausted → dead-letter, then commit to unblock the partition
            await _deadLetter.SendAsync(result, ex, ct);
            _log.LogWarning(ex, "Message dead-lettered at offset {Offset}", result.Offset);
        }

        _consumer.Commit(result); // commit AFTER handling (or dead-lettering)
    }
}
```

Conventions: `dotnet format` clean; exceptions wrapped with context; no swallowed
exceptions in consume paths; public APIs documented with XML doc comments where
non-obvious; tests are `[Theory]`-driven where it fits.

---

## Testing Strategy

A test pyramid (most tests are fast units):

- **Unit (majority):** the order **state machine** (valid/invalid transitions),
  validation, dead-letter routing decisions. Pure, no I/O, `[Theory]`-driven.
- **Integration (few, high-value):** Testcontainers spins real Kafka + Postgres to
  verify: produce→consume→persist, **idempotency** (same event twice → one row),
  **at-least-once across a crash**, and **dead-letter + replay**.
- **Manual / evidence:** the `make seed` happy path and a documented poison/replay
  walkthrough are the end-to-end proof.

Tests live in a `tests/` folder beside each service (per-service isolation). No
coverage-percentage gate; coverage is meaningful tests on the state machine and
the consumer's correctness paths.

---

## Boundaries

**Always:**
- Each `lesson/NN-*` branch builds and runs; its `LESSON.md` explains the "why".
- Write the failing test first for domain logic (state machine, validation).
- `make test` and `make format` green before any commit.
- Commit the Kafka offset only *after* the DB write (or dead-letter) succeeds.
- Make consumer writes idempotent (`ON CONFLICT`).
- Keep `SPEC.md`, ADRs, and lesson plans updated when a decision changes.

**Ask first:**
- Adding a NuGet dependency, a new service, or a new Kafka topic.
- Changing the JSON message contract (it's the cross-service contract).
- Database schema / migration changes.
- Changing ports or the network-isolation rules in the topology table.
- Adding infrastructure (metrics stack, registry, proxy, Aspire AppHost, etc.).

**Never:**
- Commit secrets or a real `.env`.
- Share C# code between the three services (contract is JSON, documented).
- Give `order-query` Kafka access, or `order-ingest` Postgres access.
- Remove or skip a failing test to make a suite pass.
- Pull in a frontend, Kubernetes, or cloud deployment — out of scope for this tutorial.

---

## Success Criteria

Specific, testable conditions for "done" (verified on the **`final`** branch;
`main` holds foundation docs only):

1. **One-command boot:** `make up` brings up kafka, postgres, topic-init, migrate,
   and the three services; all report healthy via compose healthchecks.
2. **Happy path:** `POST /orders` returns `202` with an order id; the transition
   commands (`confirm`→`prepare`→`dispatch`→`deliver`) drive it through
   `PLACED → … → DELIVERED`, observable via `GET /orders/{id}`. `make seed` does this.
3. **Validation:** malformed `POST /orders` returns `400` and produces nothing to Kafka.
4. **Idempotency:** the same event delivered twice → exactly one row (integration test).
5. **No loss under crash:** kill the order-processor mid-load and restart;
   afterwards `processed + dlq == produced` (zero lost orders), lag returns to ~0.
6. **Poison quarantine:** business-invalid commands land in `orders.DLT`; no good
   order is dead-lettered; the partition never stalls.
7. **Safe replay:** `make replay` drains `orders.DLT` into `orders`; now-valid
   messages process; already-processed ones are no-ops.
8. **Quality gates:** `make test` and `make format` pass.
9. **Tutorial integrity:** each `lesson/NN-*` branch is the start state of that
   lesson (prior lessons complete + this lesson's `LESSON.md`) and builds and runs;
   `lesson/N+1` is the solution to lesson `N`; the `final` branch is the finished
   system; `main` is the foundation; every lesson branch has a `LESSON.md`.

---

## Resolved Decisions

1. **Postgres access:** **Dapper + hand-written SQL** (keeps SQL visible, mirrors
   the Go repo's `sqlc`; not EF Core). → ADR.
2. **Query read API shape:** **offset pagination** —
   `GET /orders?status=&limit=&offset=` with optional status filter.
3. **Migration tool:** **DbUp** — plain `.sql` scripts run by a one-shot console
   container (idiomatic .NET + KISS; keeps SQL visible; not FluentMigrator/psql). → ADR.
4. **Service layout:** **fully separate per-service folders**, each its own
   project + `Dockerfile` + image, no shared `.sln` — built and run as independent
   containers (mirrors the Go repo's per-service `go.mod`). → ADR.
5. **No Aspire AppHost:** compose is the orchestrator (moving parts visible for
   teaching), diverging deliberately from the user's production services. → ADR.
