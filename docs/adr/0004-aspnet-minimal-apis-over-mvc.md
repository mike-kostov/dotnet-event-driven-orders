# ADR-0004: ASP.NET Core Minimal APIs over MVC controllers

## Status
Accepted

## Date
2026-06-05

## Context
`order-ingest` and `order-query` expose small HTTP surfaces (a handful of
endpoints each). .NET offers two mainstream styles: MVC controllers or Minimal
APIs. The audience is new to backend, so the HTTP layer should be as small and
readable as possible.

## Decision
Use **ASP.NET Core Minimal APIs** (`WebApplication.CreateBuilder`, `app.MapPost`,
`app.MapGet`). No controllers, no MVC conventions. This matches the user's
production `phnotificationsapi`.

## Alternatives Considered

### MVC controllers
- Pros: familiar to many .NET devs; attribute routing; model binding ceremony some
  teams prefer; richer filters/conventions.
- Cons: more boilerplate (controller classes, base types, attributes) for tiny
  surfaces; indirection between route and handler obscures the request→handler
  path for a beginner.
- Rejected: too much ceremony to teach for ~6 endpoints.

### FastEndpoints / Carter (third-party)
- Pros: structure for larger APIs.
- Cons: another dependency and mental model to teach; unnecessary at this size.
- Rejected: not idiomatic-default; adds surface area.

## Consequences
- Each endpoint is a few lines in `Program.cs` (or a small extension), so the
  request→Kafka and request→SQL paths are immediately visible.
- Less to explain before a learner sees a working endpoint.
- If the API grew large, controllers/FastEndpoints could be revisited — out of
  scope for this tutorial.
