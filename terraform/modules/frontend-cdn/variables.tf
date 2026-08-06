variable "name_prefix" {
  description = "Prefix for the S3 bucket name and resource tags, e.g. \"microservice0-develop\""
  type        = string
}

variable "domain_name" {
  description = "Custom domain the CloudFront distribution answers on, e.g. \"usermgn.ekslab.xyz\" - a distinct name from the gateway API's own domain (microservice0.ekslab.xyz, served directly by the ALB, untouched by this module) - must match the Route53 zone given in route53_zone_id"
  type        = string
}

variable "route53_zone_id" {
  description = "Hosted zone ID for domain_name - used for ACM DNS validation records and the distribution's alias record"
  type        = string
}

variable "price_class" {
  description = "CloudFront price class - PriceClass_100 (US/Canada/Europe only) is the cheapest tier, free-tier-friendly for a project with no real global traffic"
  type        = string
  default     = "PriceClass_100"
}

variable "bucket_force_destroy" {
  description = "Allow `terraform destroy` to delete the bucket even if it still has objects - convenient for a learning/portfolio environment that gets torn down and rebuilt"
  type        = bool
  default     = true
}
