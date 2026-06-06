# ADR-0003: Use raw Confluent.Kafka over a message-bus abstraction

## Status
Accepted

## Date
2026-06-05

## Context
The producer and consumer need a way to talk to Kafka in .NET. The dominant
options are a high-level bus abstraction (MassTransit, NServiceBus) or the
low-level `Confluent.Kafka` client directly. This is a teaching repo: the goal is
to *understand* event-driven plumbing, not to hide it.

## Decision
Use **`Confluent.Kafka`** directly — produce, consume, manual offset commit, and a
hand-rolled DLQ producer are all written explicitly. No bus abstraction.

This matches the user's production services (`phnotificationsproducer`,
`phnotificationsconsumer`) and keeps the mechanics (partitions, keys, offsets,
consumer groups, at-least-once) visible.

## Alternatives Considered

### MassTransit
- Pros: ergonomic; sagas, retries, scheduling, outbox built in; less boilerplate.
- Cons: hides offsets, commits, partitioning, and DLQ mechanics behind
  conventions — exactly the things the tutorial exists to teach. Heavy abstraction
  to explain before a learner sees a single message move.
- Rejected: teaches the framework, not Kafka.

### NServiceBus
- Pros: mature, enterprise-grade saga + recoverability story.
- Cons: commercial licensing; even more abstraction; transport-agnostic design
  obscures Kafka specifics.
- Rejected: licensing + opacity are wrong for a free teaching repo.

## Consequences
- Learners see and write the real mechanics: `IProducer`, `IConsumer`, `Commit`,
  partition keys, consumer groups.
- At-least-once + manual commit (ADR-0006) and the DLQ (ADR-0007) are explicit code.
- More boilerplate than a bus — which is the point; each piece is a teachable unit.
- If a learner later wants MassTransit, they'll understand what it abstracts.
