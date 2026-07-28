output "amp_workspace_id" {
  value = aws_prometheus_workspace.this.id
}

output "amp_prometheus_endpoint" {
  value = aws_prometheus_workspace.this.prometheus_endpoint
}

output "amp_remote_write_role_arn" {
  value = aws_iam_role.amp_remote_write.arn
}
