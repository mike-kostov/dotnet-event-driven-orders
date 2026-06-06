#!/usr/bin/env bash
# Places an order, drives it to DELIVERED, then queries it back via order-query.
# Override hosts if you changed ports in .env, e.g.:
#   HOST_INGEST=localhost:8088 HOST_QUERY=localhost:8089 bash scripts/seed.sh
set -euo pipefail

HOST_INGEST=${HOST_INGEST:-localhost:8080}
HOST_QUERY=${HOST_QUERY:-localhost:8081}

OID=$(curl -s -X POST "$HOST_INGEST/orders" -H 'content-type: application/json' \
  -d '{"customer":"alice@example.com","items":[{"sku":"MARGHERITA","quantity":2,"unitPriceCents":1200}]}' \
  | sed 's/.*"orderId":"//;s/".*//')
echo "placed order: $OID"

for t in confirm prepare dispatch deliver; do
  curl -s -o /dev/null -X POST "$HOST_INGEST/orders/$OID/$t"
  sleep 1
done

echo "querying $HOST_QUERY/orders/$OID :"
curl -s "$HOST_QUERY/orders/$OID"; echo
