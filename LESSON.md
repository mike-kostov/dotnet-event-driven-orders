# Lesson 06 — The state machine & client-driven transitions

> **You are on** `lesson/06-state-machine-transitions`. Lesson 5 is complete here
> (PLACE persists to Postgres). Now you bring the order **lifecycle** to life.
> Fill in the `TODO(you)` and check against `lesson/07-order-query`.

---

## 1. Why this lesson exists

An order has rules: you can't DELIVER before DISPATCH; you can't CANCEL once it's
dispatched. Those rules are **domain logic** and belong in one place, expressed
clearly and tested exhaustively (lesson 8). order-ingest can't enforce them — it
has no state (ADR-0011) — so it emits transition commands blindly, and
**order-processor** validates each against the order's *persisted* state.

This is the heart of client-driven transitions: the client requests, the system
decides, asynchronously.

---

## 2. Concepts

- **State machine** — a pure function: given the current state and a command,
  return the next state, or "illegal." No database, no Kafka — just logic.
- **Client-driven transitions** — order-ingest produces `CONFIRM`/`PREPARE`/… 
  commands (already wired this lesson); the processor decides legality.
- **Illegal transitions** — for now we log and skip them. Lesson 9 routes them to
  a dead-letter topic.

The lifecycle:
```
PLACED → CONFIRMED → PREPARING → DISPATCHED → DELIVERED
   └──────────┴──────────┴── CANCELLED      (CANCEL only before DISPATCH)
```

---

## 3. Do this — implement the state machine  ← main task

Open `order-processor/OrderStateMachine.cs`. Fill `TODO(you) 6.1`: implement
`Next(currentState, commandType)` to return the resulting state for a legal
transition or `null` for an illegal one. A `switch` expression on
`(currentState, commandType)` reads cleanly (the exact arms are in the hint).

Everything else is wired for you: order-ingest produces the transition commands,
and the processor loads state → calls your `Next(...)` → applies legal transitions
(updating `orders` + `order_view` and appending to `order_events`) or logs illegal ones.

---

## 4. Run it and drive an order through its lifecycle

```bash
cp .env.example .env            # if needed
make up
```

Place an order, capture its id, and walk it forward:

```bash
OID=$(curl -s -X POST localhost:8080/orders -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[{"sku":"MARGHERITA","quantity":1,"unitPriceCents":1200}]}' \
  | sed 's/.*"orderId":"//;s/".*//')
echo "order: $OID"

for t in confirm prepare dispatch deliver; do
  curl -s -o /dev/null -X POST "localhost:8080/orders/$OID/$t"; sleep 1
done

make psql -- # then:  SELECT state FROM orders WHERE order_id = '$OID';   → DELIVERED
```

Now try an **illegal** transition and watch it get rejected (not applied):

```bash
OID2=$(curl -s -X POST localhost:8080/orders -H 'content-type: application/json' \
  -d '{"customer":"bob","items":[{"sku":"X","quantity":1,"unitPriceCents":100}]}' \
  | sed 's/.*"orderId":"//;s/".*//')
curl -s -o /dev/null -X POST "localhost:8080/orders/$OID2/deliver"   # DELIVER before DISPATCH
docker compose logs order-processor | grep -i illegal | tail -1       # logged + skipped
```

---

## 5. Your turn — defend a rule

Confirm the cancel rule: CANCEL works before DISPATCH but not after. Place two
orders; cancel one while PLACED (→ CANCELLED), and try to cancel another after
dispatching it (→ logged illegal, stays DISPATCHED). Verify with `make psql`.

---

## 6. You're done when

- [ ] A placed order walks `PLACED → CONFIRMED → PREPARING → DISPATCHED → DELIVERED`,
      observable in `orders.state`.
- [ ] An illegal transition (e.g. DELIVER before DISPATCH) is logged and **not** applied.
- [ ] CANCEL is accepted before DISPATCH and rejected after.
- [ ] The state machine has no I/O (it's a pure function) — ready to unit-test in lesson 8.

Check your work:

```bash
git diff lesson/07-order-query -- order-processor/OrderStateMachine.cs
```

---

## 7. Next

In **lesson 07** you build `order-query` — the read-side HTTP API that serves the
`order_view` projection. Check out `lesson/07-order-query`.
