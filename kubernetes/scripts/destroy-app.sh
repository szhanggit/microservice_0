#!/usr/bin/env bash
# Tears down the app layer for one environment - run this BEFORE `just
# destroy` in ../terraform, or the ALB's security groups/ENIs can block
# Terraform from deleting the VPC.
#
# Captures the Ingress's ExternalDNS hostname *before* deleting anything,
# since it's needed by cleanup-dns.sh afterward - ExternalDNS runs with
# policy=upsert-only and will never delete its own Route53 records itself
# (see cleanup-dns.sh), so this script does that cleanup directly instead of
# just uninstalling ExternalDNS and hoping it reconciled in time.
#
# Usage: ./destroy-app.sh <env>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

ENV="${1:?Usage: $0 <env>}"

HOSTNAME="$(kubectl get ingress -n microservice0 -o jsonpath='{.items[0].metadata.annotations.external-dns\.alpha\.kubernetes\.io/hostname}' 2>/dev/null || true)"

kubectl delete -f "$K8S_DIR/ingress/" --ignore-not-found
kubectl delete -f "$K8S_DIR/namespace/" --ignore-not-found

if [ -n "$HOSTNAME" ]; then
  "$SCRIPT_DIR/cleanup-dns.sh" "$ENV" "$HOSTNAME"
else
  echo "No Ingress hostname found (already deleted?) - skipping DNS cleanup."
fi

helm uninstall aws-load-balancer-controller -n kube-system || true
helm uninstall external-dns -n default || true
