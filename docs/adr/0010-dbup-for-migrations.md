# ADR-0010: DbUp for database migrations

## Status
Accepted

## Date
2026-06-05

## Context
The Postgres schema (write model + read projection, ADR-0005) must be created
before the services start, repeatably, with no migration tooling required on the
host (`docker compose up` must just work). The user's production project uses
FluentMigrator, but asked specifically for an "idiomatic, simple, KISS" tool here.

## Decision
Use **DbUp**: plain ordered `.sql` scripts in `db/migrations/`, run by a small
console program (`tools/migrate`) that DbUp drives. DbUp tracks which scripts have
run in a `SchemaVersions` table and applies only new ones. The console program is
packaged as the **one-shot `migrate` container** in compose, gated before the
services start.

## Alternatives Considered

### FluentMigrator (what the user's `phnotificationsapi` uses)
- Pros: familiar to the user; mature; up/down migrations; provider-agnostic.
- Cons: migrations are C# classes using a fluent DSL — the actual SQL is hidden
  behind `Create.Table(...).WithColumn(...)`. That conflicts with the
  hand-written-SQL choice for queries (ADR-0009): a learner would meet two
  different schema languages.
- Rejected: hides SQL; inconsistent with Dapper.

### EF Core migrations
- Pros: integrated if EF Core were the data layer.
- Cons: we rejected EF Core (ADR-0009); generated migrations are opaque.
- Rejected: no EF Core in the stack.

### Generic psql one-shot container (mirror the Go repo's `migrate` container)
- Pros: zero .NET code; language-agnostic; simplest infra.
- Cons: not "idiomatic .NET"; no applied-version tracking without extra scripting.
- Rejected: DbUp gives the same plain-SQL scripts *and* version tracking *and*
  stays in the .NET toolchain the learner is already using.

## Consequences
- Schema lives as readable `.sql`, consistent with Dapper queries (ADR-0009).
- The migrate step is a real .NET console app — itself a small teachable artifact.
- Re-running compose is safe: already-applied scripts are skipped.
- Forward-only by default (DbUp's model); destructive changes are new scripts.
