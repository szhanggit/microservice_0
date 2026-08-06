output "bucket_name" {
  value = aws_s3_bucket.frontend.id
}

output "bucket_arn" {
  value = aws_s3_bucket.frontend.arn
}

output "distribution_id" {
  description = "Needed by a future CI step to invalidate the cache after syncing a new Angular build to the bucket"
  value       = aws_cloudfront_distribution.frontend.id
}

output "distribution_domain_name" {
  value = aws_cloudfront_distribution.frontend.domain_name
}

output "acm_certificate_arn" {
  value = aws_acm_certificate_validation.frontend.certificate_arn
}
