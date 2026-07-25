#!/usr/bin/env bash
# Builds each service's production Dockerfile and pushes it to the ECR
# repository terraform/ created for it, reading repo URLs from SSM Parameter
# Store (see ssm-outputs.tf) - no dependency on the Terraform CLI, this
# project's state, or S3 backend credentials.
# Requires: aws cli, docker, jq.
#
# Usage: ./build-and-push-images.sh <env> [tag]
#   env defaults are develop | staging | production (must match what
#   terraform/ was applied with). tag defaults to the short git SHA, falling
#   back to "latest".
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

ENV="${1:?Usage: $0 <env> [tag]}"
TAG="${2:-$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || echo latest)}"
REGION="${AWS_REGION:-ca-central-1}"

echo "Reading ECR repository URLs from SSM Parameter Store..."
REPO_URLS_JSON="$(aws ssm get-parameter --name "/microservice0/$ENV/ecr_repository_urls" --region "$REGION" --query 'Parameter.Value' --output text)"

ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
REGISTRY="$ACCOUNT_ID.dkr.ecr.$REGION.amazonaws.com"

echo "Logging in to $REGISTRY..."
aws ecr get-login-password --region "$REGION" | docker login --username AWS --password-stdin "$REGISTRY"

# service name -> Dockerfile path (relative to repo root), matching each
# component's docker-compose.yml dockerfile: entry exactly.
declare -A DOCKERFILES=(
  [userrepositoryservice]="components/UserRepositoryService/src/private/UserRepositoryService/Dockerfile"
  [usermanagementservice]="components/UserManagementService/src/private/UserManagementService/Dockerfile"
  [usermanagementgateway]="components/UserManagementGateway/src/private/UserManagementGateway/Dockerfile"
)

for service in "${!DOCKERFILES[@]}"; do
  image_url="$(echo "$REPO_URLS_JSON" | jq -r --arg svc "$service" '.[$svc]')"
  dockerfile="$REPO_ROOT/${DOCKERFILES[$service]}"

  echo ""
  echo "=== $service -> $image_url:$TAG ==="
  docker build -f "$dockerfile" -t "$image_url:$TAG" -t "$image_url:latest" "$REPO_ROOT"
  docker push "$image_url:$TAG"
  docker push "$image_url:latest"
done

echo ""
echo "All images pushed with tag '$TAG' (and 'latest')."
