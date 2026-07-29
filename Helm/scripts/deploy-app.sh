#!/usr/bin/env bash
# Applies the app layer to whatever cluster kubectl is currently pointed at,
# via the Helm chart in .. (Helm/). Image tags and the RDS ExternalName are
# passed in via --set, reading the real values from SSM Parameter Store, same
# source ./build-and-push-images.sh already uses. xray-daemon, the DB Secret,
# and the db-init schema reset stay outside Helm (see ../Chart.yaml's
# description for why) and are applied directly here, using the sibling
# scripts/manifests in this same directory/../xray/../db-init - this pipeline
# is fully self-contained under Helm/, independent of kubernetes/.
#
# Requires: helm, jq, aws cli, kubectl pointed at the target cluster (see
# ./update-kubeconfig.sh).
#
# Usage: ./deploy-app.sh <env> [tag]
#   tag defaults to the current commit's short SHA (same default
#   build-and-push-images.sh uses) - pass an explicit tag if you want to
#   deploy a different build instead.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HELM_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$HELM_DIR/.." && pwd)"

ENV="${1:?Usage: $0 <env> [tag]}"
TAG="${2:-$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || echo latest)}"
REGION="${AWS_REGION:-ca-central-1}"

REPO_URLS_JSON="$(aws ssm get-parameter --name "/microservice0/$ENV/ecr_repository_urls" --region "$REGION" --query 'Parameter.Value' --output text)"

DATAACCESS_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.userrepositoryservice'):$TAG"
MANAGEMENT_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.usermanagementservice'):$TAG"
GATEWAY_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.usermanagementgateway'):$TAG"
JOBMONITOR_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.jobmonitorservice'):$TAG"
MYSQL_EXTERNAL_NAME="$(aws ssm get-parameter --name "/microservice0/$ENV/db_instance_address" --region "$REGION" --query 'Parameter.Value' --output text)"

echo "dataaccess -> $DATAACCESS_IMAGE"
echo "management -> $MANAGEMENT_IMAGE"
echo "gateway    -> $GATEWAY_IMAGE"
echo "jobmonitor -> $JOBMONITOR_IMAGE"
echo "mysql (ExternalName) -> $MYSQL_EXTERNAL_NAME"

# Needed before apply-db-secret.sh/init-db.sh can run. The Helm release below
# also manages this same object (see ../templates/namespace.yaml) - Helm 3
# refuses to "adopt" a pre-existing resource that lacks its ownership
# metadata (fails with "invalid ownership metadata"), so stamp the same
# labels/annotations Helm itself would apply, letting it adopt this object
# instead of erroring on it.
kubectl create namespace microservice0 --dry-run=client -o yaml | kubectl apply -f -
kubectl label namespace microservice0 app.kubernetes.io/managed-by=Helm --overwrite
kubectl annotate namespace microservice0 \
  meta.helm.sh/release-name=microservice0 \
  meta.helm.sh/release-namespace=microservice0 \
  --overwrite

kubectl apply -f "$HELM_DIR/xray/"

"$SCRIPT_DIR/apply-db-secret.sh" "$ENV"

"$SCRIPT_DIR/init-db.sh" "$ENV"

helm upgrade --install microservice0 "$HELM_DIR" \
  --namespace microservice0 \
  --set images.dataaccess="$DATAACCESS_IMAGE" \
  --set images.management="$MANAGEMENT_IMAGE" \
  --set images.gateway="$GATEWAY_IMAGE" \
  --set images.jobmonitor="$JOBMONITOR_IMAGE" \
  --set mysql.externalName="$MYSQL_EXTERNAL_NAME" \
  --wait --timeout 5m

echo ""
echo "Deployed. Once the AWS Load Balancer Controller provisions the ALB:"
echo "  kubectl get ingress -n microservice0"
