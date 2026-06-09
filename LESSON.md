# Lesson 07 — The read side: order-query

> **You are on** `lesson/07-order-query`. Lesson 6 is complete here (orders move
> through their lifecycle). Now you build the **third and final service**,
> `order-query`, which serves the read projection over HTTP. Fill in the
> `TODO(you)` and check against `lesson/08-testing`.

---

## 1. Why this lesson exists

Reads and writes have different needs. The write side optimizes for correctness
(normalized, transactional); the read side optimizes for fast, simple queries —
served from the `order_view` projection the processor maintains. `order-query` is
deliberately tiny and has **no Kafka client at all** (ADR-0002): isolation you can
see. Because the projection lags the write model slightly, querying right after a
transition shows **eventual consistency** first-hand.

---

## 2. Concepts

- **Read model / projection** — `order_view` is denormalized (items embedded as
  JSONB) and shaped for the query, not the write.
- **Offset pagination** — `?limit=&offset=` with newest-first ordering.
- **Isolation** — order-query reads Postgres only. It has no producer, no
  consumer, no topic. Grep the project: zero Kafka.
- **Eventual consistency** — a transition is applied asynchronously, so a query a
  moment later may show the previous state briefly.

---

## 3. Do this — implement the read endpoints  ← main task

Open `order-query/Program.cs`. The connection and a `ToResponse` helper (which
parses the items JSON) are given. The SQL for each endpoint is written for you as
a `const`. Fill the two `TODO(you)` markers:

- **7.1 — `GET /orders/{id}`**: query one row with
  `QuerySingleOrDefaultAsync<OrderRow>`; return `404` if missing, else
  `Results.Ok(ToResponse(row))`.
- **7.2 — `GET /orders`**: query a page with `QueryAsync<OrderRow>` (clamp `limit`,
  guard `offset`); return `Results.Ok(rows.Select(ToResponse))`.

Remove each `Results.StatusCode(501)` placeholder.

---

## 4. Run it end-to-end

```bash
cp .env.example .env            # if needed
make up                         # now starts all three services
make seed                       # places an order, drives it to DELIVERED, queries it
```

`make seed` should print the order as JSON with `"state":"DELIVERED"` and its
items. Or do it by hand:

```bash
curl -s localhost:8081/orders | jq .            # list (newest first)
curl -s 'localhost:8081/orders?status=DELIVERED&limit=5' | jq .
curl -s localhost:8081/orders/<id> | jq .       # one order
```

> Different ports? `make seed` honors `HOST_INGEST` / `HOST_QUERY`, e.g.
> `HOST_INGEST=localhost:8088 HOST_QUERY=localhost:8089 make seed`.

---

## 5. Your turn — prove the isolation

Confirm order-query truly has no Kafka:

```bash
grep -ri kafka order-query/ || echo "no Kafka in order-query — read side is isolated"
```

Then add a filter of your own (e.g. by customer) end-to-end: extend the SQL +
query parameter, rebuild, and call it.

---

## 6. You're done when

- [ ] All three services run (`make up`); `make seed` shows an order reaching
      `DELIVERED` via the query API.
- [ ] `GET /orders/{id}` returns the order or `404`; `GET /orders` supports
      `status`, `limit`, `offset`.
- [ ] `grep kafka order-query/` finds nothing — the read side is isolated.
- [ ] You can explain CQRS + eventual consistency end-to-end.

Check your work:

```bash
git diff lesson/08-testing -- order-query/Program.cs
```

---

## 7. Next

The system works end-to-end. In **lesson 08** you prove it with tests — xUnit for
the state machine and Testcontainers for the real Kafka + Postgres path. Check out
`lesson/08-testing`.

<!-- NAV:START -->

---

## 🧭 Navigate

| ◀ Previous | 🔑 Solution to this lesson | Next ▶ |
|:---|:---:|---:|
| [Lesson 06 — State machine](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/lesson/06-state-machine-transitions) | [view the diff](https://github.com/mike-kostov/dotnet-event-driven-orders/compare/lesson/07-order-query...lesson/08-testing) | [Lesson 08 — Testing](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/lesson/08-testing) |

Or from the terminal: `make prev` · `make solution` · `make next` · `make goto LESSON=7`
*(`next`/`prev`/`goto` switch branches — commit or stash your edits first; they never discard your work.)*

<!-- NAV:END -->
