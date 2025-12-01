output "api_url" {
  description = "API Gateway base URL."
  value       = aws_apigatewayv2_stage.default.invoke_url
}

output "ping_url" {
  description = "Ping endpoint — curl this to verify the deployed stack."
  value       = "${aws_apigatewayv2_stage.default.invoke_url}/ping"
}

output "cloudfront_domain" {
  description = "CloudFront distribution domain for the frontend."
  value       = aws_cloudfront_distribution.frontend.domain_name
}

output "frontend_bucket" {
  description = "S3 bucket name for frontend assets (used by frontend-deploy)."
  value       = aws_s3_bucket.frontend.bucket
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID (used for cache invalidation by frontend-deploy)."
  value       = aws_cloudfront_distribution.frontend.id
}

output "dynamodb_table" {
  description = "DynamoDB table name."
  value       = aws_dynamodb_table.main.name
}
