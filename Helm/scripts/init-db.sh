#!/usr/bin/env bash
# Resets the schema via a Kubernetes Job (../db-init/job.yaml): drops the
# database, then replays each component's create_tables.sql
# (UserRepositoryService's creates the database if missing plus UserInfo and
# re-inserts the seed rows; JobMonitorService's adds the JobMonitor table).
# The Job pulls DB credentials from the dataaccess-db-credentials Secret
# already in the cluster (populated by apply-db-secret.sh), so this script
# itself needs no AWS/Secrets Manager access.
#
# Destructive by design - every deploy-all wipes and reseeds the schema. Fine
# for this learning/portfolio environment; do not wire this into a real
# production pipeline without gating it behind an explicit flag.
#
# Usage: ./init-db.sh <env>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HELM_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$HELM_DIR/.." && pwd)"

# env isn't used directly (see above) - kept as a required argument so this
# script's interface matches its sibling scripts.
: "${1:?Usage: $0 <env>}"

USERREPOSITORY_SQL="$REPO_ROOT/components/UserRepositoryService/resources/mysql/create_tables.sql"
JOBMONITOR_SQL="$REPO_ROOT/components/JobMonitorService/resources/mysql/create_tables.sql"

echo "Publishing db-init-scripts ConfigMap..."
kubectl create configmap db-init-scripts \
  --namespace microservice0 \
  --from-file="01-userrepositoryservice.sql=$USERREPOSITORY_SQL" \
  --from-file="02-jobmonitorservice.sql=$JOBMONITOR_SQL" \
  --dry-run=client -o yaml | kubectl apply -f -

# Job specs are immutable - delete any previous run before applying a new one.
kubectl delete job db-init --namespace microservice0 --ignore-not-found

kubectl apply -f "$HELM_DIR/db-init/job.yaml"

echo "Waiting for db-init Job to complete..."
if ! kubectl wait --for=condition=complete job/db-init --namespace microservice0 --timeout=120s; then
  echo "db-init Job did not complete successfully - logs:"
  kubectl logs job/db-init --namespace microservice0 || true
  exit 1
fi

kubectl logs job/db-init --namespace microservice0
echo "Schema reset complete."
