# ADR-0006: At-least-once delivery, manual commit, idempotent writes

## Status
Accepted

## Date
2026-06-05

## Context
`order-processor` consumes commands from Kafka and writes to Postgres. If it
crashes between writing and acknowledging, or a message is redelivered, we must
not lose orders or double-apply them. We need to choose delivery semantics and an
idempotency strategy.

## Decision
**At-least-once with manual offset commit, made safe by idempotent writes:**

1. Consume a message **without auto-commit**.
2. Process it: run the state machine, write the event + write model + projection
   in one Postgres transaction.
3. **Commit the Kafka offset only after** the transaction succeeds (or after the
   message is dead-lettered, ADR-0007).
4. Writes are idempotent: `order_events` keys on `event_id` with
   `INSERT ... ON CONFLICT (event_id) DO NOTHING`, so a redelivered or replayed
   event produces exactly one row and no double state transition.

This yields effectively-once *effects* on the database from at-least-once delivery.

## Alternatives Considered

### Auto-commit / at-most-once
- Pros: simplest config.
- Cons: a crash after commit but before the DB write loses the order — violates
  the "no loss under crash" success criterion.
- Rejected: data loss is unacceptable and untestable-as-correct.

### Exactly-once (Kafka transactions / EOS)
- Pros: no idempotency handling needed in theory.
- Cons: Kafka EOS spans only Kafka↔Kafka; the write is to Postgres, so we'd still
  need a transactional outbox or 2PC. Heavy machinery to explain.
- Rejected: at-least-once + idempotent writes is the industry-standard, teachable
  pattern and is enough here.

## Consequences
- The commit-after-write ordering is explicit code learners can read and test.
- An integration test proves "same event twice → one row".
- A crash test proves `processed + dlq == produced` with lag returning to ~0.
- `event_id` is load-bearing — it must be unique per command and stable across
  retries/replay.
