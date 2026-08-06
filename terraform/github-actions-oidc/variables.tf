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

# GitHub's OIDC token `sub` claim actually looks like
# "repo:OWNER@OWNER_ID/REPO@REPO_ID:ref:refs/heads/develop" - immutable
# numeric IDs are embedded alongside the names specifically so a trust policy
# can't be silently hijacked if this org/repo is later renamed and the old
# name gets claimed by someone else. Discovered via CloudTrail after the
# name-only pattern ("repo:szhanggit/microservice_0:*") never matched and
# every AssumeRoleWithWebIdentity call failed with AccessDenied. These are
# fixed per-repo identifiers, not expected to change - find them again via
# CloudTrail (`aws cloudtrail lookup-events --lookup-attributes
# AttributeKey=EventName,AttributeValue=AssumeRoleWithWebIdentity`) if this
# role is ever repointed at a different repo.
variable "github_owner_id" {
  description = "Immutable numeric ID for the szhanggit account/org (from the actual OIDC sub claim, not the same as the login name)"
  type        = string
  default     = "17355395"
}

variable "github_repo_id" {
  description = "Immutable numeric ID for the microservice_0 repo (from the actual OIDC sub claim, not the same as the repo name)"
  type        = string
  default     = "1306568515"
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

variable "frontend_bucket_pattern" {
  description = "S3 bucket name pattern for ../modules/frontend-cdn buckets across all environments - matches that module's \"${name_prefix}-frontend\" naming (name_prefix = cluster_name, e.g. microservice0-develop), same wildcard-by-prefix style already used for ECR above"
  type        = string
  default     = "microservice0-*-frontend"
}

variable "frontend_distribution_ids" {
  description = "CloudFront distribution ID(s) for ../modules/frontend-cdn. Unlike frontend_bucket_pattern, this can't be a name-based wildcard - CloudFront distribution IDs are opaque/unpredictable, assigned at creation. Must be updated (and this state reapplied) whenever a distribution is destroyed and recreated, e.g. after a full environment teardown - find the current ID via `aws cloudfront list-distributions --query \"DistributionList.Items[].{Id:Id,Aliases:Aliases.Items}\"`."
  type        = list(string)
  default     = ["E1DTFKOE3Q9TNY"] # microservice0-develop / usermgn.ekslab.xyz
}
