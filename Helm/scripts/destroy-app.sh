#!/usr/bin/env bash
# Tears down the app layer for one environment - run this BEFORE `just
# destroy` in ../../terraform, or the ALB's security groups/ENIs can block
# Terraform from deleting the VPC.
#
# Captures the Ingress's ExternalDNS hostname *before* deleting anything,
# since it's needed by cleanup-dns.sh afterward - ExternalDNS runs with
# policy=upsert-only and will never delete its own Route53 records itself
# (see ./cleanup-dns.sh), so this script does that cleanup directly instead
# of just uninstalling ExternalDNS and hoping it reconciled in time.
#
# Fully self-contained under Helm/, independent of kubernetes/.
#
# Usage: ./destroy-app.sh <env>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HELM_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

ENV="${1:?Usage: $0 <env>}"

HOSTNAME="$(kubectl get ingress -n microservice0 -o jsonpath='{.items[0].metadata.annotations.external-dns\.alpha\.kubernetes\.io/hostname}' 2>/dev/null || true)"

# Deletes the Ingress, namespace, and every other resource in the Helm
# release (Helm/) in one shot. --wait is required here: without it, `helm
# uninstall` returns almost immediately without waiting for the Ingress's
# ingress.k8s.aws/resources finalizer to actually clear (the ALB controller
# needs time to delete the real AWS ALB first) - the aws-load-balancer-
# controller uninstall below would then race ahead and kill the controller
# before it finishes, permanently orphaning that finalizer and leaving the
# namespace stuck in Terminating forever (only fixable afterward by manually
# patching the finalizer off once the real ALB is confirmed gone - a pain
# best avoided by waiting properly here).
helm uninstall microservice0 -n microservice0 --wait --timeout 5m || true

# Lives in the default namespace (IRSA trust policy requires it), so the
# microservice0 release above (which only touches its own namespace) never
# cleans this up.
kubectl delete -f "$HELM_DIR/xray/" --ignore-not-found

if [ -n "$HOSTNAME" ]; then
  "$SCRIPT_DIR/cleanup-dns.sh" "$ENV" "$HOSTNAME"
else
  echo "No Ingress hostname found (already deleted?) - skipping DNS cleanup."
fi

helm uninstall aws-load-balancer-controller -n kube-system || true
helm uninstall external-dns -n default || true
helm uninstall cluster-autoscaler -n kube-system || true
helm uninstall alloy -n default || true
