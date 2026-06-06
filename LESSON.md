# Lesson 09 — Reliability: at-least-once, idempotency, DLQ & replay

> **You are on** `lesson/09-reliability-dlq-replay`. The system works (lessons
> 1–8). Now you make it **survive crashes and poison messages** — the senior core
> the whole tutorial has been building toward. Fill in the `TODO(you)` and check
> against `lesson/10-observability-polish`.

---

## 1. Why this lesson exists

Real systems crash mid-work and receive bad data. Two failures must never happen:
**losing** an order, and **double-applying** one. The fix is a pair (ADR-0006):

- Commit the Kafka offset **only after** the Postgres write succeeds. A crash then
  just *redelivers* the message — at-least-once.
- Make writes **idempotent** on `event_id` (`ON CONFLICT DO NOTHING`), so a
  redelivery changes nothing.

And for messages that can *never* succeed (illegal transitions, bad payloads), we
**quarantine** them in a dead-letter topic so one poison message can't stall a
partition (ADR-0007) — with operator-triggered **replay** to recover after a fix.

---

## 2. Concepts

- **At-least-once** — every message is processed *at least* once; combined with
  idempotency, the *effect* is exactly-once.
- **Manual commit** — you decide when the offset advances. Commit *after* the
  write (or after dead-lettering), never before.
- **Idempotency key** — `event_id`. `ON CONFLICT (event_id) DO NOTHING` returns 0
  rows on a duplicate; skip the rest of the work.
- **Dead-letter topic** — `orders.DLT`. Failed messages go here (original bytes +
  diagnostic headers), then the offset is committed so the partition keeps moving.
- **Replay** — `POST /admin/replay` drains `orders.DLT` back into `orders`,
  bounded by a high-watermark snapshot. Safe because of idempotency.

---

## 3. Do this — three TODOs

**9.1 — manual commit** (`order-processor/ConsumerService.cs`):
set `EnableAutoCommit = false`, and at the bottom of the loop add
`_consumer.Commit(result);` so the offset advances only after handling.

**9.2 — idempotency** (`order-processor/Store/OrderStore.cs`):
in both `SavePlacedOrderAsync` and `ApplyTransitionAsync`, capture the
`InsertEvent` result and early-return on a duplicate:
```csharp
var inserted = await conn.ExecuteAsync(InsertEvent, new { cmd.EventId, cmd.OrderId, cmd.Type, cmd.IssuedAt }, tx);
if (inserted == 0) { await tx.CommitAsync(); return; }   // already processed
```
(remove the plain `InsertEvent` line that follows).

**9.3 — dead-letter failures** (`order-processor/ConsumerService.cs`):
inject `DeadLetter` and, in the illegal-transition branch and the `catch`, send to
the DLT instead of only logging:
```csharp
await _deadLetter.SendAsync(result, "illegal transition ...");   // and in catch: ex.Message
```

---

## 4. Verify the reliability properties

```bash
cp .env.example .env            # if needed
make up                         # topic-init now also creates orders.DLT
```

**Poison quarantine + partition keeps moving:**
```bash
# place a good order, then send an illegal transition to a different order
OID=$(curl -s -X POST localhost:8080/orders -H 'content-type: application/json' \
  -d '{"customer":"a","items":[{"sku":"X","quantity":1,"unitPriceCents":100}]}' | sed 's/.*"orderId":"//;s/".*//')
curl -s -o /dev/null -X POST "localhost:8080/orders/$OID/deliver"   # illegal (PLACED -> DELIVER)
make dlq                        # the illegal command is in orders.DLT, with headers
```

**Replay:**
```bash
make replay                     # {"replayed":N} — drains orders.DLT back into orders
```

**Idempotency** (redelivery → no duplicate): the same `event_id` delivered twice
results in exactly one `order_events` row (`make psql` →
`SELECT event_id, count(*) FROM order_events GROUP BY event_id HAVING count(*) > 1;`
returns nothing).

---

## 5. Your turn — crash test

Under a little load, kill the processor and restart it; confirm nothing is lost:

```bash
docker compose kill order-processor && docker compose up -d order-processor
make lag                        # catches back up to ~0; no orders lost
```

---

## 6. You're done when

- [ ] Offsets commit only after the write (`EnableAutoCommit = false` + `Commit`).
- [ ] A duplicate `event_id` yields exactly one `order_events` row.
- [ ] An illegal command lands in `orders.DLT` (with headers); good orders on the
      same partition still process.
- [ ] `make replay` drains the DLT; reprocessing is a no-op for already-applied events.
- [ ] You can explain why commit-after-write + idempotency = effectively-once.

Check your work:

```bash
git diff lesson/10-observability-polish -- order-processor
```

---

## 7. Next

In **lesson 10** — the finish line — you make the system observable (structured
logs, health/readiness, OpenTelemetry) and polish the repo. Completing it produces
the **`final`** branch. Check out `lesson/10-observability-polish`.
