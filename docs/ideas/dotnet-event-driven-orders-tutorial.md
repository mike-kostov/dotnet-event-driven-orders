# dotnet-event-driven-orders tutorial

## Problem Statement
How might we create a beginner-friendly .NET tutorial that teaches event-driven
system design through a concrete, runnable order processing system — structured
so learners build understanding incrementally, one milestone at a time?

## Recommended Direction

A tutorial repository with **~10 lesson branches** (milestone-driven, not
count-driven), each containing a `LESSON.md` with conceptual "why", objectives,
and step-by-step instructions. Lesson `N+1` is the solution to lesson `N` — no
duplication, no paired start/solution branches.

Stack: **ASP.NET Core Minimal APIs + Confluent.Kafka (raw) + PostgreSQL +
Docker Compose + Make**. This matches what the user already runs in production
(`phnotificationsapi` uses Minimal APIs; `phnotificationsproducer` /
`phnotificationsconsumer` use raw `Confluent.Kafka`). Same 3-service topology as
`go-event-driven-orders` (order-ingest, order-processor, order-query) so Go
developers can compare side-by-side and .NET beginners see a real
production-shaped system.

### Audience
Total beginners to backend — including front-end developers who are curious and
want to learn. This shapes everything: tooling is taught, not assumed.

### Git structure
- `main` — complete working system; `docker-compose up` brings it alive
- `lesson/01-tooling` … `lesson/10-polish` — each branch is the **start state**
  for that lesson; the next branch is its solution

Each `LESSON.md` contains: the conceptual **why** (event-driven theory in
plain language), learning objectives, step-by-step tasks, verification criteria
("you know you're done when…"), and a pointer to the next lesson.

### Milestone-driven lesson arc (≈10)
1. **Tooling & dev environment** — Docker, docker-compose, Make. Learners cover
   the basics hands-on: what a `Dockerfile` does, what an image is, what a
   container is, how we get reproducible images/state. Make is explained enough
   to use confidently — learners run and even add one or two `make` targets so
   they're not afraid to touch the `Makefile`.
2. **Project structure & first ASP.NET Core Minimal API**
3. **Kafka concepts + docker-compose Kafka setup**
4. **order-ingest**: HTTP → publishes `OrderPlaced`
5. **order-processor**: consumes `OrderPlaced` → processes → persists state
6. **order-query**: read-side HTTP API
7. **Testing** (unit + integration with Testcontainers)
8. **Observability** (OpenTelemetry, structured logging, health checks)
9. **Idempotency & error handling** (at-least-once delivery, dead letter)
10. **Final polish**: `docker-compose up` brings everything alive

If a lesson naturally splits (e.g. tooling into 1a/1b), the count grows. The
boundaries come from milestones, not a target number.

## Key Assumptions to Validate
- [ ] Beginners can learn Docker/compose/Make in one lesson — may need to split
      into 1a/1b. Test with a front-end developer.
- [ ] Raw Confluent.Kafka is approachable without a bus abstraction — test with
      a non-backend reviewer.
- [ ] The 3-service topology doesn't overwhelm beginners in early lessons —
      lessons 1–3 may need to defer multi-service awareness.

## MVP Scope
Lessons 1–6: a running 3-service system with `docker-compose up`. Lessons 7–10
add testing, observability, error handling, and polish. Personal git identity:
**Mike Kostov / mike.kostov@gmail.com** throughout.

## Not Doing (and Why)
- **No MassTransit or NServiceBus** — abstractions hide the learning; raw Kafka
  forces understanding (and matches the user's production stack).
- **No cloud deployment lesson** — scope creep for beginners; separate tutorial.
- **No protobuf contracts** — JSON Kafka messages; protobuf is advanced.
- **No frontend** — backend fundamentals only.
- **No CI/CD pipeline** — same reasoning as cloud deployment.

## Decisions (resolved during refinement)
- **Make**: explained enough to use; learners add one or two targets hands-on so
  the `Makefile` feels approachable, not magic.
- **Docker/compose**: learners cover the basics — what the `Dockerfile` does,
  what the image is, what the container is, how reproducibility is achieved.
- **LESSON.md**: each includes the conceptual "why", not just steps.

## Open Questions
- Does lesson 1 (tooling) hold together, or does it split into 1a (Docker) and
  1b (Make + compose) once written?
