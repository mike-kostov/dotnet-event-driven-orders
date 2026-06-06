# Lesson 01 — Tooling & dev environment

> Plan for authoring `lesson/01-tooling`. Dev-facing: what the branch contains,
> how to build it, and what its learner-facing `LESSON.md` must teach.
> Part of the arc in [SPEC.md](../../SPEC.md); decisions in
> [ADR-0013](../adr/0013-compose-only-no-aspire-apphost.md).

## Objective

Get a complete beginner comfortable with the **tooling** the whole tutorial rests
on — Docker, Docker Compose, and Make — by standing up the project's real
infrastructure (Kafka + Postgres) with one command. No application services yet.

After this lesson a learner can: explain what a Dockerfile, an image, and a
container are; run and stop the stack with `make`; and read a `docker-compose.yml`.

## The "why" (for LESSON.md)

A backend system is many programs that must run the same way on every machine.
Before writing any of them, you need a reproducible way to *run things*:

- A **Dockerfile** is a recipe for building an **image** (a frozen, reproducible
  snapshot of an app + its dependencies). A **container** is a running instance of
  an image. Same image → same behaviour on your laptop and in CI → "works on my
  machine" stops being a problem.
- **Docker Compose** declares a set of containers (and how they depend on each
  other) in one `docker-compose.yml`, so the whole system starts with one command.
- **Make** is a tiny command memory: instead of typing long `docker compose …`
  incantations, you run `make up`. The `Makefile` is plain text you can read and
  extend — it is not magic.

We run the real infrastructure (Kafka, Postgres) from this very first lesson so the
tooling is learned against the actual moving parts, not a toy.

## The slice this lesson builds

`lesson/01-tooling` is the **start state** of lesson 1 (branched from the `main`
foundation). What the learner **builds** here, guided step-by-step by `LESSON.md`:

- `hello/` — a tiny (~10-line) `dotnet` console app and its **hand-written
  `Dockerfile`**. Writing this Dockerfile is the core hands-on moment (per the
  agreed "keep Dockerfile-writing"); it makes "Dockerfile → image → container"
  concrete, and its shape matches the real services' Dockerfiles in later lessons.
- `docker-compose.yml` — **Kafka** (KRaft, single broker) and **Postgres 16**,
  both health-gated, plus the `hello` service.
- `Makefile` — `up`, `down`, `logs`, `ps` targets, then one more (`psql` or
  `topics`) as the closing hands-on exercise (Task 4).
- `.env.example` — ports per SPEC (Postgres `:5432`, Kafka `:9092`).

Provided on the branch as the starting point: `LESSON.md` (the walkthrough) and
light scaffolding/stubs (e.g. file skeletons with `TODO` markers) so a total
beginner has a frame to fill, not a blank page. `.gitignore` is already on `main`.

> Branch model (ADR-0012): `main` is the foundation (docs only). `lesson/01-tooling`
> is the **start** of lesson 1 — the learner builds the tooling in place by
> following `LESSON.md`. The **completed** result is the next branch
> (`lesson/02-order-ingest-api`); diff against it to check your work. (Lesson 1 is
> the one lesson that builds tooling rather than app code, since it's the entry point.)

## Tasks (authoring order)

### Task 1: Compose file for Kafka + Postgres
**Description:** Author `docker-compose.yml` with Kafka (KRaft, no Zookeeper) and
Postgres 16, each with a healthcheck.
**Acceptance:**
- [ ] `docker compose up` starts both; both reach `healthy`.
- [ ] Postgres on `:5432`, Kafka on `:9092` (per SPEC ports).
**Verify:** `docker compose ps` shows both healthy; `docker compose exec postgres
pg_isready` succeeds; Kafka broker API reachable (e.g. list topics).
**Files:** `docker-compose.yml`, `.env.example`
**Scope:** S

### Task 2: A hand-written Dockerfile (the `hello` service)
**Description:** A minimal app with its own `Dockerfile` so the learner builds one
image from source and runs it as a container. Keep it ~10 lines; its only job is
to be a concrete Dockerfile example.
**Acceptance:**
- [ ] `docker compose up hello` builds the image and prints a hello line / serves
      a trivial `/` response, then the learner can see it in `docker compose ps`/logs.
**Verify:** `make logs` (or `docker compose logs hello`) shows the output.
**Files:** `hello/Dockerfile`, `hello/` (minimal app)
**Scope:** S

### Task 3: Makefile with the core targets
**Description:** `Makefile` wrapping the compose commands.
**Acceptance:**
- [ ] `make up`, `make down`, `make logs`, `make ps` work and match the SPEC's
      intended command shape.
- [ ] `make down` uses `-v` to reset state (teaches reproducibility).
**Verify:** each target runs; `make down` then `make up` is a clean reset.
**Files:** `Makefile`
**Scope:** S

### Task 4: Hands-on target (learner exercise)
**Description:** `LESSON.md` walks the learner through adding one small target —
e.g. `make psql` (open a `psql` shell in the Postgres container) or `make topics`
(list Kafka topics) — themselves. On `lesson/01` it's a documented `TODO` in the
`Makefile`; the completed version lands on `lesson/02`.
**Acceptance:**
- [ ] `Makefile` has a clearly-marked `# TODO (you):` stub + a pointer to the
      `LESSON.md` step.
- [ ] Following `LESSON.md`, `make <target>` works.
**Verify:** the learner adds it and runs it; diff against `lesson/02` matches.
**Files:** `Makefile`, `LESSON.md`
**Scope:** XS

### Task 5: LESSON.md
**Description:** Write the learner-facing teaching content (structure below).
**Acceptance:** covers the "why", has a do-this walkthrough, the Task 4 exercise,
and a verification checklist; links to ADR-0013 for the compose-vs-Aspire "why".
**Verify:** a reader new to backend can go from clone → `make up` → healthy stack
→ added target, using only LESSON.md.
**Files:** `LESSON.md`
**Scope:** S

## LESSON.md contents (learner-facing)

1. **Why this lesson** — the reproducibility story above.
2. **Concepts** — Dockerfile vs image vs container; what Compose adds; what Make is.
3. **Do this** — install check (`docker`, `make`); `make up`; watch it become
   healthy; `make ps`/`make logs`; `make down`.
4. **Inspect** — read `hello/Dockerfile` line by line; read `docker-compose.yml`
   (services, healthchecks, ports, `depends_on`).
5. **Your turn** — add the missing Make target (Task 4).
6. **You're done when** — verification checklist.
7. **Next** — `lesson/02` adds the first real .NET service.

## Checkpoint / done criteria

- [ ] `make up` brings Kafka + Postgres (+ `hello`) up healthy on a clean machine.
- [ ] `make down` fully resets; re-running `make up` is reproducible.
- [ ] Learner can articulate Dockerfile/image/container and add a Make target.
- [ ] `LESSON.md` is self-sufficient for a beginner.

## Dependencies & next

- **Depends on:** nothing (entry lesson).
- **Feeds:** `lesson/02` (first service) reuses this compose + Makefile and adds a
  service with its own Dockerfile.

## Risks / open questions

- **`hello` service language:** a tiny `dotnet` console keeps the toolchain
  consistent (learner sees .NET from day 1) but adds a build step; a stock
  `hello-world`/`alpine echo` image is simpler but non-.NET. *Leaning: tiny dotnet
  console, so the Dockerfile they read is the same shape as the real services'.*
- **Does lesson 1 split into 1a/1b?** Assumption in SPEC. Validate after drafting
  LESSON.md — if the concepts + hands-on exceed ~60 min, split Docker (1a) from
  Compose+Make (1b).
