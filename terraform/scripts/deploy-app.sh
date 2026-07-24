#!/usr/bin/env bash
# Applies kubernetes/ to whatever cluster kubectl is currently pointed at,
# substituting the ECR image placeholders in the three Deployments with the
# real repository URLs from Terraform state. Requires: envsubst (gettext),
# jq, kubectl pointed at the target cluster (see update-kubeconfig.sh).
#
# Usage: ./deploy-app.sh [tag]
#   tag defaults to "latest" - pass the same tag build-and-push-images.sh used
#   if you want to deploy a specific build instead.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TF_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$TF_DIR/.." && pwd)"
K8S_DIR="$REPO_ROOT/kubernetes"

TAG="${1:-latest}"
REPO_URLS_JSON="$(terraform -chdir="$TF_DIR" output -json ecr_repository_urls)"

export DATAACCESS_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.userrepositoryservice'):$TAG"
export MANAGEMENT_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.usermanagementservice'):$TAG"
export GATEWAY_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.usermanagementgateway'):$TAG"

echo "dataaccess -> $DATAACCESS_IMAGE"
echo "management -> $MANAGEMENT_IMAGE"
echo "gateway    -> $GATEWAY_IMAGE"

kubectl apply -f "$K8S_DIR/namespace/"

"$SCRIPT_DIR/apply-db-secret.sh"

envsubst '${DATAACCESS_IMAGE}' < "$K8S_DIR/dataaccess/deployment.yaml" | kubectl apply -f -
kubectl apply -f "$K8S_DIR/dataaccess/configmap.yaml" -f "$K8S_DIR/dataaccess/service.yaml" \
  -f "$K8S_DIR/dataaccess/hpa.yaml" -f "$K8S_DIR/dataaccess/pdb.yaml"

envsubst '${MANAGEMENT_IMAGE}' < "$K8S_DIR/management/deployment.yaml" | kubectl apply -f -
kubectl apply -f "$K8S_DIR/management/configmap.yaml" -f "$K8S_DIR/management/service.yaml" \
  -f "$K8S_DIR/management/hpa.yaml" -f "$K8S_DIR/management/pdb.yaml"

envsubst '${GATEWAY_IMAGE}' < "$K8S_DIR/gateway/deployment.yaml" | kubectl apply -f -
kubectl apply -f "$K8S_DIR/gateway/configmap.yaml" -f "$K8S_DIR/gateway/service.yaml" \
  -f "$K8S_DIR/gateway/hpa.yaml" -f "$K8S_DIR/gateway/pdb.yaml"

kubectl apply -f "$K8S_DIR/ingress/"

echo ""
echo "Deployed. Once the AWS Load Balancer Controller provisions the ALB:"
echo "  kubectl get ingress -n microservice0"
