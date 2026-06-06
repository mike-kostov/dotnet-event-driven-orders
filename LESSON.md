# Lesson 02 — Your first service: order-ingest

> **You are on** `lesson/02-order-ingest-api`. Lesson 1's tooling is complete
> here (the `hello` container + `make psql` are done — that's the "solution to
> lesson 1"). Now you build the first **real** service.
>
> Fill in the `TODO(you)` markers following the steps below. Check your work
> against the next branch, `lesson/03-kafka-producer`.

---

## 1. Why this lesson exists

Every event-driven system has a **front door**: something that receives a
request and turns it into work. Here that's `order-ingest` — an HTTP API where a
client places an order.

We build it on its own first, before adding Kafka (lesson 3), so the HTTP
concerns are clear. The big idea you'll meet: **`202 Accepted`**. order-ingest
has no database and doesn't decide whether an order is *valid business-wise* — it
only checks the request *shape* and replies "got it, I'll process this." That's
your first taste of **asynchronous** thinking: accepted ≠ done.

---

## 2. Concepts

- **Minimal API** — ASP.NET Core's lightweight style: you map a route straight to
  a handler (`app.MapPost("/orders", ...)`). No controller classes. (ADR-0004)
- **DTO** (Data Transfer Object) — a plain shape for the request/response body.
  Ours live in `order-ingest/Contracts/` and are **owned by this service**
  (no shared project — ADR-0002).
- **Status codes** — `202 Accepted` (taken, will process), `400 Bad Request`
  (the request shape is wrong).
- **Multi-stage Dockerfile** — build with the big SDK image, ship only the
  published output in a small runtime image.

---

## 3. Do this — build the POST /orders handler  ← main task

Open `order-ingest/Program.cs`. The app, `/healthz`, and the route are wired for
you. Fill in the four `TODO(you)` markers in the `POST /orders` handler:

- **2.1 — validate the shape.** Return `400` if `Customer` is empty, `Items` is
  empty, or any item has `Quantity <= 0` / `UnitPriceCents <= 0`.
- **2.2 — make an order id.** `var orderId = Guid.NewGuid().ToString();`
- **2.3 — log it.** `app.Logger.LogInformation("Accepted order {OrderId}", orderId);`
- **2.4 — return 202** with the id:
  `return Results.Accepted($"/orders/{orderId}", new PlaceOrderResponse(orderId));`

Remove the `Results.StatusCode(501)` placeholder once the TODOs return real results.

---

## 4. Build the Dockerfile (multi-stage)

Open `order-ingest/Dockerfile` and fill `TODO(you)` 3.1–3.5 — a **two-stage**
build (you did a one-stage build in lesson 1; the hints are inline). The result:
a small image that contains the published app but not the SDK.

Then run it:

```bash
cp .env.example .env            # if you don't have a .env yet
make up                         # builds order-ingest and starts everything
docker compose ps               # order-ingest should be Up
```

Try it:

```bash
# valid → 202 Accepted + an order id
curl -i -X POST localhost:8080/orders \
  -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[{"sku":"MARGHERITA","quantity":1,"unitPriceCents":1200}]}'

# invalid (no items) → 400 Bad Request
curl -i -X POST localhost:8080/orders \
  -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[]}'
```

> Port 8080 already in use? Change `ORDER_INGEST_PORT` in `.env`, then `make up`.

---

## 5. Your turn — add the transition route stubs

Later (lesson 6) an order moves through states via commands. Add **stub** routes
now that just return `202` (no logic yet):

```csharp
app.MapPost("/orders/{id}/confirm",  (string id) => Results.Accepted());
app.MapPost("/orders/{id}/prepare",  (string id) => Results.Accepted());
app.MapPost("/orders/{id}/dispatch", (string id) => Results.Accepted());
app.MapPost("/orders/{id}/deliver",  (string id) => Results.Accepted());
app.MapPost("/orders/{id}/cancel",   (string id) => Results.Accepted());
```

`curl -i -X POST localhost:8080/orders/abc/confirm` → `202`.

---

## 6. You're done when

- [ ] `make up` builds `order-ingest` and it shows `Up` in `docker compose ps`.
- [ ] Valid `POST /orders` → `202` with an `orderId`; the app logs it.
- [ ] Invalid `POST /orders` (empty items) → `400`.
- [ ] The five transition stubs return `202`.
- [ ] You can explain why order-ingest returns `202` and not `200`/`201`.

Check your work:

```bash
git diff lesson/03-kafka-producer -- order-ingest Program.cs
```

---

## 7. Next

In **lesson 03** `order-ingest` stops just logging and starts **producing** each
accepted order to Kafka. Check out `lesson/03-kafka-producer`.
