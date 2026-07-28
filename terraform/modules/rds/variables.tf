variable "vpc_id" {
  description = "VPC ID of the EKS cluster's VPC, used for the RDS security group"
  type        = string
}

variable "vpc_cidr_block" {
  description = "CIDR block of the EKS cluster's VPC, scopes the RDS security group's ingress rule"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs of the EKS cluster's VPC, for the DB subnet group"
  type        = list(string)
}

variable "db_security_group_name" {
  description = "Name of the security group allowing inbound MySQL access"
  type        = string
  default     = "microservice0-rds-sg"
}

variable "db_subnet_group_name" {
  description = "Name of the RDS DB subnet group"
  type        = string
  default     = "microservice0-rds-subnetgroup"
}

variable "db_instance_identifier" {
  description = "DB instance identifier"
  type        = string
  default     = "microservice0-db"
}

variable "db_name" {
  description = "Initial database name created on the RDS instance"
  type        = string
  default     = "userdb"
}

variable "db_engine_version" {
  description = "MySQL engine version. Closest available RDS version to the mysql:8.0.36 image used by docker-compose for local dev parity - AWS has since deprecated 8.0.36 on RDS (run `aws rds describe-db-engine-versions --engine mysql` to see what's currently offered in your region)."
  type        = string
  default     = "8.0.42"
}

variable "db_instance_class" {
  description = "DB instance size"
  type        = string
  default     = "db.t3.micro"
}

variable "db_allocated_storage" {
  description = "Allocated storage (GiB)"
  type        = number
  default     = 20
}

variable "db_storage_type" {
  description = "Storage type"
  type        = string
  default     = "gp2"
}

variable "db_master_username" {
  description = "Master username for the database - what dataaccess's ConnectionStrings__MySql secret authenticates as"
  type        = string
  default     = "dbadmin"
}

variable "db_master_password" {
  description = "Master password for the database. Set via a local-only tfvars file (see environments/<env>/secrets.tfvars.example), never commit it."
  type        = string
  sensitive   = true
}

variable "db_port" {
  description = "Database port"
  type        = number
  default     = 3306
}

variable "monitoring_interval" {
  description = <<-EOT
    Enhanced Monitoring granularity in seconds (0, 1, 5, 10, 15, 30, or 60).
    0 disables Enhanced Monitoring entirely. Unlike Performance Insights, this
    is supported on every instance class including db.t3.micro.
  EOT
  type        = number
  default     = 60
}

variable "performance_insights_enabled" {
  description = <<-EOT
    Enable Performance Insights (query-level/wait-event visibility). Requires
    at least db.t3.medium for MySQL - db.t3.micro/small report
    SupportsPerformanceInsights=false (verified via
    `aws rds describe-orderable-db-instance-options`). Terraform apply will
    fail if enabled against an unsupported instance class.

    Defaults to false: this account is on an AWS free-plan that hard-blocks
    ModifyDBInstance to any non-free-tier instance class (FreeTierRestrictionError
    on db.t3.medium, confirmed live) - so on a free-plan account Performance
    Insights isn't reachable at all without upgrading the account plan first,
    independent of anything Terraform controls. Flip to true (and bump
    db_instance_class to db.t3.medium+) once the account plan allows it.
  EOT
  type        = bool
  default     = false
}

variable "performance_insights_retention_period" {
  description = "Performance Insights retention in days. 7 is the free-tier default - longer retention (up to 731 days) is billed."
  type        = number
  default     = 7
}

variable "db_publicly_accessible" {
  description = <<-EOT
    Whether the DB instance gets a public IP. Defaults to false: dataaccess
    reaches RDS entirely inside the VPC, and this is meant to be a
    production-ready setup rather than the public-by-default convenience of
    the eksctl tutorial this module is based on. Flip to true only if you need
    to reach it directly from outside the VPC (e.g. a local MySQL client).
  EOT
  type        = bool
  default     = false
}
