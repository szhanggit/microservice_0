#!/usr/bin/env bash
# Builds the Angular app (components/UserManagementWeb) for production and
# publishes it to the S3 bucket + CloudFront distribution
# terraform/modules/frontend-cdn created. Bucket name/distribution ID are
# read from SSM Parameter Store, same cross-pipeline handoff pattern as
# deploy-app.sh - no dependency on the Terraform CLI, this project's state,
# or S3 backend credentials. Fails fast (set -euo pipefail) if those SSM
# parameters don't exist yet, i.e. frontend-cdn hasn't been applied for this
# environment - run `terraform apply` there first.
#
# Requires: node/npm, aws cli, kubectl not needed - this doesn't touch the
# EKS cluster at all.
#
# Usage: ./deploy-frontend.sh <env>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HELM_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$HELM_DIR/.." && pwd)"
WEB_DIR="$REPO_ROOT/components/UserManagementWeb"

ENV="${1:?Usage: $0 <env>}"
REGION="${AWS_REGION:-ca-central-1}"

BUCKET_NAME="$(aws ssm get-parameter --name "/microservice0/$ENV/frontend_bucket_name" --region "$REGION" --query 'Parameter.Value' --output text)"
DISTRIBUTION_ID="$(aws ssm get-parameter --name "/microservice0/$ENV/frontend_distribution_id" --region "$REGION" --query 'Parameter.Value' --output text)"

echo "Building Angular app for production..."
cd "$WEB_DIR"
npm ci
npm run build -- --configuration production

BUILD_DIR="$WEB_DIR/dist/UserManagementWeb/browser"

echo "Syncing $BUILD_DIR -> s3://$BUCKET_NAME ..."
# Hashed filenames (main-<hash>.js, chunk-<hash>.js, styles-<hash>.css) are
# content-addressed - safe to cache for a year. index.html is the one file
# whose content changes without its name changing, so it's uploaded
# separately below with no caching, otherwise a browser could keep serving
# an old index.html that references since-deleted (--delete'd) hashed assets.
aws s3 sync "$BUILD_DIR" "s3://$BUCKET_NAME" \
  --delete --exclude index.html \
  --cache-control "public, max-age=31536000, immutable"

aws s3 cp "$BUILD_DIR/index.html" "s3://$BUCKET_NAME/index.html" \
  --cache-control "no-cache, no-store, must-revalidate"

echo "Invalidating CloudFront distribution $DISTRIBUTION_ID ..."
aws cloudfront create-invalidation --distribution-id "$DISTRIBUTION_ID" --paths "/*" >/dev/null

echo ""
echo "Deployed."
