# Lesson 03 — Producing to Kafka

> **You are on** `lesson/03-kafka-producer`. Lesson 2 is complete here
> (order-ingest accepts orders over HTTP). Now you make it **publish** each
> accepted order to Kafka. Fill in the `TODO(you)` and check against
> `lesson/04-kafka-consumer`.

---

## 1. Why this lesson exists

Right now `order-ingest` accepts an order and just logs it — the order goes
nowhere. In an event-driven system, the front door's real job is to put a
**message** onto a log that other services read. That log is **Kafka**.

After this lesson, every accepted order becomes an `OrderCommand` message on the
`orders` topic, ready for `order-processor` to consume in lesson 4.

---

## 2. Concepts

- **Topic** — a named, append-only log (ours is `orders`).
- **Partition** — a topic is split into partitions for scale. Ordering is
  guaranteed only *within* a partition.
- **Key** — each message has a key; the key decides its partition. We use
  `order_id` as the key so **all commands for one order share a partition** and
  stay in order (this matters in lesson 6 when transitions arrive).
- **Producer** — the client that writes messages. We use the raw `Confluent.Kafka`
  client, not a bus abstraction, so the mechanics stay visible (ADR-0003).
- **No auto-create** — the broker won't invent topics; `topic-init` creates
  `orders` with exactly 3 partitions on purpose.

Read `order-ingest/Contracts/OrderCommand.cs` — that's the JSON we publish.

---

## 3. Do this — produce the message  ← main task

Open `order-ingest/Program.cs`. The producer is already registered for you
(see the `AddSingleton<IProducer<...>>` block — it reads `KAFKA_BOOTSTRAP`).
Find `TODO(you) 3.6` in the `POST /orders` handler and:

1. **Make the handler async.** Change its signature to:
   ```csharp
   app.MapPost("/orders", async (PlaceOrderRequest request, IProducer<string, string> producer) =>
   ```
2. **Build, serialize, and produce** the command (the exact lines are in the TODO):
   build an `OrderCommand(... Type: "PLACE" ...)`, `JsonSerializer.Serialize` it,
   then `await producer.ProduceAsync("orders", new Message<string,string> { Key = orderId, Value = json });`

---

## 4. Run it and watch the message arrive

```bash
cp .env.example .env            # if needed
make up                         # starts infra, runs topic-init, builds order-ingest
make topics                     # should show `orders` with 3 partitions
```

In one terminal, tail the topic; in another, place an order:

```bash
# terminal A — watch the topic
make consume

# terminal B — place an order
curl -s -X POST localhost:8080/orders -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[{"sku":"MARGHERITA","quantity":1,"unitPriceCents":1200}]}'
```

Terminal A should print your JSON `OrderCommand`, prefixed by its key (the
order id). 🎉 You just produced to Kafka.

> Port already in use? Set `ORDER_INGEST_PORT` / `POSTGRES_PORT` / `KAFKA_PORT`
> in `.env` and `make up` again.

---

## 5. Your turn — see partitioning by key

Place several orders, then run `make consume` with keys visible (it already
shows them). Notice each order's commands carry the same key. Try
`make topics` and read the partition count. (Optional: produce two orders and
reason about which partition each could land on.)

---

## 6. You're done when

- [ ] `make topics` shows `orders` with **3 partitions**.
- [ ] A valid `POST /orders` makes a JSON `OrderCommand` appear via `make consume`,
      keyed by the order id.
- [ ] Invalid `POST /orders` still returns `400` and produces **nothing**.
- [ ] You can explain why we key by `order_id`.

Check your work:

```bash
git diff lesson/04-kafka-consumer -- order-ingest/Program.cs
```

---

## 7. Next

In **lesson 04** you build `order-processor` — a worker that **consumes** these
messages from Kafka. Check out `lesson/04-kafka-consumer`.
