data "aws_iam_policy_document" "lambda_assume" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "lambda" {
  name               = "${local.name_prefix}-lambda"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json
}

resource "aws_iam_role_policy_attachment" "lambda_logs" {
  role       = aws_iam_role.lambda.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

data "aws_iam_policy_document" "lambda_dynamo" {
  statement {
    effect = "Allow"

    actions = [
      "dynamodb:GetItem",
      "dynamodb:PutItem",
      "dynamodb:Query",
      "dynamodb:Scan",
    ]

    resources = [aws_dynamodb_table.main.arn]
  }
}

resource "aws_iam_role_policy" "lambda_dynamo" {
  name   = "dynamodb"
  role   = aws_iam_role.lambda.id
  policy = data.aws_iam_policy_document.lambda_dynamo.json
}

resource "aws_lambda_function" "api" {
  function_name    = "${local.name_prefix}-api"
  role             = aws_iam_role.lambda.arn
  filename         = local.artifact_zip
  source_code_hash = filebase64sha256(local.artifact_zip)
  runtime          = "provided.al2023"
  handler          = "bootstrap"
  architectures    = ["x86_64"]

  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = "Production"
      DynamoDB__TableName    = var.table_name
      CalendarProvider__Type = "Mock"
    }
  }

  depends_on = [aws_iam_role_policy_attachment.lambda_logs]
}
