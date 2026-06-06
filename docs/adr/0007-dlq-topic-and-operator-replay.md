# ADR-0007: Dead-letter topic and operator-triggered replay

## Status
Accepted

## Date
2026-06-05

## Context
Some messages can never succeed on the normal path: business-invalid commands
(e.g. DELIVER before DISPATCH) or payloads that exhaust transient retries. If such
a "poison" message blocks its partition, all later messages for other orders on
that partition stall. We need a way to quarantine failures and recover later.

## Decision
A **dead-letter topic `orders.DLT`** plus **operator-triggered replay**:

- On a permanent error (business-invalid) or exhausted bounded retries
  (transient), `order-processor` publishes the original message — key and value
  bytes unchanged — to `orders.DLT` with diagnostic headers (`x-error`,
  `x-attempts`, `x-original-topic/partition/offset`, `x-failed-at`), then commits
  the offset so the partition keeps moving.
- **Replay** is explicit and operator-run: `POST /admin/replay` on `order-ingest`
  drains `orders.DLT` and republishes to `orders`. Idempotency (ADR-0006) makes
  reprocessing safe; already-applied events are no-ops.

`orders` has 3 partitions (ordering per `order_id`); `orders.DLT` has 1.

## Alternatives Considered

### Block/retry forever on failure
- Pros: nothing is ever "set aside".
- Cons: a single poison message halts a partition indefinitely — a classic
  outage. Unacceptable.
- Rejected: violates the "partition never stalls" success criterion.

### Automatic/scheduled replay
- Pros: no human in the loop.
- Cons: replaying before the root cause is fixed just re-poisons; hides failures.
- Rejected: replay is a deliberate operator action after a fix — and a better lesson.

### Discard failed messages
- Pros: trivial.
- Cons: silent data loss; no audit trail.
- Rejected: the DLT is the audit trail and the recovery path.

## Consequences
- Poison messages are quarantined with enough context to diagnose them.
- The partition keeps flowing for healthy orders.
- Replay is a demonstrable recovery story (`make replay`), not a hidden mechanism.
- Bounded retry counts and the retry/permanent classification are explicit in code.
