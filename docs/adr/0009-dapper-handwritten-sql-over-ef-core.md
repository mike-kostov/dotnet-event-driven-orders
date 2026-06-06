# ADR-0009: Dapper with hand-written SQL over EF Core

## Status
Accepted

## Date
2026-06-05

## Context
`order-processor` (write + projection) and `order-query` (read) need to talk to
Postgres. .NET's mainstream choices are EF Core (a full ORM) or Dapper (a thin
micro-ORM over hand-written SQL). The tutorial's guiding philosophy is to *show
how things work* rather than hide them (cf. raw Kafka, ADR-0003).

## Decision
Use **Dapper** over **`Npgsql`**, with **hand-written SQL** kept visible in the
codebase. Queries are plain SQL strings/files; Dapper handles parameterization and
row mapping. This is the read/write analog of the Go repo's `sqlc` (also
hand-written SQL).

## Alternatives Considered

### EF Core
- Pros: very common in .NET; LINQ queries; change tracking; migrations built in.
- Cons: hides the SQL — a beginner never sees the query that runs, and the
  CQRS write-model/read-model distinction blurs behind `DbContext`. Change
  tracking and the unit-of-work add concepts orthogonal to the lesson.
- Rejected: hiding SQL works against the teaching goal; also conflicts with using
  DbUp for migrations (ADR-0010).

### Raw ADO.NET (`NpgsqlCommand` + manual readers)
- Pros: nothing hidden at all.
- Cons: tedious mapping boilerplate that obscures intent.
- Rejected: Dapper removes the boilerplate while keeping the SQL fully visible.

## Consequences
- Every query a learner runs is right there as SQL — directly teachable.
- Pairs cleanly with DbUp (ADR-0010): same hand-written-SQL mental model for
  schema and queries.
- No migrations/change-tracking magic; the schema is owned by DbUp scripts.
- Idempotent upserts (`ON CONFLICT`, ADR-0006) are written as explicit SQL.
