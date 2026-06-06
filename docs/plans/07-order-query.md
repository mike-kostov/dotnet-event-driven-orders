# Lesson 07 — The read side: order-query

> Plan for authoring `lesson/07-order-query`. Builds on `lesson/06-state-machine-transitions`.
> Decisions: [ADR-0005](../adr/0005-cqrs-with-postgres-only.md),
> [ADR-0002](../adr/0002-three-isolated-services-no-shared-code.md),
> [ADR-0004](../adr/0004-aspnet-minimal-apis-over-mvc.md).

## Objective

Build the third and final service, `order-query`: a Minimal API that serves the
denormalized `order_view` projection over HTTP. It reads Postgres only — **no
Kafka** — completing the CQRS picture and the three-service topology.

After this lesson a learner can: build a read-side service over a projection,
implement offset pagination + filtering, and explain read/write isolation and
eventual consistency end-to-end.

## The "why" (for LESSON.md)

Reads and writes have different needs. The write side optimizes for correctness
(normalized, transactional); the read side optimizes for fast, simple queries —
served from the `order_view` projection the processor maintains. order-query is
deliberately tiny and has **no Kafka client at all** (ADR-0002): isolation you can
see. Because the projection lags the write model slightly, `GET /orders/{id}` right
after a transition demonstrates **eventual consistency** in the flesh.

## The slice this lesson builds

- `order-query/` — its own project, `Dockerfile`, image; Minimal API:
  - `GET /orders?status=&limit=&offset=` — offset pagination + optional status filter.
  - `GET /orders/{id}` — single order from `order_view` (404 if absent).
  - `GET /healthz`. Dapper reads; no Kafka dependency.
- `docker-compose.yml` — adds order-query (port `:8081`), gated on migrate/postgres.
- `Makefile` — `make seed` (place an order, drive transitions, then query it) as the
  end-to-end happy-path demo.
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: order-query project + read endpoints
**Acceptance:**
- [ ] `GET /orders/{id}` returns the projected order or 404.
- [ ] `GET /orders` supports `status`, `limit`, `offset`.
**Verify:** place + transition an order, then `curl` both endpoints.
**Files:** `order-query/OrderQuery.csproj`, `Program.cs`, `Store/*.cs`
**Scope:** M

### Task 2: Dockerfile + compose wiring (port 8081)
**Acceptance:** runs as a container, health-gated, no Kafka access.
**Verify:** `make up`; `make ps` healthy; `curl localhost:8081/...`.
**Files:** `order-query/Dockerfile`, `docker-compose.yml`
**Scope:** S

### Task 3: `make seed` end-to-end demo
**Acceptance:** one command places an order, drives it to DELIVERED, and prints the
queried result.
**Verify:** `make seed` shows the full lifecycle via query.
**Files:** `Makefile`, `scripts/seed.*`
**Scope:** S

### Task 4: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — read vs write needs; projection; isolation (no Kafka here).
2. **Concepts** — serving a projection; offset pagination; eventual consistency.
3. **Do this** — `make seed`; query by id and by status; observe a brief read-lag.
4. **Inspect** — confirm order-query has no Kafka client; read the SQL.
5. **Your turn** — add a filter (e.g. by customer) end-to-end.
6. **You're done when** — checklist.
7. **Next** — lesson 08 proves all this with tests.

## Checkpoint / done criteria
- [ ] Three services run together; `make seed` shows place → transitions → query.
- [ ] order-query has zero Kafka dependency (isolation verified).
- [ ] Learner can explain CQRS + eventual consistency end-to-end.

## Dependencies & next
- **Depends on:** lesson 06 (state advancing in the projection).
- **Feeds:** lesson 08 (testing), and the final polish in lesson 10.

## Risks / open questions
- Pagination contract details (max `limit`, default ordering) — pick sensible
  defaults and document them in the endpoint.
