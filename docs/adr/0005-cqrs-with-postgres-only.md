# ADR-0005: CQRS with Postgres only

## Status
Accepted

## Date
2026-06-05

## Context
The write side (order-processor) and the read side (order-query) have different
shapes: writes are normalized and transactional; reads want a denormalized view
served fast. We need a storage design that demonstrates CQRS without dragging in
a second datastore that would complicate `docker compose up`.

## Decision
Apply **CQRS within a single PostgreSQL database**:

- **Write model (source of truth):** `orders` (current state), `order_items`
  (line items, written on PLACE), `order_events` (append-only log; `event_id` is
  the primary key — the idempotency anchor).
- **Read model:** `order_view` — a denormalized projection (items embedded as
  JSONB), maintained by `order-processor` and served by `order-query`.

Read and write are **eventually consistent**: the projection is updated in the
same transaction as the write model, but `order-query` reads it independently.

## Alternatives Considered

### Single normalized model, queried directly by order-query
- Pros: no projection to maintain; one schema.
- Cons: gives order-query joins across `orders`/`order_items`, and couples read
  query shape to write schema; no CQRS lesson.
- Rejected: the projection is the teachable artifact.

### Separate read datastore (e.g. Redis/Elasticsearch projection)
- Pros: closer to large-scale CQRS; fast denormalized reads.
- Cons: a second piece of infra to run, seed, and explain; more failure modes.
- Rejected: Postgres-only keeps boot to one command while still showing the
  write-model/read-model split.

## Consequences
- CQRS is demonstrated with one datastore — minimal infra, full concept.
- `order_view` being eventually consistent is itself a lesson (read-after-write).
- The projection update lives in the processor's write transaction (ADR-0006),
  so a successful write and its projection move together.
- order-query stays trivial: `SELECT` from one denormalized table.
