# Lesson 08 — Testing: the state machine

> **You are on** `lesson/08-testing`. The whole system works (lessons 1–7). Now
> you prove a piece of it with **automated tests**. Fill in the `TODO(you)` and
> check against `lesson/09-reliability-dlq-replay`.

---

## 1. Why this lesson exists

"Seems right" never closes the loop — a task is done when there's *evidence*. The
cheapest, highest-value evidence is **unit tests** of pure logic. Our state
machine is pure (no database, no Kafka), so we can test every `(state, command)`
pair in milliseconds. That's the base of the **test pyramid**: lots of fast unit
tests, fewer slow integration tests.

> **Integration tests** (Testcontainers spinning real Kafka + Postgres) arrive in
> **lesson 9**, alongside the resilience behaviors worth integration-testing —
> idempotency, crash recovery, and replay. Testing those here would be premature:
> they don't exist yet.

---

## 2. Concepts

- **xUnit** — the test framework. `[Fact]` is one case; `[Theory]` + `[InlineData]`
  runs the same test body over many inputs.
- **Test pyramid** — many unit tests (pure, fast), fewer integration tests (real
  dependencies, slow), fewer still end-to-end.
- **No host tooling** — you don't need the .NET SDK installed. `make test` runs
  `dotnet test` inside an SDK container, mounting the source.

The tests live in `order-processor/tests/OrderProcessor.UnitTests/`.

---

## 3. Do this — run the tests, then extend them

```bash
make test
```

This pulls the SDK image (first time), restores, builds, and runs the suite. You
should see the given tests pass (legal transitions advance; illegal ones return
null).

Now open `order-processor/tests/OrderProcessor.UnitTests/OrderStateMachineTests.cs`
and fill `TODO(you) 8.1`: add a `[Theory]` proving CANCEL is **legal** from
`PLACED`, `CONFIRMED`, and `PREPARING` (each → `"CANCELLED"`). The exact code is in
the comment. Run `make test` again — your new cases should pass too.

---

## 4. Watch a test fail (then fix it)

Testing is most convincing when you see red turn green. Temporarily break a case —
e.g. change an expected value to something wrong — and run `make test` to see it
fail with a clear message. Then revert it. (Don't commit the broken version.)

---

## 5. Your turn — cover one more rule

Add a case asserting that CANCEL is **illegal** once `DISPATCHED` or `DELIVERED`
(returns null). Re-run `make test`.

---

## 6. You're done when

- [ ] `make test` is green.
- [ ] Your `TODO(you) 8.1` cancel-legal `[Theory]` is present and passing.
- [ ] You added at least one cancel-illegal case.
- [ ] You can explain the test pyramid and why the state machine is the easiest,
      highest-value thing to unit-test.

Check your work:

```bash
git diff lesson/09-reliability-dlq-replay -- order-processor/tests
```

---

## 7. Next

In **lesson 09** — the big one — you make the system survive crashes and poison
messages: at-least-once delivery, idempotency, a dead-letter topic, and replay,
with Testcontainers integration tests to prove it. Check out
`lesson/09-reliability-dlq-replay`.
