variable "cluster_name" {
  description = "EKS cluster name the Fargate profile attaches to"
  type        = string
}

variable "fargate_profile_name" {
  description = "Name of the EKS Fargate profile"
  type        = string
}

variable "namespace" {
  description = "Kubernetes namespace selector for the Fargate profile"
  type        = string
}

variable "subnet_ids" {
  description = "Private subnet IDs where Fargate pods are launched"
  type        = list(string)
}
