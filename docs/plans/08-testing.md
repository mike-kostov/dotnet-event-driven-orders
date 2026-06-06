# Lesson 08 — Testing: units + Testcontainers integration

> Plan for authoring `lesson/08-testing`. Builds on `lesson/07-order-query`.
> Decisions: SPEC Testing Strategy; mirrors the user's `*.UnitTests` /
> `*.IntegrationTests` convention.

## Objective

Prove the system works with automated tests: fast **xUnit unit tests** for the pure
state machine (legal/illegal matrix), and a few high-value **Testcontainers**
integration tests against real Kafka + Postgres (produce → consume → persist, and
idempotency).

After this lesson a learner can: write `[Theory]`-driven unit tests, spin real
infra in tests with Testcontainers, and explain the test pyramid.

## The "why" (for LESSON.md)

"Seems right" never closes the loop — a task is done when there's *evidence*. The
state machine is pure, so it's cheap to test exhaustively: every (state, command)
pair. The risky, I/O-heavy paths (Kafka + Postgres) get a few integration tests
against the *real* dependencies — not mocks — using Testcontainers, because the
bugs that matter live in the seams. Most tests are fast units; a few are slow and
high-value. That shape is the test pyramid.

## The slice this lesson builds

- `order-processor/tests/OrderProcessor.UnitTests/` — state machine matrix +
  validation; `[Theory]` data-driven, no I/O.
- `order-processor/tests/OrderProcessor.IntegrationTests/` — Testcontainers Kafka +
  Postgres: produce a PLACE → assert persisted rows; deliver the same `event_id`
  twice → assert exactly one row (idempotency preview of L09).
- `Makefile` — `make test` runs all; `make test-unit` / `make test-int` split.
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: Unit tests for the state machine
**Acceptance:** every legal transition passes; representative illegal ones are
rejected; CANCEL-before/after-DISPATCH covered.
**Verify:** `make test-unit` green.
**Files:** `.../OrderProcessor.UnitTests/*`
**Scope:** M

### Task 2: Testcontainers integration test — produce→consume→persist
**Acceptance:** spins Kafka + Postgres; runs the processor path; asserts write
model + projection rows.
**Verify:** `make test-int` green (may be slow; document it).
**Files:** `.../OrderProcessor.IntegrationTests/*`
**Scope:** L

### Task 3: Idempotency test
**Acceptance:** same `event_id` delivered twice → exactly one `order_events` row,
state applied once.
**Verify:** `make test-int` includes and passes this case.
**Files:** `.../OrderProcessor.IntegrationTests/*`
**Scope:** M

### Task 4: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — evidence over "seems right"; the test pyramid; real infra vs mocks.
2. **Concepts** — xUnit `[Theory]`; Testcontainers lifecycle; arrange/act/assert
   across async Kafka.
3. **Do this** — `make test`; read a failing test you introduce, then fix it.
4. **Inspect** — see how the integration test boots and tears down containers.
5. **Your turn** — add one unit case for a transition rule.
6. **You're done when** — checklist.
7. **Next** — lesson 09 makes the system *resilient* (idempotency, DLQ, replay).

## Checkpoint / done criteria
- [ ] `make test` runs units + integration; all green.
- [ ] State machine matrix covered; idempotency demonstrated by a test.
- [ ] Learner can explain why integration tests use real Kafka/Postgres.

## Dependencies & next
- **Depends on:** lessons 05–07 (something to test).
- **Feeds:** lesson 09 (which adds the reliability behaviors these tests then cover).

## Risks / open questions
- Integration-test runtime/flakiness — keep them few; ensure deterministic waits
  (poll for the projected row, no fixed sleeps). Note Docker is required to run them.
