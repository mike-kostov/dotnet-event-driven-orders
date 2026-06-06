# Lesson 02 — First service: order-ingest HTTP API

> Plan for authoring `lesson/02-order-ingest-api`. Builds on `lesson/01-tooling`.
> Decisions: [ADR-0002](../adr/0002-three-isolated-services-no-shared-code.md),
> [ADR-0004](../adr/0004-aspnet-minimal-apis-over-mvc.md),
> [ADR-0011](../adr/0011-client-driven-transitions.md).

## Objective

Build the first real .NET service: `order-ingest`, an ASP.NET Core **Minimal API**
that accepts orders over HTTP, validates their shape, and returns `202 Accepted`.
No Kafka yet — this lesson is about the HTTP service and its container.

After this lesson a learner can: create a Minimal API, define request/response
DTOs, validate input, write a service `Dockerfile`, and run the service in compose
alongside the infra from lesson 1.

## The "why" (for LESSON.md)

Every event-driven system still has a front door — something that receives a
request and turns it into a message. We build that door first, on its own, so the
HTTP concerns (routing, validation, status codes) are learned before Kafka is
added. `202 Accepted` is deliberate: order-ingest *accepts* the request for
processing but cannot confirm the order is applied — it has no database
(ADR-0002) and does not decide legality (ADR-0011). That gap is the first taste of
asynchronous, eventually-consistent thinking.

## The slice this lesson builds

Contents of `lesson/02-order-ingest-api` (lessons 1–2 completed) = everything from
`lesson/01-tooling`, plus:

- `order-ingest/` — its own project, `Dockerfile`, image (ADR-0002):
  - Minimal API with `POST /orders` (PLACE): validates body, returns `202` + a
    generated `order_id`; malformed body → `400` (SPEC success criterion 3).
  - `GET /healthz` (so compose can health-gate it).
  - Request/response DTOs owned by this service (no shared project).
  - In this lesson the handler just validates + logs the accepted order (no Kafka).
- `docker-compose.yml` — extended to build & run `order-ingest`, health-gated.
- `Makefile` — already has the core targets; add `make seed`-style stub later.
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: order-ingest project + Minimal API skeleton
**Acceptance:** `dotnet run` serves `GET /healthz` → `200`.
**Verify:** `curl localhost:8080/healthz` → 200.
**Files:** `order-ingest/OrderIngest.csproj`, `order-ingest/Program.cs`
**Scope:** S

### Task 2: `POST /orders` with DTOs + validation
**Acceptance:**
- [ ] Valid body → `202` with `{ "order_id": "…" }`.
- [ ] Missing/invalid fields (no items, non-positive total) → `400` with a problem detail.
**Verify:** two `curl` calls (valid → 202, invalid → 400).
**Files:** `order-ingest/Program.cs`, `order-ingest/Contracts/*.cs`
**Scope:** S

### Task 3: Dockerfile for order-ingest
**Acceptance:** multi-stage `Dockerfile` builds a runnable image; container serves
`/healthz`.
**Verify:** `docker build` + run; `/healthz` → 200 from the container.
**Files:** `order-ingest/Dockerfile`
**Scope:** S

### Task 4: Wire into compose, health-gated
**Acceptance:** `make up` brings up infra + order-ingest, all healthy.
**Verify:** `make ps` shows order-ingest healthy; `curl` the running container.
**Files:** `docker-compose.yml`, `.env.example`
**Scope:** S

### Task 5: LESSON.md
**Acceptance:** beginner can build the endpoint and explain why it returns `202`.
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents

1. **Why** — the front door + the meaning of `202` (async accept).
2. **Concepts** — Minimal APIs vs controllers (link ADR-0004); DTOs; validation;
   HTTP status codes; multi-stage Dockerfiles.
3. **Do this** — create endpoints; `curl` valid + invalid; build the image; `make up`.
4. **Inspect** — read the Dockerfile stages; see health-gating in compose.
5. **Your turn** — add the `cancel`/transition route stubs (returning 202), fully
   fleshed out in lesson 06.
6. **You're done when** — checklist.
7. **Next** — lesson 03 makes `POST /orders` actually produce a Kafka message.

## Checkpoint / done criteria
- [ ] `make up` → infra + order-ingest healthy.
- [ ] Valid POST → 202 + order_id; invalid → 400, nothing else happens.
- [ ] Learner can explain the no-DB / 202 design.

## Dependencies & next
- **Depends on:** lesson 01 (compose + Make + infra).
- **Feeds:** lesson 03 (Kafka producer) turns the accepted order into a message.

## Risks / open questions
- Validation approach: hand-rolled checks (simplest, most visible) vs a library
  (FluentValidation). *Leaning hand-rolled for lesson clarity; revisit if it gets noisy.*
