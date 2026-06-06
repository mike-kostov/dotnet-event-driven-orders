# Lesson 05 — Persistence & CQRS write model

> Plan for authoring `lesson/05-persistence-cqrs`. Builds on `lesson/04-kafka-consumer`.
> Decisions: [ADR-0005](../adr/0005-cqrs-with-postgres-only.md),
> [ADR-0009](../adr/0009-dapper-handwritten-sql-over-ef-core.md),
> [ADR-0010](../adr/0010-dbup-for-migrations.md).

## Objective

Give `order-processor` a memory: persist PLACE commands to PostgreSQL. Introduce
the schema via **DbUp** migrations, query it with **Dapper** + hand-written SQL,
and set up the **CQRS** split — write model (`orders`, `order_items`,
`order_events`) plus a read projection (`order_view`).

After this lesson a learner can: write SQL migrations run by DbUp, use Dapper to
read/write Postgres, and explain the write-model vs read-model distinction.

## The "why" (for LESSON.md)

So far messages vanish into logs. Now the processor records them durably. We keep
the **write model** normalized and correct (the source of truth), and maintain a
separate **read projection** (`order_view`) shaped for fast queries — that's
**CQRS** (ADR-0005). We write the SQL by hand (Dapper, ADR-0009) and manage schema
as plain `.sql` scripts (DbUp, ADR-0010) so nothing about the database is hidden.
`order_events` is append-only with `event_id` as its primary key — the anchor that
makes idempotency possible in lesson 09.

## The slice this lesson builds

- `db/migrations/*.sql` — DbUp scripts creating `orders`, `order_items`,
  `order_events` (PK `event_id`), and `order_view` (JSONB items).
- `tools/migrate/` — DbUp console runner; one-shot `migrate` container in compose,
  gated before order-processor and order-query.
- `order-processor`: on PLACE, in **one transaction** — insert `order_events`,
  upsert `orders` (state = PLACED), insert `order_items`, and update `order_view`.
  Dapper + hand-written SQL.
- `docker-compose.yml` — adds `migrate` one-shot; processor now depends on it.
- `Makefile` — `make psql` (open a psql shell), `make migrate` (run migrations).
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: DbUp migrate runner + first migration
**Acceptance:** `migrate` container applies `.sql` scripts; re-running is a no-op
(SchemaVersions tracking).
**Verify:** `make up`; `make psql` → `\dt` shows the tables; re-run `make up` skips applied scripts.
**Files:** `tools/migrate/*`, `db/migrations/0001_init.sql`, `docker-compose.yml`
**Scope:** M

### Task 2: Schema for write model + projection
**Acceptance:** tables match ADR-0005 (`order_events.event_id` PK; `order_view` JSONB items).
**Verify:** inspect schema via `make psql`.
**Files:** `db/migrations/0001_init.sql`
**Scope:** S

### Task 3: Dapper persistence on PLACE (transactional)
**Acceptance:**
- [ ] A PLACE command results in rows in `order_events`, `orders` (PLACED),
      `order_items`, and an `order_view` row — all-or-nothing in one transaction.
**Verify:** `curl` POST → `make psql` shows the rows; a forced failure rolls back all.
**Files:** `order-processor/Store/*.cs` (SQL + Dapper), `Worker.cs`
**Scope:** M

### Task 4: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — durability; CQRS write vs read model; append-only event log.
2. **Concepts** — DbUp scripts & version tracking (ADR-0010); Dapper + hand-written
   SQL (ADR-0009); transactions; `order_view` projection (ADR-0005).
3. **Do this** — `make up`; POST an order; `make psql` and inspect all four writes.
4. **Inspect** — read the migration SQL and the Dapper transaction.
5. **Your turn** — add a column via a new migration; observe DbUp apply only the new one.
6. **You're done when** — checklist.
7. **Next** — lesson 06 adds the state machine and the transition commands.

## Checkpoint / done criteria
- [ ] `migrate` applies schema; idempotent across re-runs.
- [ ] PLACE persists write model + projection transactionally.
- [ ] Learner can explain CQRS split and read/write the SQL.

## Dependencies & next
- **Depends on:** lesson 04 (a consumer to persist from).
- **Feeds:** lesson 06 (state machine), lesson 07 (query reads the projection).

## Risks / open questions
- Connection management/pooling shape with Npgsql — keep a single configured
  data source; show it once, don't over-abstract.
