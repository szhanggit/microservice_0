#!/usr/bin/env bash
# Installs Cluster Autoscaler via Helm. terraform/ (module
# eks-cluster-autoscaler) only provisions its IAM role and IRSA-annotated
# ServiceAccount - the autoscaler itself isn't a first-party Terraform
# resource, so it's installed here instead. Reads cluster name from SSM
# Parameter Store - no dependency on the Terraform CLI, this project's state,
# or S3 backend credentials.
#
# Usage: ./install-cluster-autoscaler.sh <env>
set -euo pipefail

ENV="${1:?Usage: $0 <env>}"
REGION="${AWS_REGION:-ca-central-1}"
SSM_PREFIX="/microservice0/$ENV"

CLUSTER_NAME="$(aws ssm get-parameter --name "$SSM_PREFIX/cluster_name" --region "$REGION" --query 'Parameter.Value' --output text)"

helm repo add autoscaler https://kubernetes.github.io/autoscaler
helm repo update autoscaler

# Cluster Autoscaler must track the EKS control plane's minor version (see
# https://github.com/kubernetes/autoscaler/blob/master/cluster-autoscaler/README.md#releases).
# Chart 9.44.0 -> CA 1.31.0, matching this cluster's Kubernetes 1.31. Installing
# an unpinned/newer CA against an older control plane isn't just a version
# mismatch warning - a CA built for a newer minor watches APIs (e.g. the
# resource.k8s.io Dynamic Resource Allocation types) that don't exist yet on
# this control plane, and its informer cache sync hangs waiting on them,
# silently blocking the entire autoscaling loop from ever running (no
# scale-up logs at all, even with pods stuck Pending indefinitely).
CHART_VERSION="9.44.0"

helm upgrade --install cluster-autoscaler autoscaler/cluster-autoscaler \
  -n kube-system \
  --version "$CHART_VERSION" \
  --set autoDiscovery.clusterName="$CLUSTER_NAME" \
  --set awsRegion="$REGION" \
  --set rbac.serviceAccount.create=false \
  --set rbac.serviceAccount.name=cluster-autoscaler \
  --set extraArgs.balance-similar-node-groups=true \
  --set extraArgs.skip-nodes-with-local-storage=false
