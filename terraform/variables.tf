variable "cluster_name" {
  description = "EKS cluster name - also the Kubernetes namespace this project's manifests deploy into"
  type        = string
  default     = "microservice0"
}

variable "region" {
  description = "AWS region for the EKS cluster"
  type        = string
  default     = "ca-central-1"
}

variable "azs" {
  description = "Availability zones for the cluster's public/private subnets"
  type        = list(string)
  default     = ["ca-central-1a", "ca-central-1b"]
}

variable "kubernetes_version" {
  description = "Kubernetes version for the EKS control plane"
  type        = string
  default     = "1.31"
}

variable "additional_admin_role_arn" {
  description = "Optional extra IAM role ARN granted cluster-admin via an EKS access entry (e.g. a future CI/CD deploy role). Leave blank to skip."
  type        = string
  default     = ""
}

variable "vpc_cidr_block" {
  description = "CIDR block for the EKS VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for the public subnets, one per AZ in var.azs"
  type        = list(string)
  default     = ["10.0.0.0/24", "10.0.1.0/24"]
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for the private subnets, one per AZ in var.azs"
  type        = list(string)
  default     = ["10.0.2.0/24", "10.0.3.0/24"]
}

variable "nodegroup_name" {
  description = "Name of the EKS managed node group"
  type        = string
  default     = "microservice0-ng-private1"
}

variable "node_instance_type" {
  description = "EC2 instance type for the node group"
  type        = string
  default     = "t3.medium"
}

variable "node_desired_size" {
  description = "Desired number of nodes in the node group"
  type        = number
  default     = 2
}

variable "node_min_size" {
  description = "Minimum number of nodes in the node group"
  type        = number
  default     = 2
}

variable "node_max_size" {
  description = "Maximum number of nodes in the node group - sized for up to 5 replicas x 3 services (kubernetes/*/hpa.yaml) plus cluster add-ons"
  type        = number
  default     = 4
}

variable "node_volume_size" {
  description = "Root EBS volume size (GiB) for each node"
  type        = number
  default     = 20
}

variable "ssh_public_key_name" {
  description = "Name of an existing EC2 key pair to enable SSH access to nodes. Leave blank to skip SSH access entirely."
  type        = string
  default     = ""
}

variable "ecr_repository_names" {
  description = "Image names to create ECR repositories for - must match each service's Dockerfile image name"
  type        = list(string)
  default     = ["userrepositoryservice", "usermanagementservice", "usermanagementgateway"]
}

variable "fargate_profile_name" {
  description = "Name of the EKS Fargate profile"
  type        = string
  default     = "microservice0-fp"
}

variable "fargate_namespace" {
  description = "Kubernetes namespace selector for the Fargate profile. No workload targets this namespace today (see modules/eks-fargate-profile)."
  type        = string
  default     = "fargate-demo"
}

variable "db_instance_identifier" {
  description = "RDS DB instance identifier"
  type        = string
  default     = "microservice0-db"
}

variable "db_name" {
  description = "Initial application database name, matches Database=userdb in every service's connection string"
  type        = string
  default     = "userdb"
}

variable "db_engine_version" {
  description = "RDS MySQL engine version. Closest available RDS version to the mysql:8.0.36 image used by docker-compose for local dev parity - AWS has since deprecated 8.0.36 on RDS (run `aws rds describe-db-engine-versions --engine mysql` to see what's currently offered in your region)."
  type        = string
  default     = "8.0.42"
}

variable "db_instance_class" {
  description = "RDS DB instance size"
  type        = string
  default     = "db.t3.micro"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage (GiB)"
  type        = number
  default     = 20
}

variable "db_storage_type" {
  description = "RDS storage type"
  type        = string
  default     = "gp2"
}

variable "db_master_username" {
  description = "Master username for the RDS database - what dataaccess's ConnectionStrings__MySql secret authenticates as"
  type        = string
  default     = "dbadmin"
}

variable "db_master_password" {
  description = "Master password for the RDS database. Set this in a local-only tfvars file (see environments/<env>/secrets.tfvars.example), never commit it."
  type        = string
  sensitive   = true
}

variable "db_port" {
  description = "RDS database port"
  type        = number
  default     = 3306
}

variable "db_publicly_accessible" {
  description = "Whether the RDS instance gets a public IP. Defaults to false - see modules/rds/variables.tf for why."
  type        = bool
  default     = false
}
