# Amazon Managed Prometheus (AMP) workspace, plus the IRSA role/service account
# a future in-cluster metrics scraper (e.g. an ADOT collector) would use to
# remote-write into it. No scraper is deployed yet - see kubernetes/xray/ for
# the equivalent pattern already completed for X-Ray, this module is the same
# kind of scaffold, one step earlier.
resource "aws_prometheus_workspace" "this" {
  alias = "${var.cluster_name}-amp"
}

data "aws_iam_policy_document" "amp_remote_write_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [var.oidc_provider_arn]
    }

    condition {
      test     = "StringEquals"
      variable = "${replace(var.oidc_provider_url, "https://", "")}:sub"
      values   = ["system:serviceaccount:default:amp-remote-write"]
    }

    condition {
      test     = "StringEquals"
      variable = "${replace(var.oidc_provider_url, "https://", "")}:aud"
      values   = ["sts.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "amp_remote_write" {
  name               = "${var.cluster_name}-amp-remote-write-role"
  assume_role_policy = data.aws_iam_policy_document.amp_remote_write_assume_role.json
}

resource "aws_iam_role_policy_attachment" "amp_remote_write" {
  role       = aws_iam_role.amp_remote_write.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonPrometheusRemoteWriteAccess"
}

resource "kubernetes_service_account" "amp_remote_write" {
  metadata {
    name      = "amp-remote-write"
    namespace = "default"

    annotations = {
      "eks.amazonaws.com/role-arn" = aws_iam_role.amp_remote_write.arn
    }
  }
}
