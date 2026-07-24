#!/usr/bin/env bash
# Feeds Terraform's RDS output into the dataaccess-db-credentials Secret,
# replacing the hand-typed placeholder in kubernetes/mysql/secret.yaml (see
# that file's own comment - this script is what it was asking for).
#
# The namespace must already exist first: kubectl apply -f kubernetes/namespace/
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TF_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

CONNECTION_STRING="$(terraform -chdir="$TF_DIR" output -raw db_connection_string)"

kubectl create secret generic dataaccess-db-credentials \
  --namespace microservice0 \
  --from-literal=ConnectionStrings__MySql="$CONNECTION_STRING" \
  --dry-run=client -o yaml | kubectl apply -f -
