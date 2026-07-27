#!/usr/bin/env bash
# Applies kubernetes/ to whatever cluster kubectl is currently pointed at,
# substituting the ECR image placeholders in the three Deployments with the
# real repository URLs from SSM Parameter Store. No dependency on the
# Terraform CLI, this project's state, or S3 backend credentials. Requires:
# envsubst (gettext), jq, aws cli, kubectl pointed at the target cluster (see
# update-kubeconfig.sh).
#
# Usage: ./deploy-app.sh <env> [tag]
#   tag defaults to the current commit's short SHA (same default
#   build-and-push-images.sh uses) - pass an explicit tag if you want to
#   deploy a different build instead.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$K8S_DIR/.." && pwd)"

ENV="${1:?Usage: $0 <env> [tag]}"
TAG="${2:-$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || echo latest)}"
REGION="${AWS_REGION:-ca-central-1}"

REPO_URLS_JSON="$(aws ssm get-parameter --name "/microservice0/$ENV/ecr_repository_urls" --region "$REGION" --query 'Parameter.Value' --output text)"

export DATAACCESS_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.userrepositoryservice'):$TAG"
export MANAGEMENT_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.usermanagementservice'):$TAG"
export GATEWAY_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.usermanagementgateway'):$TAG"
export JOBMONITOR_IMAGE="$(echo "$REPO_URLS_JSON" | jq -r '.jobmonitorservice'):$TAG"
export MYSQL_EXTERNAL_NAME="$(aws ssm get-parameter --name "/microservice0/$ENV/db_instance_address" --region "$REGION" --query 'Parameter.Value' --output text)"

echo "dataaccess -> $DATAACCESS_IMAGE"
echo "management -> $MANAGEMENT_IMAGE"
echo "gateway    -> $GATEWAY_IMAGE"
echo "jobmonitor -> $JOBMONITOR_IMAGE"
echo "mysql (ExternalName) -> $MYSQL_EXTERNAL_NAME"

kubectl apply -f "$K8S_DIR/namespace/"

"$SCRIPT_DIR/apply-db-secret.sh" "$ENV"

"$SCRIPT_DIR/init-db.sh" "$ENV"

envsubst '${MYSQL_EXTERNAL_NAME}' < "$K8S_DIR/dataaccess/externalname.yaml" | kubectl apply -f -

envsubst '${DATAACCESS_IMAGE}' < "$K8S_DIR/dataaccess/deployment.yaml" | kubectl apply -f -
kubectl apply -f "$K8S_DIR/dataaccess/configmap.yaml" -f "$K8S_DIR/dataaccess/service.yaml" \
  -f "$K8S_DIR/dataaccess/hpa.yaml" -f "$K8S_DIR/dataaccess/pdb.yaml"

envsubst '${MANAGEMENT_IMAGE}' < "$K8S_DIR/management/deployment.yaml" | kubectl apply -f -
kubectl apply -f "$K8S_DIR/management/configmap.yaml" -f "$K8S_DIR/management/service.yaml" \
  -f "$K8S_DIR/management/hpa.yaml" -f "$K8S_DIR/management/pdb.yaml"

envsubst '${GATEWAY_IMAGE}' < "$K8S_DIR/gateway/deployment.yaml" | kubectl apply -f -
kubectl apply -f "$K8S_DIR/gateway/configmap.yaml" -f "$K8S_DIR/gateway/service.yaml" \
  -f "$K8S_DIR/gateway/hpa.yaml" -f "$K8S_DIR/gateway/pdb.yaml"

kubectl apply -f "$K8S_DIR/jobmonitor/configmap.yaml"
envsubst '${JOBMONITOR_IMAGE}' < "$K8S_DIR/jobmonitor/cronjob.yaml" | kubectl apply -f -

kubectl apply -f "$K8S_DIR/ingress/"

echo ""
echo "Deployed. Once the AWS Load Balancer Controller provisions the ALB:"
echo "  kubectl get ingress -n microservice0"
