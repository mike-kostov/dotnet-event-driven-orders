# Lesson 04 — Consuming from Kafka: order-processor

> **You are on** `lesson/04-kafka-consumer`. Lesson 3 is complete here
> (order-ingest produces to Kafka). Now you build the **second service**,
> `order-processor`, which **consumes** those messages. Fill in the `TODO(you)`
> and check against `lesson/05-persistence-cqrs`.

---

## 1. Why this lesson exists

A producer alone is half a pipe. Something has to **read** the log. That's
`order-processor` — a background worker that consumes `OrderCommand` messages and
(later) acts on them. This lesson keeps it simple: consume each message and log
it. No database yet.

The idea to internalize: **consumer groups and offsets**. Kafka remembers how far
a *group* has read via committed **offsets**. *When* you commit is the crux of
the reliability lesson later (lesson 9) — so we meet offsets now, simply.

---

## 2. Concepts

- **Consumer** — reads messages from a topic at its own pace.
- **Consumer group** — `order-processor`. Kafka tracks read-progress (offsets)
  per group, and can spread partitions across multiple instances of a group.
- **Offset** — the position in a partition. "Committed offset" = "the group has
  processed up to here."
- **Auto-commit (for now)** — offsets commit automatically on a timer. Simple,
  but it can lose/duplicate work on a crash — **lesson 9** replaces it with a
  manual commit *after* the database write.
- **BackgroundService** — .NET's hosted long-running worker; our consume loop
  lives in `order-processor/ConsumerService.cs`.

`order-processor` is its own service with its **own** copy of the `OrderCommand`
DTO (no shared project — ADR-0002).

---

## 3. Do this — fill in the consume loop  ← main task

Open `order-processor/ConsumerService.cs`. The consumer is configured for you
(group `order-processor`, reads from the earliest offset). Find `TODO(you) 4.1`
inside the loop and:

1. **Deserialize** the message JSON into an `OrderCommand`:
   `var cmd = JsonSerializer.Deserialize<OrderCommand>(result.Message.Value);`
2. **Log** what you got and where it came from:
   ```csharp
   _logger.LogInformation(
       "Consumed {Type} for order {OrderId} (partition {Partition}, offset {Offset})",
       cmd?.Type, cmd?.OrderId, result.Partition.Value, result.Offset.Value);
   ```

---

## 4. Run it and watch end-to-end flow

```bash
cp .env.example .env            # if needed
make up                         # infra + topic-init + order-ingest + order-processor
docker compose ps               # all up
```

Place an order, then watch the processor consume it:

```bash
curl -s -X POST localhost:8080/orders -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[{"sku":"MARGHERITA","quantity":1,"unitPriceCents":1200}]}'

docker compose logs order-processor | grep Consumed
make lag                        # consumer-group lag should be ~0 (caught up)
```

You should see a `Consumed PLACE for order ...` line. 🎉 Producer → Kafka →
consumer works end-to-end.

---

## 5. Your turn — see the group catch up

Stop the processor, place a few orders, then start it again and watch it process
the backlog (offsets remember where it left off):

```bash
docker compose stop order-processor
curl -s -X POST localhost:8080/orders -H 'content-type: application/json' \
  -d '{"customer":"bob","items":[{"sku":"PEPPERONI","quantity":1,"unitPriceCents":1500}]}'
docker compose start order-processor
docker compose logs --since=1m order-processor | grep Consumed   # it catches up
make lag
```

---

## 6. You're done when

- [ ] `make up` runs all services; `docker compose ps` shows them up.
- [ ] Placing an order produces a `Consumed PLACE ...` log line in order-processor.
- [ ] `make lag` shows the group caught up (lag ~0).
- [ ] You can explain consumer group, offset, and lag — and why commit *timing*
      will matter (foreshadowing lesson 9).

Check your work:

```bash
git diff lesson/05-persistence-cqrs -- order-processor/ConsumerService.cs
```

---

## 7. Next

In **lesson 05** order-processor stops just logging and starts **persisting** to
Postgres (DbUp migrations + Dapper + the CQRS write model). Check out
`lesson/05-persistence-cqrs`.

<!-- NAV:START -->

---

## 🧭 Navigate

| ◀ Previous | 🔑 Solution to this lesson | Next ▶ |
|:---|:---:|---:|
| [Lesson 03 — Kafka producer](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/lesson/03-kafka-producer) | [view the diff](https://github.com/mike-kostov/dotnet-event-driven-orders/compare/lesson/04-kafka-consumer...lesson/05-persistence-cqrs) | [Lesson 05 — Persistence & CQRS](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/lesson/05-persistence-cqrs) |

Or from the terminal: `make prev` · `make solution` · `make next` · `make goto LESSON=4`
*(`next`/`prev`/`goto` switch branches — commit or stash your edits first; they never discard your work.)*

<!-- NAV:END -->
