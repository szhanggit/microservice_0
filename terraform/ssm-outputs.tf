# Cross-pipeline handoff: writes the values a separate application/Kubernetes
# CI pipeline needs (cluster name, region, VPC ID, ECR repo URLs, DB
# connection string) to SSM Parameter Store / Secrets Manager. That pipeline
# then reads these via plain AWS CLI/SDK calls - see kubernetes/scripts/*.sh -
# with zero dependency on the Terraform CLI, this project's state, or S3
# backend credentials.

locals {
  ssm_prefix = "/microservice0/${var.environment}"
}

resource "aws_ssm_parameter" "cluster_name" {
  name  = "${local.ssm_prefix}/cluster_name"
  type  = "String"
  value = module.eks_cluster.cluster_name
}

resource "aws_ssm_parameter" "region" {
  name  = "${local.ssm_prefix}/region"
  type  = "String"
  value = var.region
}

resource "aws_ssm_parameter" "vpc_id" {
  name  = "${local.ssm_prefix}/vpc_id"
  type  = "String"
  value = module.vpc.vpc_id
}

# JSON-encoded map of service name -> ECR repository URL, e.g.
# { "userrepositoryservice": "<acct>.dkr.ecr.<region>.amazonaws.com/microservice0-develop/userrepositoryservice", ... }
resource "aws_ssm_parameter" "ecr_repository_urls" {
  name  = "${local.ssm_prefix}/ecr_repository_urls"
  type  = "String"
  value = jsonencode(module.ecr.repository_urls)
}

# Plain RDS endpoint hostname (no port/credentials) - used by
# kubernetes/dataaccess/externalname.yaml to demo an ExternalName Service
# giving the RDS instance an in-cluster DNS name.
resource "aws_ssm_parameter" "db_instance_address" {
  name  = "${local.ssm_prefix}/db_instance_address"
  type  = "String"
  value = module.rds.db_instance_address
}

# Read by kubernetes/scripts/install-alloy.sh to build the Prometheus
# remote_write URL for Alloy's AMP pipeline.
resource "aws_ssm_parameter" "amp_prometheus_endpoint" {
  name  = "${local.ssm_prefix}/amp_prometheus_endpoint"
  type  = "String"
  value = module.eks_amp.amp_prometheus_endpoint
}

# Same format dataaccess's ConnectionStrings__MySql secret expects. Stored in
# Secrets Manager rather than SSM since it holds the DB master password.
resource "aws_secretsmanager_secret" "db_connection_string" {
  name = "microservice0/${var.environment}/db-connection-string"

  # Allows immediate deletion on `terraform destroy` instead of Secrets
  # Manager's default 7-30 day recovery window - this is a learning/portfolio
  # environment that gets torn down and rebuilt, not something needing
  # accidental-deletion recovery.
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "db_connection_string" {
  secret_id     = aws_secretsmanager_secret.db_connection_string.id
  secret_string = "Server=${module.rds.db_instance_address};Port=${module.rds.db_instance_port};Database=${module.rds.db_name};User=${var.db_master_username};Password=${var.db_master_password};"
}
