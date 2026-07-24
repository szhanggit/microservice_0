#!/usr/bin/env bash
# Installs the AWS Load Balancer Controller via Helm. Terraform (module
# eks-alb-controller) only provisions its IAM role and IRSA-annotated
# ServiceAccount - the controller itself isn't a first-party Terraform
# resource, so it's installed here instead, same as the reference project's
# justfile does it.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TF_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

REGION="$(terraform -chdir="$TF_DIR" output -raw region)"
CLUSTER_NAME="$(terraform -chdir="$TF_DIR" output -raw cluster_name)"
VPC_ID="$(terraform -chdir="$TF_DIR" output -raw vpc_id)"

helm repo add eks https://aws.github.io/eks-charts
helm repo update eks

helm upgrade --install aws-load-balancer-controller eks/aws-load-balancer-controller \
  -n kube-system \
  --set clusterName="$CLUSTER_NAME" \
  --set serviceAccount.create=false \
  --set serviceAccount.name=aws-load-balancer-controller \
  --set region="$REGION" \
  --set vpcId="$VPC_ID" \
  --set image.repository=public.ecr.aws/eks/aws-load-balancer-controller
