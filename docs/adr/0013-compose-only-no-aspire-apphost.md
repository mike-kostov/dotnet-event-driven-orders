# ADR-0013: Docker Compose only — no .NET Aspire AppHost

## Status
Accepted

## Date
2026-06-05

## Context
.NET Aspire provides an AppHost that orchestrates services and dependencies for
local dev (the user's production services use one). For this tutorial we must
decide whether to orchestrate with Aspire or plain Docker Compose. The audience is
new to backend and the explicit goal is to make the moving parts *visible*.

## Decision
Orchestrate with **Docker Compose only**. A single `docker-compose.yml` wires
Kafka (KRaft), Postgres, the one-shot `topic-init` and `migrate` containers, and
the three services — each gated on its dependencies' health. **No Aspire AppHost.**

This is a deliberate divergence from the user's production setup.

## Alternatives Considered

### .NET Aspire AppHost
- Pros: rich local dev dashboard; service discovery; less YAML; production-aligned
  with the user's other projects.
- Cons: adds an orchestration abstraction and its own model to learn *before* the
  system runs; hides container/image/network mechanics that lesson 1 exists to
  teach; couples the tutorial to the Aspire toolchain.
- Rejected: the tutorial teaches Docker, images, containers, and compose directly
  (lesson 1) — Aspire would paper over exactly that.

### Kubernetes / Helm
- Pros: production-grade.
- Cons: massively more to learn; wrong altitude for beginners.
- Rejected: out of scope (also excluded in `SPEC.md` boundaries).

## Consequences
- Lesson 1 can teach `Dockerfile` → image → container → compose with nothing
  hidden, and reproducibility is demonstrable.
- `docker compose up` is the single entry point across the whole tutorial.
- Health-gating and one-shot init containers are explicit in the compose file —
  themselves teachable.
- Learners who later meet Aspire will understand what it orchestrates.
