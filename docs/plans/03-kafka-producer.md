# Lesson 03 — Producing to Kafka

> Plan for authoring `lesson/03-kafka-producer`. Builds on `lesson/02-order-ingest-api`.
> Decisions: [ADR-0003](../adr/0003-raw-confluent-kafka-over-bus-abstraction.md),
> [ADR-0008](../adr/0008-json-message-contract-per-service-dtos.md).

## Objective

Make `order-ingest` actually **produce** an `OrderCommand` (PLACE) to Kafka using
`Confluent.Kafka` directly. Introduce Kafka's core concepts: topics, partitions,
keys, and why we key by `order_id`.

After this lesson a learner can: configure a Kafka producer, serialize a message
to JSON, choose a partition key, and see the message land on a topic.

## The "why" (for LESSON.md)

The front door now writes to the system's backbone. Kafka is an append-only log
split into **partitions**; the **key** decides which partition a message lands on,
and ordering is guaranteed only *within* a partition. We key by `order_id` so all
commands for one order are processed in order (this matters the moment transitions
arrive in lesson 06). We use the raw `Confluent.Kafka` client, not a bus
abstraction, so these mechanics stay visible (ADR-0003). Messages are JSON so you
can read them with your eyes (ADR-0008).

## The slice this lesson builds

- `topic-init` one-shot container in compose: creates `orders` (3 partitions);
  topic auto-creation disabled.
- `order-ingest`: a Kafka producer; `POST /orders` now serializes an
  `OrderCommand` (PLACE) and produces it to `orders` keyed by `order_id`, then
  returns `202`. `event_id` generated per command (idempotency key, used later).
- `OrderCommand` DTO + `System.Text.Json` serialization, owned by order-ingest.
- `Makefile`: `make topics` (list/describe topics), `make consume` (tail the
  `orders` topic via a console consumer for inspection).
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: topic-init container
**Acceptance:** `orders` exists with 3 partitions after `make up`; auto-create off.
**Verify:** `make topics` shows `orders` / 3 partitions.
**Files:** `docker-compose.yml`, `scripts/topic-init.*`
**Scope:** S

### Task 2: Kafka producer in order-ingest
**Acceptance:** producer configured from env (broker `:9092`); graceful flush on shutdown.
**Verify:** service starts; logs broker connection.
**Files:** `order-ingest/Kafka/*.cs`, `order-ingest/Program.cs`, `.env.example`
**Scope:** S

### Task 3: Produce OrderCommand(PLACE) on POST /orders
**Acceptance:**
- [ ] Valid POST → message on `orders`, key = `order_id`, JSON value with
      `event_id`, `order_id`, `type=PLACE`, items, total.
- [ ] Still returns `202`; invalid still `400` and produces nothing.
**Verify:** `make consume` shows the produced JSON after a `curl` POST.
**Files:** `order-ingest/Program.cs`, `order-ingest/Contracts/OrderCommand.cs`
**Scope:** M

### Task 4: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — Kafka as a partitioned log; keys & ordering; why `order_id` as key.
2. **Concepts** — topic, partition, key, producer; raw client vs bus (ADR-0003);
   JSON contract (ADR-0008); the role of `event_id`.
3. **Do this** — `make up`; `curl` POST; `make consume` to watch it arrive; inspect
   partitions with `make topics`.
4. **Inspect** — read the producer setup; note the key and serialization.
5. **Your turn** — POST several orders and observe partition spread by key.
6. **You're done when** — checklist.
7. **Next** — lesson 04 builds order-processor to *consume* these messages.

## Checkpoint / done criteria
- [ ] `orders` topic has 3 partitions; auto-create disabled.
- [ ] POST /orders produces a correctly-keyed JSON OrderCommand.
- [ ] `make consume` shows messages; learner can explain key→partition→ordering.

## Dependencies & next
- **Depends on:** lesson 02 (the HTTP endpoint) + lesson 01 (Kafka infra).
- **Feeds:** lesson 04 (consumer).

## Risks / open questions
- Whether `make consume` uses the bundled Kafka console consumer (no extra dep) vs
  a tiny .NET tool. *Leaning console consumer in the Kafka container — zero new code.*
