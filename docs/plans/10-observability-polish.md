# Lesson 10 — Observability & final polish

> Plan for authoring `lesson/10-observability-polish`. Builds on `lesson/09-reliability-dlq-replay`.
> `lesson/10` is the **start state** of lesson 10 (lesson 9 complete + this
> `LESSON.md` + stubs). Completing it produces the **`final`** branch — the
> finished system and reachable end state SPEC.md specifies. `main` holds the
> foundation only (ADR-0012).

## Objective

Make the running system observable and the repo presentable: structured JSON
logging, health/readiness endpoints, basic OpenTelemetry traces/metrics, and the
final polish (README with diagram + curl walkthrough, `make seed`/`make lag`,
all SPEC success criteria green under one `docker compose up`).

After this lesson a learner can: add structured logging and OpenTelemetry to a
.NET service, expose health/readiness, and read traces to follow an order across
services.

## The "why" (for LESSON.md)

A system you can't see is a system you can't operate. Earlier lessons logged
ad hoc and used minimal `/healthz`; now we make observability deliberate.
**Structured JSON logs** are queryable, not just readable. **Health vs readiness**
distinguishes "alive" from "ready for traffic" (what compose health-gates on).
**Distributed tracing** (OpenTelemetry) lets you follow a single order across
ingest → Kafka → processor → query — the only sane way to debug an async system.
This is also where the repo becomes something a learner can show off.

## The slice this lesson builds

- All three services: structured JSON logging via `Microsoft.Extensions.Logging`;
  `/healthz` (liveness) + `/readyz` (readiness: deps reachable); OpenTelemetry
  traces + a few metrics (consumer lag, processed/DLQ counts), exported to console
  (and OTLP-ready).
- `docker-compose.yml`: health/readiness wired into `depends_on` gating; everything
  comes up healthy in dependency order.
- `README.md`: the thesis, an architecture diagram, a copy-pasteable `curl`
  walkthrough, the lesson index, and a "what's intentionally out of scope" note.
- Final pass: `make up` → all green; `make seed`, `make chaos`, `make replay`,
  `make lag`, `make test` all work as documented (SPEC success criteria 1–9).
- `LESSON.md`.

## Tasks (authoring order)

### Task 1: Structured logging across services
**Acceptance:** JSON logs with correlation (order_id / event_id) in each service.
**Verify:** `make logs` shows structured entries you can grep by order_id.
**Files:** logging setup in each service's `Program.cs`
**Scope:** M

### Task 2: Health + readiness endpoints
**Acceptance:** `/healthz` (liveness) and `/readyz` (deps) on each service; compose
gates on readiness where appropriate.
**Verify:** `make ps` healthy; stopping Postgres flips order-query `/readyz` to unhealthy.
**Files:** each `Program.cs`, `docker-compose.yml`
**Scope:** M

### Task 3: OpenTelemetry traces + key metrics
**Acceptance:** a trace follows an order ingest→processor; metrics expose consumer
lag and processed/DLQ counts.
**Verify:** console exporter shows spans spanning services for one order.
**Files:** OTel setup in each service
**Scope:** M

### Task 4: README + final polish
**Acceptance:** README has diagram, curl walkthrough, lesson index, scope note;
all SPEC criteria 1–9 verified.
**Verify:** a fresh clone of `main` → `make up` → walkthrough works end-to-end.
**Files:** `README.md`, `Makefile`
**Scope:** M

### Task 5: LESSON.md
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents
1. **Why** — operability; logs/metrics/traces; liveness vs readiness.
2. **Concepts** — structured logging; OpenTelemetry traces & metrics; health gating.
3. **Do this** — `make up`; follow one order through the trace; break a dep and watch `/readyz`.
4. **Inspect** — read the OTel setup; find the correlation id in logs.
5. **Your turn** — add a metric or a log field; see it appear.
6. **You're done when** — all SPEC success criteria pass; the completed result is the `final` branch.
7. **Done** — recap the journey; pointers to advanced follow-ups (protobuf+Schema
   Registry, MassTransit, cloud deploy, Aspire) explicitly *out of scope* here.

## Checkpoint / done criteria
- [ ] `docker compose up` brings everything up healthy (SPEC criterion 1).
- [ ] All SPEC success criteria (1–9) verified; completed result becomes the `final` branch.
- [ ] Traces follow an order across services; logs are structured.
- [ ] README is self-sufficient for a newcomer.

## Dependencies & next
- **Depends on:** lesson 09 (behaviors to observe).
- **Feeds:** the `final` branch — completing this lesson produces the finished system.

## Risks / open questions
- Keep OTel to console export by default (zero extra infra); mention OTLP/Collector
  as optional so we don't pull a metrics stack into compose (SPEC boundary).
- If observability proves too large for one lesson, split logging/health (10a) from
  tracing/metrics (10b).
