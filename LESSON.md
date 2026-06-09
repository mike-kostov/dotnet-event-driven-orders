# Lesson 10 — Observability & final polish

> **You are on** `lesson/10-observability-polish` — the finish line. The system is
> complete and reliable (lessons 1–9). Now you make it **observable** and polish
> the repo. Completing this lesson produces the **`final`** branch — the finished
> system. Fill in the `TODO(you)` and diff against `final` to check your work.

---

## 1. Why this lesson exists

A system you can't see is a system you can't operate. Earlier lessons logged
ad-hoc; now we make observability deliberate:

- **Structured JSON logs** are queryable, not just readable.
- **Health vs readiness** — `/healthz` ("alive") vs `/readyz` ("ready for
  traffic"); compose health-gates on these.
- **Distributed tracing** (OpenTelemetry) lets you follow one request as a span —
  the only sane way to debug async systems.

This is also where the repo becomes something you can show off (the README).

---

## 2. Concepts

- **Structured logging** — `builder.Logging.AddJsonConsole()` emits each log line
  as JSON (with the `{OrderId}` etc. as fields).
- **OpenTelemetry** — vendor-neutral traces/metrics. We export **to the console**
  (no extra infra, per the SPEC boundary); swapping in an OTLP collector later is
  one line.
- **AspNetCore instrumentation** — auto-creates a span per HTTP request.

`order-processor` and `order-query` already have this wired (read their
`Program.cs`). Your job is to add it to `order-ingest`.

---

## 3. Do this — wire observability into order-ingest

Open `order-ingest/Program.cs`. The OpenTelemetry packages are already referenced.
Fill the two `TODO(you)` markers:

- **10.1** — add `builder.Logging.AddJsonConsole();`
- **10.2** — add the usings (`using OpenTelemetry.Resources;`, `using OpenTelemetry.Trace;`)
  and:
  ```csharp
  builder.Services.AddOpenTelemetry()
      .ConfigureResource(r => r.AddService("order-ingest"))
      .WithTracing(t => t.AddAspNetCoreInstrumentation().AddConsoleExporter());
  ```

---

## 4. Run it and observe

```bash
cp .env.example .env            # if needed
make up
curl -s -o /dev/null -X POST localhost:8080/orders -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[{"sku":"MARGHERITA","quantity":1,"unitPriceCents":1200}]}'
docker compose logs order-ingest | tail -20
```

You should see (a) log lines as **JSON**, and (b) an **Activity/span** printed by
the console exporter for the `POST /orders` request.

---

## 5. Your turn — see readiness gating

`/healthz` and `/readyz` already exist on the services. Confirm the stack comes up
healthy in dependency order (`make ps`), and skim a trace end-to-end.

---

## 6. You're done when — the whole tutorial's success criteria (SPEC.md)

- [ ] `make up` brings everything up healthy.
- [ ] Happy path: place → confirm → prepare → dispatch → deliver → `DELIVERED` (`make seed`).
- [ ] Malformed `POST /orders` → `400`, nothing produced.
- [ ] Duplicate event → exactly one row (idempotency).
- [ ] No loss under crash; lag returns to ~0.
- [ ] Poison quarantined in `orders.DLT`; partition keeps moving.
- [ ] `make replay` drains the DLT safely.
- [ ] `make test` passes.
- [ ] order-ingest logs are JSON and a trace span appears for `POST /orders`.

When all green, this branch's state **is the `final` branch**.

Check your work:

```bash
git diff final -- order-ingest/Program.cs
```

---

## 7. Done 🎉

You built a complete, reliable, observable event-driven system from `make` and a
`Dockerfile` up to CQRS, idempotency, and dead-lettering. Advanced follow-ups
(all intentionally out of scope here): protobuf + Schema Registry, MassTransit,
.NET Aspire, an OTLP collector + metrics stack, and cloud deployment.

<!-- NAV:START -->

---

## 🧭 Navigate

| ◀ Previous | 🔑 Solution to this lesson | Next ▶ |
|:---|:---:|---:|
| [Lesson 09 — Reliability, DLQ, replay](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/lesson/09-reliability-dlq-replay) | [view the diff](https://github.com/mike-kostov/dotnet-event-driven-orders/compare/lesson/10-observability-polish...final) | [Finished system (`final`)](https://github.com/mike-kostov/dotnet-event-driven-orders/tree/final) |

Or from the terminal: `make prev` · `make solution` · `make next` · `make goto LESSON=10`
*(`next`/`prev`/`goto` switch branches — commit or stash your edits first; they never discard your work.)*

<!-- NAV:END -->
