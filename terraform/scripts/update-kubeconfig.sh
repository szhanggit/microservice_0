#!/usr/bin/env bash
# Points your local kubectl at the EKS cluster Terraform just created.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TF_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

REGION="$(terraform -chdir="$TF_DIR" output -raw region)"
CLUSTER_NAME="$(terraform -chdir="$TF_DIR" output -raw cluster_name)"

aws eks --region "$REGION" update-kubeconfig --name "$CLUSTER_NAME"
