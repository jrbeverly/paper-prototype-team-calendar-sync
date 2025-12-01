.PHONY: build frontend-build test lint validate run serve infra-plan infra-apply frontend-deploy

SOLUTION    := TeamCalendarSync.sln
API_PROJECT := src/Api
WEB_PROJECT := src/Web
ARTIFACTS   := artifacts
INFRA_DIR   := infra

build:
	dotnet restore $(SOLUTION)
	mkdir -p $(ARTIFACTS)
	dotnet publish $(API_PROJECT) -c Release -r linux-x64 --self-contained true -o $(ARTIFACTS)/api-publish
	cd $(ARTIFACTS)/api-publish && zip -r ../api.zip .

frontend-build:
	cd $(WEB_PROJECT) && npm ci && npm run build
	mkdir -p $(ARTIFACTS)
	rm -rf $(ARTIFACTS)/web-dist
	cp -r $(WEB_PROJECT)/dist $(ARTIFACTS)/web-dist

test:
	dotnet test $(SOLUTION)

lint:
	dotnet format --verify-no-changes $(SOLUTION)

validate:
	dotnet build $(SOLUTION)

# Start the API locally against LocalStack (requires LocalStack to be running)
run:
	dotnet run --project $(API_PROJECT)

serve:
	bash scripts/serve-local.sh

# Preview infrastructure changes (builds the Lambda ZIP first)
infra-plan: build
	terraform -chdir=$(INFRA_DIR) init -input=false
	terraform -chdir=$(INFRA_DIR) plan

# Deploy infrastructure to AWS (builds the Lambda ZIP first, then prompts for confirmation)
infra-apply: build
	terraform -chdir=$(INFRA_DIR) init -input=false
	terraform -chdir=$(INFRA_DIR) apply

# Upload the frontend build to S3 and invalidate the CloudFront cache.
# Run make frontend-build first, then set VITE_API_URL at build time.
# Example: VITE_API_URL=https://abc123.execute-api.us-east-1.amazonaws.com make frontend-build frontend-deploy
frontend-deploy: frontend-build
	aws s3 sync $(ARTIFACTS)/web-dist/ \
	  s3://$$(terraform -chdir=$(INFRA_DIR) output -raw frontend_bucket) \
	  --delete
	aws cloudfront create-invalidation \
	  --distribution-id $$(terraform -chdir=$(INFRA_DIR) output -raw cloudfront_distribution_id) \
	  --paths "/*"
