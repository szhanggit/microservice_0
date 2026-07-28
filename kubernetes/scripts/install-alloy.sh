#!/usr/bin/env bash
# Installs Grafana Alloy via Helm - the in-cluster collector for three
# backends: Amazon Managed Prometheus (metrics, remote_write with sigv4 via
# the amp-remote-write IRSA ServiceAccount terraform/modules/eks-amp already
# created), and Grafana Cloud's hosted Loki/Tempo (logs/traces - no AWS
# equivalent exists for those, see apply-alloy-secret.sh). Reads cluster
# name/region and the AMP endpoint from SSM Parameter Store - no dependency on
# the Terraform CLI, this project's state, or S3 backend credentials.
#
# Usage: ./install-alloy.sh <env>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

ENV="${1:?Usage: $0 <env>}"
REGION="${AWS_REGION:-ca-central-1}"
SSM_PREFIX="/microservice0/$ENV"

AMP_ENDPOINT="$(aws ssm get-parameter --name "$SSM_PREFIX/amp_prometheus_endpoint" --region "$REGION" --query 'Parameter.Value' --output text)"

export AMP_REMOTE_WRITE_URL="${AMP_ENDPOINT}api/v1/remote_write"
export REGION

if ! kubectl get secret alloy-grafana-cloud-credentials -n default >/dev/null 2>&1; then
  echo "alloy-grafana-cloud-credentials Secret not found - creating with placeholder values."
  echo "Populate real Grafana Cloud credentials later via: just apply-alloy-secret"
  "$SCRIPT_DIR/apply-alloy-secret.sh"
fi

helm repo add grafana https://grafana.github.io/helm-charts
helm repo update grafana

envsubst '${AMP_REMOTE_WRITE_URL} ${REGION}' < "$K8S_DIR/alloy/values.yaml" \
  | helm upgrade --install alloy grafana/alloy -n default -f -
