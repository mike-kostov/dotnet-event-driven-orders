# dotnet-event-driven-orders

A hands-on tutorial that builds an **event-driven order system** in .NET — an
order flows in over HTTP, is published to Kafka, processed asynchronously through
a state machine, and persisted to PostgreSQL for querying. Built for people new to
backend (including curious front-end developers), it teaches the senior parts
deliberately: **CQRS, idempotency, dead-lettering, eventual consistency, and
network isolation** — one lesson at a time.

## What it looks like

```
                POST /orders                       consume (group: order-processor)
  client ─────▶ order-ingest ──produce──▶ ( orders ) ─────────────▶ order-processor
  POST /orders/{id}/{transition}         3 partitions                  │ state machine
                  │ key = order_id                          ┌──────────┴───────────┐
                  ▼ POST /admin/replay                  legal? apply          illegal/poison
            (drain orders.DLT) ──▶ ( orders )               ▼                      ▼
                                                        Postgres              ( orders.DLT )
  client ── GET /orders[/{id}] ─────────────────▶ order-query ◀── read ── (order_view)
```

Three isolated services (ADR-0002): **order-ingest** (HTTP→Kafka, no DB),
**order-processor** (Kafka→Postgres, the state machine + reliability),
**order-query** (Postgres→HTTP, no Kafka). See [`docs/architecture.md`](docs/architecture.md)
and the decisions in [`docs/adr/`](docs/adr).

## How this tutorial works

This is **tutorial-first** (ADR-0012):

- **`main`** is the *foundation* — docs only, no service code. You're reading it.
- **`lesson/01` … `lesson/10`** are *start states*: each has the previous lessons
  complete plus this lesson's `LESSON.md` and `TODO(you)` markers to fill in.
- **`final`** is the *finished system* — the reachable end state.

**Start here:** `git checkout lesson/01-tooling` and open its `LESSON.md`.
**Just want to run it:** `git checkout final`, then `make up`.
**Read the journey:** walk `lesson/01 → … → lesson/10 → final`.

### Lessons

| # | Branch | You build |
|---|--------|-----------|
| 01 | `lesson/01-tooling` | Docker, Compose, Make; containerize a tiny app |
| 02 | `lesson/02-order-ingest-api` | order-ingest HTTP API (Minimal APIs, `202`) |
| 03 | `lesson/03-kafka-producer` | produce `OrderCommand` to Kafka |
| 04 | `lesson/04-kafka-consumer` | order-processor consumes |
| 05 | `lesson/05-persistence-cqrs` | Postgres write model + projection (DbUp, Dapper) |
| 06 | `lesson/06-state-machine-transitions` | the order lifecycle state machine |
| 07 | `lesson/07-order-query` | the read-side API |
| 08 | `lesson/08-testing` | xUnit unit tests |
| 09 | `lesson/09-reliability-dlq-replay` | at-least-once, idempotency, DLQ, replay |
| 10 | `lesson/10-observability-polish` | structured logs, health, OpenTelemetry |

## Quick start (on `final`)

```bash
cp .env.example .env          # ports/creds; change a *_PORT if one is taken
make up                       # kafka, postgres, migrate, the 3 services
make seed                     # place an order, drive it to DELIVERED, query it
```

## Commands

| Command | What it does |
|---------|--------------|
| `make up` / `make down` | start / reset the stack |
| `make ps` / `make logs` | status / tail logs |
| `make seed` | place → drive to DELIVERED → query |
| `make psql` | open a SQL shell |
| `make topics` / `make consume` | inspect Kafka |
| `make lag` | consumer-group lag |
| `make dlq` / `make replay` | inspect / drain the dead-letter topic |
| `make test` | run unit tests (in an SDK container) |

## Intentionally out of scope

Kept out to stay focused (each is a fine follow-up): protobuf + Schema Registry
(we use JSON, ADR-0008), a message-bus abstraction like MassTransit (we use raw
`Confluent.Kafka`, ADR-0003), .NET Aspire (compose only, ADR-0013), cloud
deployment, Kubernetes, and a frontend.

## Provenance & AI disclosure

`skills/`, `agents/`, and `references/` are vendored from
[addyosmani/agent-skills](https://github.com/addyosmani/agent-skills) (MIT) — see
[`ATTRIBUTION.md`](ATTRIBUTION.md). This repository was built with AI assistance.
