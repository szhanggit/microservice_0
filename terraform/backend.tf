# Terraform backend blocks can't reference variables, so bucket/key/region
# are left blank here and filled in at `terraform init` time via
# -backend-config=environments/<env>/backend.tfvars - one file per
# environment (develop/staging/production), each pointing at its own state
# key so the three environments never share (or clobber) state.
#
# use_lockfile enables Terraform's native S3 state locking (Terraform >= 1.10,
# no DynamoDB table needed) - it's the same for every environment so it's
# hardcoded here rather than repeated in each backend.tfvars.
#
# scripts/bootstrap-backend.sh creates the shared S3 bucket once; every
# environment's backend.tfvars reuses it, only `key` differs per environment.
terraform {
  backend "s3" {
    bucket       = ""
    key          = ""
    region       = ""
    encrypt      = true
    use_lockfile = true
  }
}
