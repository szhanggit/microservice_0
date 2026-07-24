output "fargate_profile_arn" {
  value = aws_eks_fargate_profile.main.arn
}

output "fargate_profile_status" {
  value = aws_eks_fargate_profile.main.status
}

output "pod_execution_role_arn" {
  value = aws_iam_role.fargate.arn
}
