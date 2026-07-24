variable "repository_names" {
  description = "Image names to create ECR repositories for, matching the Docker image names already built by each service's Dockerfile (userrepositoryservice, usermanagementservice, usermanagementgateway)"
  type        = list(string)
}

variable "repository_prefix" {
  description = "Prefix applied to every repository name, e.g. \"microservice0\" -> \"microservice0/userrepositoryservice\""
  type        = string
  default     = "microservice0"
}

variable "image_tag_mutability" {
  description = "Whether image tags can be overwritten (MUTABLE) or not (IMMUTABLE)"
  type        = string
  default     = "MUTABLE"
}

variable "max_image_count" {
  description = "Number of most-recent images to retain per repository before older ones are expired"
  type        = number
  default     = 10
}

variable "force_delete" {
  description = "Allow `terraform destroy` to delete repositories even if they still contain images - convenient for a learning/portfolio environment that gets torn down and rebuilt"
  type        = bool
  default     = true
}
