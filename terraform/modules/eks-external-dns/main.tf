# No Route53 hosted zone/domain is wired up in this project yet, so
# ExternalDNS has nothing to manage today. Provisioned anyway (IRSA role +
# service account) to mirror the reference design and to be a one-line drop-in
# once a real domain is available - see kubernetes/ingress/ingress.yaml for
# where the `external-dns.alpha.kubernetes.io/hostname` annotation would go.
resource "aws_iam_policy" "external_dns" {
  name        = "${var.cluster_name}-AllowExternalDNSUpdates"
  description = "Allow access to Route53 Resources for ExternalDNS"
  policy      = file("${path.module}/iam_policy.json")
}

data "aws_iam_policy_document" "external_dns_assume_role" {
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
      values   = ["system:serviceaccount:default:external-dns"]
    }

    condition {
      test     = "StringEquals"
      variable = "${replace(var.oidc_provider_url, "https://", "")}:aud"
      values   = ["sts.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "external_dns" {
  name               = "${var.cluster_name}-external-dns-role"
  assume_role_policy = data.aws_iam_policy_document.external_dns_assume_role.json
}

resource "aws_iam_role_policy_attachment" "external_dns" {
  role       = aws_iam_role.external_dns.name
  policy_arn = aws_iam_policy.external_dns.arn
}

resource "kubernetes_service_account" "external_dns" {
  metadata {
    name      = "external-dns"
    namespace = "default"

    annotations = {
      "eks.amazonaws.com/role-arn" = aws_iam_role.external_dns.arn
    }
  }
}
