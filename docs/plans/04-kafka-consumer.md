# Lesson 04 — Consuming from Kafka: order-processor

> Plan for authoring `lesson/04-kafka-consumer`. Builds on `lesson/03-kafka-producer`.
> Decisions: [ADR-0002](../adr/0002-three-isolated-services-no-shared-code.md),
> [ADR-0003](../adr/0003-raw-confluent-kafka-over-bus-abstraction.md).

## Objective

Build the second service, `order-processor`: a background worker that **consumes**
`OrderCommand` messages from `orders` using `Confluent.Kafka`. Introduce consumer
groups and offsets. No database yet — the processor deserializes and logs each
command. Persistence comes in lesson 05.

After this lesson a learner can: build a .NET worker service, run a consume loop,
explain consumer groups and offsets, and see messages flow producer → consumer.

## The "why" (for LESSON.md)

A producer is only half a pipe. The **consumer** reads from the log at its own
pace. A **consumer group** lets the broker track *how far this logical consumer has
read* via committed **offsets**, and lets partitions be shared across instances.
Understanding offsets now sets up the central reliability lesson later: *when* you
commit the offset decides whether you can lose or double-process messages (lesson
09). Here we keep it simple — consume and log — so the loop itself is clear before
we add a database and commit semantics.

## The slice this lesson builds

- `order-processor/` — its own worker project, `Dockerfile`, image (ADR-0002):
  - A hosted `BackgroundService` running a consume loop on `orders`, group
    `order-processor`.
  - Deserializes JSON `OrderCommand` (its own DTO copy) and logs it structured.
  - `GET /healthz` / `/readyz` (minimal host) so compose can health-gate it.
  - For now: auto-commit *or* a simple commit — explicitly flagged as
    "we'll revisit commit timing in lesson 09".
- `docker-compose.yml` — adds order-processor, gated on Kafka healthy.
- `Makefile` — `make lag` (show consumer-group lag).
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: order-processor worker skeleton
**Acceptance:** worker starts, exposes `/healthz`, connects to Kafka.
**Verify:** `make up`; `make ps` shows it healthy; logs show group join.
**Files:** `order-processor/OrderProcessor.csproj`, `Program.cs`, `Worker.cs`
**Scope:** S

### Task 2: Consume loop on `orders`
**Acceptance:**
- [ ] Each produced PLACE command is consumed and logged (deserialized fields).
- [ ] Graceful shutdown stops the loop cleanly (CancellationToken).
**Verify:** `curl` POST to order-ingest → processor logs the command.
**Files:** `order-processor/Kafka/*.cs`, `Worker.cs`
**Scope:** M

### Task 3: Dockerfile + compose wiring
**Acceptance:** processor runs as a container, health-gated on Kafka.
**Verify:** full `make up` → end-to-end POST visible in processor logs; `make lag`
shows lag → 0.
**Files:** `order-processor/Dockerfile`, `docker-compose.yml`
**Scope:** S

### Task 4: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — consumers, consumer groups, offsets; foreshadow commit-timing.
2. **Concepts** — `BackgroundService`/worker; consume loop; group & offset; lag;
   per-service DTO duplication (ADR-0002 / ADR-0008).
3. **Do this** — `make up`; POST orders; watch processor logs; `make lag`.
4. **Inspect** — read the consume loop; find where the offset is committed.
5. **Your turn** — stop the processor, POST more orders, restart, watch it catch up.
6. **You're done when** — checklist.
7. **Next** — lesson 05 persists what we consume (Postgres + Dapper + DbUp).

## Checkpoint / done criteria
- [ ] Producer → consumer flow works end-to-end via `make up`.
- [ ] Learner can explain consumer group, offset, and lag.
- [ ] Commit timing is acknowledged as "revisited in lesson 09".

## Dependencies & next
- **Depends on:** lesson 03 (messages on `orders`).
- **Feeds:** lesson 05 (persistence).

## Risks / open questions
- Commit strategy shown here: simplest possible, with a clear comment that lesson
  09 replaces it with commit-after-write. Avoid teaching a habit we then undo —
  phrase it as "temporary, see lesson 09".
