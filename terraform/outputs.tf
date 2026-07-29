output "region" {
  value = var.region
}

output "cluster_id" {
  value = module.eks_cluster.cluster_id
}

output "cluster_name" {
  value = module.eks_cluster.cluster_name
}

output "cluster_endpoint" {
  value = module.eks_cluster.cluster_endpoint
}

output "cluster_certificate_authority_data" {
  value = module.eks_cluster.cluster_certificate_authority_data
}

output "cluster_security_group_id" {
  value = module.eks_cluster.cluster_security_group_id
}

output "vpc_id" {
  value = module.vpc.vpc_id
}

output "public_subnet_ids" {
  value = module.vpc.public_subnet_ids
}

output "private_subnet_ids" {
  value = module.vpc.private_subnet_ids
}

output "nat_gateway_id" {
  value = module.vpc.nat_gateway_id
}

output "oidc_provider_arn" {
  value = module.eks_cluster.oidc_provider_arn
}

output "oidc_provider_url" {
  value = module.eks_cluster.oidc_provider_url
}

output "nodegroup_arn" {
  value = module.eks_nodegroup.nodegroup_arn
}

output "nodegroup_status" {
  value = module.eks_nodegroup.nodegroup_status
}

output "node_role_arn" {
  value = module.eks_nodegroup.node_role_arn
}

output "ebs_csi_driver_role_arn" {
  value = module.eks_ebs_csi.ebs_csi_driver_role_arn
}

output "alb_controller_policy_arn" {
  value = module.eks_alb_controller.alb_controller_policy_arn
}

output "alb_controller_role_arn" {
  value = module.eks_alb_controller.alb_controller_role_arn
}

output "external_dns_policy_arn" {
  value = module.eks_external_dns.external_dns_policy_arn
}

output "external_dns_role_arn" {
  value = module.eks_external_dns.external_dns_role_arn
}

output "xray_daemon_role_arn" {
  value = module.eks_xray.xray_daemon_role_arn
}

output "cluster_autoscaler_role_arn" {
  value = module.eks_cluster_autoscaler.cluster_autoscaler_role_arn
}

output "cloudwatch_agent_role_arn" {
  value = module.eks_container_insights.cloudwatch_agent_role_arn
}

output "fargate_profile_arn" {
  value = module.eks_fargate_profile.fargate_profile_arn
}

output "fargate_profile_status" {
  value = module.eks_fargate_profile.fargate_profile_status
}

output "fargate_pod_execution_role_arn" {
  value = module.eks_fargate_profile.pod_execution_role_arn
}

output "ecr_repository_urls" {
  description = "Map of service name -> ECR repository URL, e.g. { userrepositoryservice = \"<acct>.dkr.ecr.<region>.amazonaws.com/microservice0/userrepositoryservice\" }"
  value       = module.ecr.repository_urls
}

output "db_security_group_id" {
  value = module.rds.db_security_group_id
}

output "db_subnet_group_name" {
  value = module.rds.db_subnet_group_name
}

output "db_instance_endpoint" {
  value = module.rds.db_instance_endpoint
}

output "db_instance_address" {
  value = module.rds.db_instance_address
}

output "db_instance_port" {
  value = module.rds.db_instance_port
}

output "rds_enhanced_monitoring_role_arn" {
  value = module.rds.rds_enhanced_monitoring_role_arn
}

# Ready to drop straight into dataaccess's Secret - see
# scripts/apply-db-secret.sh, which reads this via `terraform output -raw`.
output "db_connection_string" {
  value = "Server=${module.rds.db_instance_address};Port=${module.rds.db_instance_port};Database=${module.rds.db_name};User=${var.db_master_username};Password=${var.db_master_password};"
  sensitive = true
}
