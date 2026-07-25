#!/usr/bin/env bash
# Installs the AWS Load Balancer Controller via Helm. terraform/ (module
# eks-alb-controller) only provisions its IAM role and IRSA-annotated
# ServiceAccount - the controller itself isn't a first-party Terraform
# resource, so it's installed here instead. Reads cluster name/VPC ID from
# SSM Parameter Store - no dependency on the Terraform CLI, this project's
# state, or S3 backend credentials.
#
# Usage: ./install-alb-controller.sh <env>
set -euo pipefail

ENV="${1:?Usage: $0 <env>}"
REGION="${AWS_REGION:-ca-central-1}"
SSM_PREFIX="/microservice0/$ENV"

CLUSTER_NAME="$(aws ssm get-parameter --name "$SSM_PREFIX/cluster_name" --region "$REGION" --query 'Parameter.Value' --output text)"
VPC_ID="$(aws ssm get-parameter --name "$SSM_PREFIX/vpc_id" --region "$REGION" --query 'Parameter.Value' --output text)"

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
