#!/usr/bin/env bash
# Creates/updates the Secret holding Grafana Cloud Prometheus/Loki/Tempo push
# credentials, consumed by Alloy (see ../alloy/values.yaml). Unlike
# apply-db-secret.sh, these values don't come from AWS - Grafana Cloud is an
# external SaaS product - so provide the three passwords yourself first,
# either:
#   - persist them in ./.env (gitignored, same pattern as
#     components/UserRepositoryService/src/.env) - copy .env.example to .env
#     and fill in real values, this script sources it automatically if
#     present; or
#   - export them directly in your shell for a one-off run:
#       export PROM_PASSWORD=... LOKI_PASSWORD=... TEMPO_PASSWORD=...
#
# Fails loudly if any of the three passwords are missing, rather than
# silently applying a placeholder - a placeholder used to let this "succeed"
# while leaving Alloy authenticating with garbage, a failure mode that only
# ever surfaced later as a confusing 401 buried in Alloy's own pod logs.
# Better to fail here, immediately, with a clear message.
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

missing=()
[ -z "${PROM_PASSWORD:-}" ] && missing+=("PROM_PASSWORD")
[ -z "${LOKI_PASSWORD:-}" ] && missing+=("LOKI_PASSWORD")
[ -z "${TEMPO_PASSWORD:-}" ] && missing+=("TEMPO_PASSWORD")

if [ "${#missing[@]}" -gt 0 ]; then
  echo "Missing required credential(s): ${missing[*]}" >&2
  echo "Set them in $SCRIPT_DIR/.env (copy from .env.example) or export them before running this script." >&2
  exit 1
fi

kubectl create secret generic alloy-grafana-cloud-credentials \
  --namespace default \
  --from-literal=PROM_REMOTE_WRITE_URL="${PROM_REMOTE_WRITE_URL:-https://prometheus-prod-32-prod-ca-east-0.grafana.net/api/prom/push}" \
  --from-literal=PROM_USERNAME="${PROM_USERNAME:-3403370}" \
  --from-literal=PROM_PASSWORD="$PROM_PASSWORD" \
  --from-literal=LOKI_PUSH_URL="${LOKI_PUSH_URL:-https://logs-prod-018.grafana.net/loki/api/v1/push}" \
  --from-literal=LOKI_USERNAME="${LOKI_USERNAME:-1697360}" \
  --from-literal=LOKI_PASSWORD="$LOKI_PASSWORD" \
  --from-literal=TEMPO_OTLP_ENDPOINT="${TEMPO_OTLP_ENDPOINT:-https://tempo-prod-13-prod-ca-east-0.grafana.net:443}" \
  --from-literal=TEMPO_USERNAME="${TEMPO_USERNAME:-1691661}" \
  --from-literal=TEMPO_PASSWORD="$TEMPO_PASSWORD" \
  --dry-run=client -o yaml | kubectl apply -f -
