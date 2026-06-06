# ADR-0011: Client-driven state transitions

## Status
Accepted

## Date
2026-06-05

## Context
An order moves through `PLACED → CONFIRMED → PREPARING → DISPATCHED → DELIVERED`
(or `CANCELLED` before dispatch). Something must decide *when* each transition
happens and *who* validates legality. `order-ingest` has no database (ADR-0002),
so it cannot know an order's current state.

## Decision
Transitions are **client-driven** and validated asynchronously:

- A client sends one command per transition: `POST /orders` (PLACE), then
  `POST /orders/{id}/{confirm|prepare|dispatch|deliver|cancel}`.
- `order-ingest` emits the corresponding `OrderCommand` to Kafka **without
  checking legality** (it has no state to check against). Transition endpoints
  return **`202 Accepted`** — the command is accepted for processing, not
  confirmed as applied.
- `order-processor` validates each command against the order's **persisted**
  state via the state machine, applies legal transitions, and **dead-letters
  illegal ones** (ADR-0007).

The state machine is pure domain logic, unit-tested in isolation.

## Alternatives Considered

### Validate transitions synchronously in order-ingest
- Pros: caller gets an immediate legal/illegal answer.
- Cons: requires order-ingest to read order state — breaking its no-database
  isolation (ADR-0002) and creating a read dependency on the write side.
- Rejected: violates isolation; couples ingest to storage.

### Server-driven transitions (a timer/orchestrator advances orders)
- Pros: models real fulfillment automation.
- Cons: hides the command→event→state mechanic behind a scheduler; harder to
  drive and observe in a tutorial.
- Rejected: client-driven commands are explicit and easy to exercise with `curl`.

## Consequences
- `202` semantics teach eventual consistency: accepted ≠ applied; confirm via
  `GET /orders/{id}` (served from the projection).
- The state machine is the highest-value unit test target (legal/illegal matrix).
- Illegal transitions are a first-class, observable path into the DLQ — useful for
  the poison/replay lesson.
- order-ingest stays database-free.
