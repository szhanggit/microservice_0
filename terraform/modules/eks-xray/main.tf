# Equivalent of:
# eksctl create iamserviceaccount --cluster=microservice0 --namespace=default
#   --name=xray-daemon --attach-policy-arn=arn:aws:iam::aws:policy/AWSXRayDaemonWriteAccess
#   --override-existing-serviceaccounts --approve
#
# Distributed tracing is listed as optional (OpenTelemetry) in this project's
# tech stack and isn't wired into the .NET services yet - this module just
# provisions the IRSA role/service account a X-Ray daemon DaemonSet would use,
# mirroring the reference design so it's a drop-in once tracing is added.
data "aws_iam_policy_document" "xray_daemon_assume_role" {
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
      values   = ["system:serviceaccount:default:xray-daemon"]
    }

    condition {
      test     = "StringEquals"
      variable = "${replace(var.oidc_provider_url, "https://", "")}:aud"
      values   = ["sts.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "xray_daemon" {
  name               = "${var.cluster_name}-xray-daemon-role"
  assume_role_policy = data.aws_iam_policy_document.xray_daemon_assume_role.json
}

resource "aws_iam_role_policy_attachment" "xray_daemon" {
  role       = aws_iam_role.xray_daemon.name
  policy_arn = "arn:aws:iam::aws:policy/AWSXRayDaemonWriteAccess"
}

resource "kubernetes_service_account" "xray_daemon" {
  metadata {
    name      = "xray-daemon"
    namespace = "default"

    annotations = {
      "eks.amazonaws.com/role-arn" = aws_iam_role.xray_daemon.arn
    }
  }
}
