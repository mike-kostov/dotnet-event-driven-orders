# ADR-0002: Three isolated services, no shared code

## Status
Accepted

## Date
2026-06-05

## Context
The system models an order lifecycle with a write path (commands → Kafka →
processing → Postgres) and a read path (HTTP → Postgres). We must decide how to
partition this into deployable units, and whether they share code.

## Decision
Three independently deployable services, each its own project, `Dockerfile`, and
image, with **no shared C# project** between them:

- **order-ingest** — HTTP in, Kafka out. No database.
- **order-processor** — Kafka in, state machine, Postgres write + projection.
- **order-query** — HTTP in, Postgres read. No Kafka.

The only cross-service coupling is the JSON message contract (ADR-0008), which is
documented, not shared as code. Each service owns its own DTOs.

## Alternatives Considered

### A single modular monolith
- Pros: simplest to build/run; no message contract to coordinate; shared types.
- Cons: hides the event-driven boundaries that are the entire point of the
  tutorial; no network isolation to demonstrate; no independent de/scaling.
- Rejected: the seams *are* the lesson.

### Three services sharing a `Contracts` / `Common` project
- Pros: DRY DTOs; compile-time contract safety.
- Cons: a shared project is a hidden coupling — a change rebuilds everyone, and it
  quietly lets domain logic leak across boundaries. Real event-driven systems
  often can't share code (different languages/teams).
- Rejected: duplication of small DTOs is cheaper than the coupling, and forces the
  lesson that the contract is the boundary. Mirrors the Go repo's per-service `go.mod`.

## Consequences
- Strong isolation is demonstrable: `order-query` literally has no Kafka client;
  `order-ingest` has no Npgsql.
- Each service builds and ships as its own container (ADR-0013).
- DTO duplication is accepted; the JSON contract (ADR-0008) is the source of truth.
- Integration is proven by tests against real infra (Testcontainers), not by
  shared types.
