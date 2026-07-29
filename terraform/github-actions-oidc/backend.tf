# Same S3 bucket as ../ (see ../backend.tf / ../scripts/bootstrap-backend.sh),
# just a distinct state key - this is deliberately its own standalone root
# module/state, not part of ../main.tf. The OIDC provider and IAM role here
# are account-wide singletons; ../main.tf gets applied independently once per
# environment (develop/staging/production, each its own state), so if this
# lived there it would try to create the same OIDC provider three times and
# fail on the 2nd/3rd apply.
terraform {
  backend "s3" {
    bucket       = ""
    key          = ""
    region       = ""
    encrypt      = true
    use_lockfile = true
  }
}
