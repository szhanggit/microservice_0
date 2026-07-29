#!/usr/bin/env bash
# Installs Grafana Alloy via Helm - the in-cluster collector shipping metrics,
# logs, and traces to Grafana Cloud (see apply-alloy-secret.sh for push
# credentials, kubernetes/alloy/values.yaml for the River config). No AWS
# IAM/IRSA involved - Amazon Managed Prometheus was dropped in favor of
# Grafana Cloud's own hosted Prometheus, since Grafana Cloud's Prometheus
# data source doesn't expose SigV4 auth on this account's plan.
#
# Usage: ./install-alloy.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

if ! kubectl get secret alloy-grafana-cloud-credentials -n default >/dev/null 2>&1; then
  echo "alloy-grafana-cloud-credentials Secret not found - creating with placeholder values."
  echo "Populate real Grafana Cloud credentials later via: just apply-alloy-secret"
  "$SCRIPT_DIR/apply-alloy-secret.sh"
fi

helm repo add grafana https://grafana.github.io/helm-charts
helm repo update grafana

helm upgrade --install alloy grafana/alloy -n default -f "$K8S_DIR/alloy/values.yaml"
