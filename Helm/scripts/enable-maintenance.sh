#!/usr/bin/env bash
# Replaces the deployed Angular app's index.html with a static "in
# maintenance" page and invalidates CloudFront - run after tearing down the
# backend (see destroy.yml) so usermgn.ekslab.xyz shows a maintenance
# notice instead of a broken app with every API call failing.
#
# frontend-cdn's custom_error_response (403/404 -> index.html, added for SPA
# routing) means this single file swap covers every path on the site, not
# just "/" - no CloudFront/Terraform changes needed.
#
# Self-healing: the next deploy-frontend.sh run overwrites this maintenance
# index.html with the real app again (see its own `aws s3 cp .../index.html`
# step) - no separate "exit maintenance mode" script needed.
#
# Bucket name/distribution ID are read from SSM Parameter Store, same
# cross-pipeline handoff pattern as deploy-frontend.sh - fails fast
# (set -euo pipefail) if those parameters don't exist yet, consistent with
# the rest of destroy.yml already failing hard (e.g. update-kubeconfig) when
# there's nothing to destroy for this environment.
#
# Usage: ./enable-maintenance.sh <env>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HELM_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

ENV="${1:?Usage: $0 <env>}"
REGION="${AWS_REGION:-ca-central-1}"

BUCKET_NAME="$(aws ssm get-parameter --name "/microservice0/$ENV/frontend_bucket_name" --region "$REGION" --query 'Parameter.Value' --output text)"
DISTRIBUTION_ID="$(aws ssm get-parameter --name "/microservice0/$ENV/frontend_distribution_id" --region "$REGION" --query 'Parameter.Value' --output text)"

echo "Uploading maintenance page to s3://$BUCKET_NAME ..."
aws s3 cp "$HELM_DIR/maintenance/index.html" "s3://$BUCKET_NAME/index.html" \
  --cache-control "no-cache, no-store, must-revalidate"

echo "Invalidating CloudFront distribution $DISTRIBUTION_ID ..."
aws cloudfront create-invalidation --distribution-id "$DISTRIBUTION_ID" --paths "/*" >/dev/null

echo ""
echo "Done - usermgn.ekslab.xyz now shows the maintenance page."
