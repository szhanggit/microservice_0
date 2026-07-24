# None of dataaccess/management/gateway run here today - they're scheduled
# onto the managed node group like any normal Deployment. This profile is
# provisioned to mirror the reference design and as a ready-made landing spot
# for a future namespace (e.g. a batch/cron workload) that would benefit from
# Fargate instead of the shared node group.
resource "aws_iam_role" "fargate" {
  name = "${var.cluster_name}-${var.fargate_profile_name}-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Action    = "sts:AssumeRole"
      Principal = { Service = "eks-fargate-pods.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "fargate_pod_execution" {
  role       = aws_iam_role.fargate.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSFargatePodExecutionRolePolicy"
}

resource "aws_eks_fargate_profile" "main" {
  cluster_name           = var.cluster_name
  fargate_profile_name   = var.fargate_profile_name
  pod_execution_role_arn = aws_iam_role.fargate.arn
  subnet_ids             = var.subnet_ids

  selector {
    namespace = var.namespace
  }

  depends_on = [aws_iam_role_policy_attachment.fargate_pod_execution]
}
