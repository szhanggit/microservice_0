#!/usr/bin/env bash
# Points your local kubectl at the EKS cluster the terraform/ pipeline
# created, reading the cluster name from SSM Parameter Store - no dependency
# on the Terraform CLI, this project's state, or S3 backend credentials.
#
# Usage: ./update-kubeconfig.sh <env>   (env = develop | staging | production)
set -euo pipefail

ENV="${1:?Usage: $0 <env>}"
REGION="${AWS_REGION:-ca-central-1}"

CLUSTER_NAME="$(aws ssm get-parameter --name "/microservice0/$ENV/cluster_name" --region "$REGION" --query 'Parameter.Value' --output text)"

aws eks --region "$REGION" update-kubeconfig --name "$CLUSTER_NAME"
