#!/usr/bin/env bash
# Feeds the RDS connection string - written by terraform/ to Secrets Manager
# (see ssm-outputs.tf) - into the dataaccess-db-credentials Secret. No
# dependency on the Terraform CLI, this project's state, or S3 backend
# credentials.
#
# The namespace must already exist first: kubectl apply -f namespace/
#
# Usage: ./apply-db-secret.sh <env>
set -euo pipefail

ENV="${1:?Usage: $0 <env>}"
REGION="${AWS_REGION:-ca-central-1}"

CONNECTION_STRING="$(aws secretsmanager get-secret-value \
  --secret-id "microservice0/$ENV/db-connection-string" \
  --region "$REGION" \
  --query SecretString --output text)"

kubectl create secret generic dataaccess-db-credentials \
  --namespace microservice0 \
  --from-literal=ConnectionStrings__MySql="$CONNECTION_STRING" \
  --dry-run=client -o yaml | kubectl apply -f -
