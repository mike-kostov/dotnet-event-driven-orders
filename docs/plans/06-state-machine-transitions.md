# Lesson 06 — The state machine & client-driven transitions

> Plan for authoring `lesson/06-state-machine-transitions`. Builds on `lesson/05-persistence-cqrs`.
> Decisions: [ADR-0011](../adr/0011-client-driven-transitions.md),
> [ADR-0005](../adr/0005-cqrs-with-postgres-only.md).

## Objective

Bring the order **lifecycle** to life: add the transition endpoints to
order-ingest, and a pure **state machine** in order-processor that validates each
command against the order's persisted state, applies legal transitions, and
identifies illegal ones (which are dead-lettered properly in lesson 09).

After this lesson a learner can: model a state machine as pure domain logic, drive
an order through `PLACED → … → DELIVERED`, and explain why legality is decided on
the processor, not the ingest, side.

## The "why" (for LESSON.md)

An order has a lifecycle with rules: you can't DELIVER before DISPATCH; you can't
CANCEL after dispatch. Those rules are **domain logic** and belong in one place,
expressed clearly and tested exhaustively. order-ingest can't enforce them — it has
no state (ADR-0002/0011) — so it emits transition commands blindly and the
**processor** validates against persisted state. This is the heart of
client-driven transitions: the client requests, the system decides, asynchronously.

## The slice this lesson builds

- `order-ingest`: transition endpoints
  `POST /orders/{id}/{confirm|prepare|dispatch|deliver|cancel}` → emit the matching
  `OrderCommand`, return `202` (no legality check).
- `order-processor`: a pure `OrderStateMachine` (legal transition table); the
  worker loads current state, asks the state machine, and on a legal transition
  updates `orders`, appends `order_events`, and updates `order_view` (one txn).
  Illegal transitions are logged + skipped for now, with a clear "lesson 09 routes
  these to the DLQ" marker.
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: State machine (pure domain)
**Acceptance:** a function/type mapping `(currentState, commandType) → legal?` for
the full lifecycle incl. CANCEL-before-DISPATCH rule. No I/O.
**Verify:** exercised by a quick local check (full unit tests in lesson 08).
**Files:** `order-processor/Domain/OrderStateMachine.cs`
**Scope:** S

### Task 2: Transition endpoints on order-ingest
**Acceptance:** each transition endpoint emits the right `OrderCommand` and returns `202`.
**Verify:** `curl` each transition → message on `orders` (`make consume`).
**Files:** `order-ingest/Program.cs`
**Scope:** S

### Task 3: Apply transitions in the processor
**Acceptance:**
- [ ] Legal transition updates state + event log + projection (one txn).
- [ ] Illegal transition is detected and skipped/logged (DLQ deferred to L09).
**Verify:** drive `confirm→prepare→dispatch→deliver`; `make psql` shows state
advance; an illegal command (e.g. deliver before dispatch) does not corrupt state.
**Files:** `order-processor/Worker.cs`, `order-processor/Store/*.cs`
**Scope:** M

### Task 4: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — lifecycles as rules; where domain logic lives; client-driven decisions.
2. **Concepts** — state machine; pure functions; legal/illegal transitions;
   eventual consistency (the `202` you saw in L02 now visibly resolves).
3. **Do this** — place an order; drive it through every transition; try an illegal one.
4. **Inspect** — read the transition table; trace a command from ingest → applied state.
5. **Your turn** — add/justify a rule (e.g. allow CANCEL only before DISPATCH) and test by hand.
6. **You're done when** — checklist.
7. **Next** — lesson 07 lets you *query* the order's state over HTTP.

## Checkpoint / done criteria
- [ ] Full happy-path lifecycle works end-to-end.
- [ ] Illegal transitions don't corrupt state (proper DLQ routing in L09).
- [ ] State machine is isolated, pure, ready to unit-test in L08.

## Dependencies & next
- **Depends on:** lesson 05 (persisted state to validate against).
- **Feeds:** lesson 07 (query), lesson 08 (state machine tests), lesson 09 (DLQ).

## Risks / open questions
- Keep the state machine free of persistence concerns so lesson 08 can test it with
  zero I/O — this is the highest-value unit-test target in the repo.
