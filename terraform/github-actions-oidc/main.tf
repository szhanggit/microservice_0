terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.39"
    }
  }
}

provider "aws" {
  region = var.region
}

data "aws_caller_identity" "current" {}

# GitHub's OIDC provider - one per AWS account. Lets GitHub Actions workflows
# assume the role below via short-lived tokens (no long-lived AWS
# credentials stored as GitHub secrets).
resource "aws_iam_openid_connect_provider" "github_actions" {
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]

  # GitHub's well-known root CA thumbprint, per AWS's official GitHub OIDC
  # setup guide - AWS validates the actual certificate chain itself, so this
  # rarely changes.
  thumbprint_list = ["6938fd4d98bab03faadb97b34396831e3780aea1"]
}

data "aws_iam_policy_document" "github_actions_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github_actions.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # StringLike (not StringEquals) - the trailing "*" is only a wildcard
    # under StringLike, scoping this to any branch/tag/workflow run in this
    # one repo without matching literally nothing. The owner/repo segments
    # must include their immutable "@ID" suffixes (see variables.tf's
    # github_owner_id/github_repo_id) - a plain "repo:${var.github_repo}:*"
    # never matches the real token and silently denies every assume-role
    # call (see CloudTrail; this cost real debugging time to track down).
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values = [
        "repo:${split("/", var.github_repo)[0]}@${var.github_owner_id}/${split("/", var.github_repo)[1]}@${var.github_repo_id}:*"
      ]
    }
  }
}

resource "aws_iam_role" "github_actions" {
  name               = var.role_name
  assume_role_policy = data.aws_iam_policy_document.github_actions_assume_role.json
}

# Scoped to exactly what Helm/scripts/*.sh already does locally - no broader.
# Actual kubectl-level access to the cluster comes from the EKS access entry
# ../modules/eks-cluster grants this role's ARN via additional_admin_role_arn
# (see ../main.tf), not from an IAM permission here - eks:DescribeCluster is
# only what `aws eks update-kubeconfig` needs to fetch the endpoint/CA.
data "aws_iam_policy_document" "github_actions_permissions" {
  statement {
    sid       = "EcrAuth"
    effect    = "Allow"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid    = "EcrPushPull"
    effect = "Allow"
    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:GetDownloadUrlForLayer",
      "ecr:BatchGetImage",
      "ecr:PutImage",
      "ecr:InitiateLayerUpload",
      "ecr:UploadLayerPart",
      "ecr:CompleteLayerUpload",
    ]
    resources = ["arn:aws:ecr:*:${data.aws_caller_identity.current.account_id}:repository/microservice0-*/*"]
  }

  statement {
    sid       = "EksDescribe"
    effect    = "Allow"
    actions   = ["eks:DescribeCluster"]
    resources = ["arn:aws:eks:*:${data.aws_caller_identity.current.account_id}:cluster/microservice0-*"]
  }

  statement {
    sid       = "SsmRead"
    effect    = "Allow"
    actions   = ["ssm:GetParameter"]
    resources = ["arn:aws:ssm:*:${data.aws_caller_identity.current.account_id}:parameter/microservice0/*"]
  }

  statement {
    sid       = "SecretsManagerRead"
    effect    = "Allow"
    actions   = ["secretsmanager:GetSecretValue"]
    resources = ["arn:aws:secretsmanager:*:${data.aws_caller_identity.current.account_id}:secret:microservice0/*"]
  }

  statement {
    sid       = "Route53ListZones"
    effect    = "Allow"
    actions   = ["route53:ListHostedZones", "route53:ListHostedZonesByName"]
    resources = ["*"] # Route53 list actions don't support resource-level scoping
  }

  statement {
    sid       = "Route53ManageEkslabZone"
    effect    = "Allow"
    actions   = ["route53:ListResourceRecordSets", "route53:ChangeResourceRecordSets"]
    resources = ["arn:aws:route53:::hostedzone/${var.route53_zone_id}"]
  }

  # Lets scripts/deploy-frontend.sh (Helm/scripts) sync the Angular build
  # into the bucket ../modules/frontend-cdn created.
  statement {
    sid    = "FrontendBucketReadWrite"
    effect = "Allow"
    actions = [
      "s3:ListBucket",
      "s3:PutObject",
      "s3:DeleteObject",
    ]
    resources = [
      "arn:aws:s3:::${var.frontend_bucket_pattern}",
      "arn:aws:s3:::${var.frontend_bucket_pattern}/*",
    ]
  }

  statement {
    sid       = "FrontendCloudFrontInvalidate"
    effect    = "Allow"
    actions   = ["cloudfront:CreateInvalidation"]
    resources = [for id in var.frontend_distribution_ids : "arn:aws:cloudfront::${data.aws_caller_identity.current.account_id}:distribution/${id}"]
  }
}

resource "aws_iam_policy" "github_actions" {
  name   = "${var.role_name}-permissions"
  policy = data.aws_iam_policy_document.github_actions_permissions.json
}

resource "aws_iam_role_policy_attachment" "github_actions" {
  role       = aws_iam_role.github_actions.name
  policy_arn = aws_iam_policy.github_actions.arn
}
