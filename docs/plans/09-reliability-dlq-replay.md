# Lesson 09 — Reliability: at-least-once, idempotency, DLQ & replay

> Plan for authoring `lesson/09-reliability-dlq-replay`. Builds on `lesson/08-testing`.
> Decisions: [ADR-0006](../adr/0006-at-least-once-manual-commit-idempotency.md),
> [ADR-0007](../adr/0007-dlq-topic-and-operator-replay.md).

## Objective

Make the system survive crashes and poison messages. Switch to **manual
offset commit after the write**, make writes **idempotent**, add the
**dead-letter topic** with bounded retry, and an operator **replay** endpoint.
This is the senior core the whole tutorial has been building toward.

After this lesson a learner can: explain and implement at-least-once delivery,
reason about commit timing, build a DLQ with diagnostic headers, and run a safe
replay.

## The "why" (for LESSON.md)

Real systems crash mid-work and receive bad data. Two failures must never happen:
**losing** an order, and **double-applying** one. The fix is a pair: commit the
Kafka offset **only after** the Postgres write succeeds (so a crash just redelivers
— at-least-once, ADR-0006), and make writes **idempotent** on `event_id` (so
redelivery changes nothing — `INSERT … ON CONFLICT DO NOTHING`). For messages that
can *never* succeed (illegal transitions, exhausted retries), we quarantine them in
`orders.DLT` so one poison message can't stall a partition (ADR-0007), and provide
an operator-triggered **replay** to recover after a fix.

## The slice this lesson builds

- `order-processor`: commit-after-write ordering (replaces the L04 placeholder);
  idempotent SQL (`ON CONFLICT (event_id) DO NOTHING`); bounded in-process retry
  with backoff for transient errors; on permanent error / exhausted retries →
  publish original key+value to `orders.DLT` with headers (`x-error`, `x-attempts`,
  `x-original-topic/partition/offset`, `x-failed-at`), then commit to unblock.
- `topic-init`: add `orders.DLT` (1 partition).
- `order-ingest`: `POST /admin/replay` — drain `orders.DLT` (group
  `order-ingest-replay`) and republish to `orders`, bounded by a high-watermark snapshot.
- `Makefile`: `make replay`; a `make chaos`-style helper to inject illegal commands
  and to kill/restart the processor under load.
- Tests: extend integration tests — crash → `processed + dlq == produced`;
  poison → quarantined, partition flows; replay → now-valid applied, dupes no-op.
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: Manual commit-after-write + idempotent writes
**Acceptance:** offset committed only after the txn; same `event_id` twice → one row.
**Verify:** integration test (crash + duplicate) green; `make lag` returns to ~0 after restart.
**Files:** `order-processor/Worker.cs`, `Store/*.cs`
**Scope:** M

### Task 2: Bounded retry + DLQ producer
**Acceptance:** transient errors retried N× w/ backoff; permanent/exhausted →
`orders.DLT` with headers, then offset committed.
**Verify:** inject an illegal transition → lands in `orders.DLT`; good orders unaffected.
**Files:** `order-processor/Kafka/DeadLetter.cs`, `Worker.cs`, `scripts/topic-init.*`
**Scope:** M

### Task 3: Replay endpoint
**Acceptance:** `POST /admin/replay` drains DLT → republishes to `orders`; bounded
by a snapshot so it doesn't chase its own tail; reprocessing is idempotent.
**Verify:** `make replay` after fixing a cause → now-valid messages apply; dupes no-op.
**Files:** `order-ingest/Program.cs`, `order-ingest/Kafka/*.cs`
**Scope:** M

### Task 4: Resilience tests + chaos helper
**Acceptance:** crash test (`processed + dlq == produced`), poison-quarantine test,
replay test.
**Verify:** `make test-int` green; `make chaos` demonstrates it live.
**Files:** integration tests, `Makefile`, `scripts/chaos.*`
**Scope:** L

### Task 5: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — the two failures (loss, double-apply); commit timing; poison messages.
2. **Concepts** — at-least-once vs at-most/exactly-once (ADR-0006); idempotency key;
   DLQ + headers; bounded retry; operator replay (ADR-0007).
3. **Do this** — `make chaos`: inject poison + kill the processor under load; observe
   no loss, quarantine, partition still flowing; then `make replay`.
4. **Inspect** — find the exact commit line; read the DLQ headers; read replay bounds.
5. **Your turn** — change commit-before-write and watch a test fail (then revert).
6. **You're done when** — checklist (SPEC criteria 4–7).
7. **Next** — lesson 10 adds observability and the final polish.

## Checkpoint / done criteria
- [ ] No loss under crash; idempotent; poison quarantined; safe replay (SPEC 4–7).
- [ ] Learner can explain commit-after-write and idempotency from memory.

## Dependencies & next
- **Depends on:** lesson 08 (test harness to extend).
- **Feeds:** lesson 10 (observe these behaviors via metrics/logs).

## Risks / open questions
- Replay bounding (high-watermark snapshot) is subtle — document it carefully so
  replay doesn't loop on messages re-dead-lettered during the same run.
