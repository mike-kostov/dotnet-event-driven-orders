# ADR-0008: JSON message contract with per-service DTOs

## Status
Accepted

## Date
2026-06-05

## Context
`order-ingest` produces `OrderCommand` messages that `order-processor` consumes.
They need an agreed wire format. The reference Go repo uses Protocol Buffers with
committed codegen; we must decide whether to mirror that or choose something
better suited to a .NET beginner audience.

## Decision
Use **JSON** as the message format, with **each service owning its own DTOs** (no
shared contract project — see ADR-0002). The contract is documented in
`docs/architecture.md`:

`OrderCommand` envelope — `event_id` (idempotency key), `order_id` (partition
key), `type` ∈ {`PLACE`,`CONFIRM`,`PREPARE`,`DISPATCH`,`DELIVER`,`CANCEL`},
`issued_at`, and for `PLACE`: `items`, `total_cents`, `customer`.

Serialized with `System.Text.Json` (built-in).

## Alternatives Considered

### Protocol Buffers (mirror the Go repo)
- Pros: schema-enforced, compact, codegen catches drift; what the reference repo does.
- Cons: adds a codegen toolchain (`protoc`/build targets), generated code to
  explain, and binary payloads a learner can't read with `cat` or in Kafka UI.
  An extra concept before the first message moves.
- Rejected: JSON is human-readable and zero-toolchain — better for teaching.
  Protobuf is noted as a possible advanced follow-up.

### Shared `Contracts` project with C# records
- Pros: compile-time safety; no duplication.
- Cons: reintroduces the cross-service coupling ADR-0002 rejects.
- Rejected: contract is the boundary; duplication of a small DTO is acceptable.

## Consequences
- Messages are human-readable — learners can inspect them in a Kafka UI or logs.
- No codegen step; `docker compose up` needs no extra tooling.
- The contract is documented prose + example JSON, not enforced by a schema —
  drift is a risk, mitigated by integration tests across the two services.
- `event_id` and `order_id` semantics (ADR-0006) are part of the documented contract.
