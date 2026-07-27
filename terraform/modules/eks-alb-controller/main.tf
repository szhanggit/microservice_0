# Equivalent of:
# aws iam create-policy --policy-name AWSLoadBalancerControllerIAMPolicy \
#   --policy-document file://iam_policy.json
#
# iam_policy.json is the file fetched from:
# https://raw.githubusercontent.com/kubernetes-sigs/aws-load-balancer-controller/main/docs/install/iam_policy.json
resource "aws_iam_policy" "alb_controller" {
  name   = "${var.cluster_name}-AWSLoadBalancerControllerIAMPolicy"
  policy = file("${path.module}/iam_policy.json")
}

# Equivalent of:
# eksctl create iamserviceaccount --cluster=microservice0 --namespace=kube-system
#   --name=aws-load-balancer-controller --attach-policy-arn=<policy arn>
#   --override-existing-serviceaccounts --approve
data "aws_iam_policy_document" "alb_controller_assume_role" {
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
      values   = ["system:serviceaccount:kube-system:aws-load-balancer-controller"]
    }

    condition {
      test     = "StringEquals"
      variable = "${replace(var.oidc_provider_url, "https://", "")}:aud"
      values   = ["sts.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "alb_controller" {
  name               = "${var.cluster_name}-aws-load-balancer-controller-role"
  assume_role_policy = data.aws_iam_policy_document.alb_controller_assume_role.json
}

resource "aws_iam_role_policy_attachment" "alb_controller" {
  role       = aws_iam_role.alb_controller.name
  policy_arn = aws_iam_policy.alb_controller.arn
}

# eksctl create iamserviceaccount also creates/annotates the Kubernetes
# ServiceAccount so pods using it assume the IAM role above (IRSA). The
# controller itself is installed separately via Helm
# (scripts/install-alb-controller.sh) since there's no first-party Terraform
# resource for it - matches how the reference project's justfile does it too.
resource "kubernetes_service_account" "alb_controller" {
  metadata {
    name      = "aws-load-balancer-controller"
    namespace = "kube-system"

    annotations = {
      "eks.amazonaws.com/role-arn" = aws_iam_role.alb_controller.arn
    }
  }
}
