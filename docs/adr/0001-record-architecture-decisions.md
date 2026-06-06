# ADR-0001: Record architecture decisions

## Status
Accepted

## Date
2026-06-05

## Context
This is a teaching repo that makes several non-obvious architectural choices
(service isolation, Kafka client, CQRS shape, delivery semantics, dead-lettering,
the lesson-branch model). Decisions made only in conversation or visible only in
code lose their *why*: future readers — and the learners this repo is built for —
re-litigate settled questions and risk reversing deliberate trade-offs. For a
tutorial, the *reasoning* is the curriculum, not a side note.

## Decision
Record each significant decision as an Architecture Decision Record (ADR) in
`docs/adr/`, numbered sequentially, using the format: Status, Date, Context,
Decision, Alternatives Considered, Consequences. ADRs are immutable once
accepted — when a decision changes, add a new ADR that supersedes the old one
rather than editing history.

> Note: the `documentation-and-adrs` skill suggests `docs/decisions/`. We use
> `docs/adr/` (also a widely-used convention) to match the approved `SPEC.md`
> and the reference repo `go-event-driven-orders`.

## Alternatives Considered

### No ADRs — rely on SPEC.md + README
- Pros: less to maintain.
- Cons: `SPEC.md` captures *what* we're building, not the rejected alternatives
  and trade-offs behind each choice; that context is exactly what a learner needs.
- Rejected: the decisions here are the lessons.

### ADRs written after implementation
- Pros: less up-front work.
- Cons: documentation of what happened, not a record of the reasoning at decision
  time; rationalizes rather than decides.
- Rejected: we write ADRs while the reasoning is fresh, before code.

## Consequences
- `docs/adr/` is the canonical home for decision rationale.
- Each major choice gets a short ADR that a lesson can link to for the "why".
- ADRs are referenced from the README, `SPEC.md`, and the relevant `LESSON.md`.
- The first batch (0002–0013) captures decisions already made in design discussion.
