-- 0001_init.sql — CQRS in one Postgres database (ADR-0005).
-- Run by the DbUp `migrate` container (ADR-0010). Plain, hand-written SQL.

-- Append-only event log. event_id is the PRIMARY KEY = the idempotency anchor.
-- Lesson 9 relies on INSERT ... ON CONFLICT (event_id) DO NOTHING here.
CREATE TABLE IF NOT EXISTS order_events (
    event_id    TEXT PRIMARY KEY,
    order_id    TEXT NOT NULL,
    type        TEXT NOT NULL,
    issued_at   TIMESTAMPTZ NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Current state per order (write model, source of truth).
CREATE TABLE IF NOT EXISTS orders (
    order_id    TEXT PRIMARY KEY,
    state       TEXT NOT NULL,
    customer    TEXT,
    total_cents INTEGER,
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Line items (write model), written on PLACE.
CREATE TABLE IF NOT EXISTS order_items (
    order_id         TEXT NOT NULL REFERENCES orders(order_id),
    sku              TEXT NOT NULL,
    quantity         INTEGER NOT NULL,
    unit_price_cents INTEGER NOT NULL
);

-- Denormalized READ projection (served by order-query in lesson 7). Items as JSONB.
-- Maintained by order-processor; eventually consistent with the write model.
CREATE TABLE IF NOT EXISTS order_view (
    order_id    TEXT PRIMARY KEY,
    state       TEXT NOT NULL,
    customer    TEXT,
    total_cents INTEGER,
    items       JSONB NOT NULL DEFAULT '[]'::jsonb,
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
