terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.39"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.38"
    }
  }
}

provider "aws" {
  region = var.region
}

# Temporary - only needed so `terraform apply` can destroy the orphaned
# module.eks_amg.* resources left in state from the removed AMG module (it
# needs the provider they were originally created with to issue the delete
# calls). Remove this block again once those orphans are gone from state.
provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
}

data "aws_caller_identity" "current" {}

provider "kubernetes" {
  host                   = module.eks_cluster.cluster_endpoint
  cluster_ca_certificate = base64decode(module.eks_cluster.cluster_certificate_authority_data)

  exec {
    api_version = "client.authentication.k8s.io/v1beta1"
    command     = "aws"
    args        = ["--region", var.region, "eks", "get-token", "--cluster-name", var.cluster_name]
  }
}

module "vpc" {
  source = "./modules/vpc"

  cluster_name         = var.cluster_name
  vpc_cidr_block       = var.vpc_cidr_block
  azs                  = var.azs
  public_subnet_cidrs  = var.public_subnet_cidrs
  private_subnet_cidrs = var.private_subnet_cidrs
}

module "eks_cluster" {
  source = "./modules/eks-cluster"

  cluster_name               = var.cluster_name
  kubernetes_version         = var.kubernetes_version
  public_subnet_ids          = module.vpc.public_subnet_ids
  additional_admin_role_arn  = var.additional_admin_role_arn

  depends_on = [module.vpc]
}

module "eks_nodegroup" {
  source = "./modules/eks-nodegroup"

  cluster_name         = var.cluster_name
  nodegroup_name       = var.nodegroup_name
  node_instance_type   = var.node_instance_type
  node_desired_size    = var.node_desired_size
  node_min_size        = var.node_min_size
  node_max_size        = var.node_max_size
  node_volume_size     = var.node_volume_size
  ssh_public_key_name  = var.ssh_public_key_name
  subnet_ids           = module.vpc.private_subnet_ids

  depends_on = [module.eks_cluster]
}

module "eks_ebs_csi" {
  source = "./modules/eks-ebs-csi"

  cluster_name      = var.cluster_name
  oidc_provider_arn = module.eks_cluster.oidc_provider_arn
  oidc_provider_url = module.eks_cluster.oidc_provider_url

  depends_on = [module.eks_nodegroup]
}

module "eks_alb_controller" {
  source = "./modules/eks-alb-controller"

  cluster_name      = var.cluster_name
  oidc_provider_arn = module.eks_cluster.oidc_provider_arn
  oidc_provider_url = module.eks_cluster.oidc_provider_url

  depends_on = [module.eks_nodegroup]
}

module "eks_external_dns" {
  source = "./modules/eks-external-dns"

  cluster_name      = var.cluster_name
  oidc_provider_arn = module.eks_cluster.oidc_provider_arn
  oidc_provider_url = module.eks_cluster.oidc_provider_url

  depends_on = [module.eks_nodegroup]
}

module "eks_xray" {
  source = "./modules/eks-xray"

  cluster_name      = var.cluster_name
  oidc_provider_arn = module.eks_cluster.oidc_provider_arn
  oidc_provider_url = module.eks_cluster.oidc_provider_url

  depends_on = [module.eks_nodegroup]
}

module "eks_cluster_autoscaler" {
  source = "./modules/eks-cluster-autoscaler"

  cluster_name      = var.cluster_name
  oidc_provider_arn = module.eks_cluster.oidc_provider_arn
  oidc_provider_url = module.eks_cluster.oidc_provider_url

  depends_on = [module.eks_nodegroup]
}

module "eks_container_insights" {
  source = "./modules/eks-container-insights"

  cluster_name = var.cluster_name

  depends_on = [module.eks_nodegroup]
}

module "eks_fargate_profile" {
  source = "./modules/eks-fargate-profile"

  cluster_name         = var.cluster_name
  fargate_profile_name = var.fargate_profile_name
  namespace            = var.fargate_namespace
  subnet_ids           = module.vpc.private_subnet_ids

  depends_on = [module.eks_cluster]
}

module "ecr" {
  source = "./modules/ecr"

  repository_names   = var.ecr_repository_names
  repository_prefix  = var.cluster_name
}

module "rds" {
  source = "./modules/rds"

  vpc_id                  = module.vpc.vpc_id
  vpc_cidr_block          = var.vpc_cidr_block
  private_subnet_ids      = module.vpc.private_subnet_ids
  db_security_group_name  = "${var.cluster_name}-rds-sg"
  db_subnet_group_name    = "${var.cluster_name}-rds-subnetgroup"
  db_instance_identifier  = var.db_instance_identifier
  db_name                 = var.db_name
  db_engine_version       = var.db_engine_version
  db_instance_class       = var.db_instance_class
  db_allocated_storage    = var.db_allocated_storage
  db_storage_type         = var.db_storage_type
  db_master_username      = var.db_master_username
  db_master_password      = var.db_master_password
  db_port                 = var.db_port
  db_publicly_accessible  = var.db_publicly_accessible

  depends_on = [module.vpc]
}
