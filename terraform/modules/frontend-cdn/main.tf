# S3 + CloudFront static hosting for the Angular app (components/UserManagementWeb),
# replacing the old plan of running it as its own nginx container in-cluster.
# Split-subdomain design: this distribution serves ONLY the static Angular
# build on its own domain (usermgn.ekslab.xyz) - the gateway API keeps
# microservice0.ekslab.xyz on the ALB, completely untouched by this module.
# That's a genuine cross-origin call from the browser's perspective (see
# UserManagementGateway's Program.cs for the CORS policy that allows it, and
# environment.prod.ts for the absolute apiBaseUrl this app is built with) -
# traded deliberately for a much simpler distribution here: a single S3
# origin, no ALB origin/custom_origin_config, no HTTP-vs-HTTPS-to-origin
# workaround, and no fight with ExternalDNS over microservice0.ekslab.xyz
# (that record is untouched - see Helm/templates/ingress.yaml).
#
# CloudFront custom domains require the ACM certificate to live in us-east-1
# specifically, regardless of where everything else in this stack runs - see
# the "us_east_1" aws provider alias in ../../main.tf (a configuration_alias,
# passed in explicitly via this module's `providers` block in the caller).
terraform {
  required_providers {
    aws = {
      source                = "hashicorp/aws"
      configuration_aliases = [aws.us_east_1]
    }
  }
}

resource "aws_s3_bucket" "frontend" {
  bucket        = "${var.name_prefix}-frontend"
  force_destroy = var.bucket_force_destroy

  tags = {
    Name = "${var.name_prefix}-frontend"
  }
}

# Private bucket, no S3 website hosting mode - CloudFront reaches it only via
# Origin Access Control (OAC) below. SPA fallback (deep links like
# /users/create) is handled by the distribution's custom_error_response
# instead of S3 website mode's index/error document feature, since that
# feature requires public/website-mode buckets that OAC doesn't support.
resource "aws_s3_bucket_public_access_block" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_ownership_controls" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_cloudfront_origin_access_control" "frontend" {
  name                              = "${var.name_prefix}-frontend-oac"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# Grants CloudFront read access to the bucket, scoped to this specific
# distribution via the aws:SourceArn condition - the bucket itself stays
# fully private (see the public access block above).
data "aws_iam_policy_document" "frontend_bucket" {
  statement {
    sid       = "AllowCloudFrontServicePrincipalReadOnly"
    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.frontend.arn}/*"]

    principals {
      type        = "Service"
      identifiers = ["cloudfront.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "AWS:SourceArn"
      values   = [aws_cloudfront_distribution.frontend.arn]
    }
  }
}

resource "aws_s3_bucket_policy" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  policy = data.aws_iam_policy_document.frontend_bucket.json
}

resource "aws_acm_certificate" "frontend" {
  provider          = aws.us_east_1
  domain_name       = var.domain_name
  validation_method = "DNS"

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_route53_record" "frontend_cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.frontend.domain_validation_options : dvo.domain_name => {
      name  = dvo.resource_record_name
      type  = dvo.resource_record_type
      value = dvo.resource_record_value
    }
  }

  zone_id         = var.route53_zone_id
  name            = each.value.name
  type            = each.value.type
  records         = [each.value.value]
  ttl             = 60
  allow_overwrite = true
}

resource "aws_acm_certificate_validation" "frontend" {
  provider                = aws.us_east_1
  certificate_arn         = aws_acm_certificate.frontend.arn
  validation_record_fqdns = [for r in aws_route53_record.frontend_cert_validation : r.fqdn]
}

data "aws_cloudfront_cache_policy" "caching_optimized" {
  name = "Managed-CachingOptimized"
}

resource "aws_cloudfront_distribution" "frontend" {
  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"
  price_class         = var.price_class
  aliases             = [var.domain_name]

  origin {
    origin_id                = "s3-frontend"
    domain_name               = aws_s3_bucket.frontend.bucket_regional_domain_name
    origin_access_control_id = aws_cloudfront_origin_access_control.frontend.id
  }

  default_cache_behavior {
    target_origin_id       = "s3-frontend"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods         = ["GET", "HEAD"]
    cached_methods          = ["GET", "HEAD"]
    cache_policy_id         = data.aws_cloudfront_cache_policy.caching_optimized.id
    compress                = true
  }

  # SPA client-side routing: a private S3+OAC origin has no "index document
  # on 404" behavior (that's an S3 website-hosting-mode feature, incompatible
  # with OAC), so a deep link like /users/create 403s/404s straight from S3.
  # Rewrite both back to index.html so the Angular router can take over -
  # same effect as nginx.conf's `try_files $uri $uri/ /index.html`.
  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate_validation.frontend.certificate_arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }

  tags = {
    Name = "${var.name_prefix}-frontend"
  }
}

# domain_name (usermgn.ekslab.xyz) is a distinct name from the ALB's own
# microservice0.ekslab.xyz, which ExternalDNS continues to manage
# unchanged - no ownership conflict between this record and ExternalDNS.
resource "aws_route53_record" "frontend" {
  zone_id = var.route53_zone_id
  name    = var.domain_name
  type    = "A"

  alias {
    name                   = aws_cloudfront_distribution.frontend.domain_name
    zone_id                = aws_cloudfront_distribution.frontend.hosted_zone_id
    evaluate_target_health = false
  }
}
