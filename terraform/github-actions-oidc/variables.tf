variable "region" {
  description = "AWS region"
  type        = string
  default     = "ca-central-1"
}

variable "github_repo" {
  description = "GitHub repo allowed to assume this role, as <org>/<repo> - scopes the OIDC trust policy so only workflow runs from this exact repo can assume it"
  type        = string
  default     = "szhanggit/microservice_0"
}

variable "role_name" {
  description = "IAM role name. Also referenced by ../variables.tf's github_actions_role_name (default must match) - ../main.tf computes this role's ARN directly (different Terraform state, so it can't reference this module's output) to pass as the eks_cluster module's additional_admin_role_arn"
  type        = string
  default     = "github-actions-microservice0"
}

variable "route53_zone_id" {
  description = "Hosted zone ID cleanup-dns.sh is allowed to modify (ekslab.xyz)"
  type        = string
  default     = "Z01003713LZ694SBOOR14"
}
