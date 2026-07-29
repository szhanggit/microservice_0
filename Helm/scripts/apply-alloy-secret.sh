#!/usr/bin/env bash
# Creates/updates the Secret holding Grafana Cloud Prometheus/Loki/Tempo push
# credentials, consumed by Alloy (see ../alloy/values.yaml). Unlike
# apply-db-secret.sh, these values don't come from AWS - Grafana Cloud is an
# external SaaS product - so provide them yourself first, either:
#   - persist them in ./.env (gitignored, same pattern as
#     components/UserRepositoryService/src/.env) - copy .env.example to .env
#     and fill in real values, this script sources it automatically if
#     present; or
#   - export them directly in your shell for a one-off run:
#       export PROM_REMOTE_WRITE_URL=... PROM_USERNAME=... PROM_PASSWORD=...
#       export LOKI_PUSH_URL=... LOKI_USERNAME=... LOKI_PASSWORD=...
#       export TEMPO_OTLP_ENDPOINT=... TEMPO_USERNAME=... TEMPO_PASSWORD=...
# Run with nothing exported and no .env present and it applies obvious
# placeholders instead, so Alloy can still start - its metrics/logs/traces
# pipelines just fail auth until you re-run this with real values. Safe to
# re-run: only overwrites what you pass.
#
# Usage: ./apply-alloy-secret.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -f "$SCRIPT_DIR/.env" ]; then
  set -a
  # shellcheck source=/dev/null
  source "$SCRIPT_DIR/.env"
  set +a
fi

kubectl create secret generic alloy-grafana-cloud-credentials \
  --namespace default \
  --from-literal=PROM_REMOTE_WRITE_URL="${PROM_REMOTE_WRITE_URL:-https://prometheus-prod-32-prod-ca-east-0.grafana.net/api/prom/push}" \
  --from-literal=PROM_USERNAME="${PROM_USERNAME:-3403370}" \
  --from-literal=PROM_PASSWORD="${PROM_PASSWORD:-<sensitive>}" \
  --from-literal=LOKI_PUSH_URL="${LOKI_PUSH_URL:-https://logs-prod-018.grafana.net/loki/api/v1/push}" \
  --from-literal=LOKI_USERNAME="${LOKI_USERNAME:-1697360}" \
  --from-literal=LOKI_PASSWORD="${LOKI_PASSWORD:-<sensitive>}" \
  --from-literal=TEMPO_OTLP_ENDPOINT="${TEMPO_OTLP_ENDPOINT:-https://tempo-prod-13-prod-ca-east-0.grafana.net:443}" \
  --from-literal=TEMPO_USERNAME="${TEMPO_USERNAME:-1691661}" \
  --from-literal=TEMPO_PASSWORD="${TEMPO_PASSWORD:-<sensitive>}" \
  --dry-run=client -o yaml | kubectl apply -f -
