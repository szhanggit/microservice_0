#!/usr/bin/env bash
# Creates/updates the Secret holding Grafana Cloud Loki/Tempo push
# credentials, consumed by Alloy (see kubernetes/alloy/values.yaml). Unlike
# apply-db-secret.sh, these values don't come from AWS - Grafana Cloud is an
# external SaaS product - so export them yourself first:
#   export LOKI_PUSH_URL=... LOKI_USERNAME=... LOKI_PASSWORD=...
#   export TEMPO_OTLP_ENDPOINT=... TEMPO_USERNAME=... TEMPO_PASSWORD=...
# Run with nothing exported and it applies obvious placeholders instead, so
# Alloy can still start - its logs/traces pipelines just fail auth until you
# re-run this with real values. Safe to re-run: only overwrites what you pass.
#
# Usage: ./apply-alloy-secret.sh
set -euo pipefail

kubectl create secret generic alloy-grafana-cloud-credentials \
  --namespace default \
  --from-literal=LOKI_PUSH_URL="${LOKI_PUSH_URL:-https://REPLACE_ME.grafana.net/loki/api/v1/push}" \
  --from-literal=LOKI_USERNAME="${LOKI_USERNAME:-REPLACE_ME}" \
  --from-literal=LOKI_PASSWORD="${LOKI_PASSWORD:-REPLACE_ME}" \
  --from-literal=TEMPO_OTLP_ENDPOINT="${TEMPO_OTLP_ENDPOINT:-https://REPLACE_ME.grafana.net:443}" \
  --from-literal=TEMPO_USERNAME="${TEMPO_USERNAME:-REPLACE_ME}" \
  --from-literal=TEMPO_PASSWORD="${TEMPO_PASSWORD:-REPLACE_ME}" \
  --dry-run=client -o yaml | kubectl apply -f -
