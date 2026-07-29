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

# The chart regenerates a fresh self-signed webhook CA/cert on every
# `helm upgrade --install` call, but an already-running controller pod keeps
# serving the OLD cert from memory (it doesn't hot-reload) - so the webhook
# config's caBundle and the pod's actual TLS cert disagree until the pod
# restarts, breaking every Service/Pod mutation cluster-wide with
# "x509: certificate signed by unknown authority" in the meantime. Force a
# restart on every run so this script stays safely re-runnable.
kubectl rollout restart deployment/aws-load-balancer-controller -n kube-system

# The controller registers a mutating webhook that intercepts every Service
# object cluster-wide (not just Ingress-related ones) as soon as the Helm
# release exists - but neither `helm upgrade --install` nor the restart above
# wait for the controller pods to actually be Ready and the webhook to have
# endpoints. Anything applied right after this (e.g.
# install-external-dns.sh creating its own Service) can hit
# "no endpoints available for service aws-load-balancer-webhook-service"
# without this wait.
kubectl rollout status deployment/aws-load-balancer-controller -n kube-system --timeout=180s
