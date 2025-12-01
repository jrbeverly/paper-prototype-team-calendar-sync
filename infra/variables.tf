variable "region" {
  description = "AWS region to deploy into."
  type        = string
  default     = "us-east-1"
}

variable "table_name" {
  description = "DynamoDB table name shared by the Lambda and the application config."
  type        = string
  default     = "TeamCalendarSync"
}
