#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONTAINER="team-calendar-sync-dynamodb"
ENDPOINT="http://127.0.0.1:8000"
API_URL="http://127.0.0.1:5000"

for command in aws curl docker dotnet npm; do
  command -v "$command" >/dev/null || { echo "$command is required"; exit 1; }
done

if docker ps -a --format '{{.Names}}' | grep -qx "$CONTAINER"; then
  echo "Docker container '$CONTAINER' already exists. Remove it before running make serve."
  exit 1
fi

api_pid=""
web_pid=""
cleanup() {
  trap - EXIT INT TERM
  [ -n "$web_pid" ] && kill "$web_pid" 2>/dev/null || true
  [ -n "$api_pid" ] && kill "$api_pid" 2>/dev/null || true
  [ -n "$web_pid" ] && wait "$web_pid" 2>/dev/null || true
  [ -n "$api_pid" ] && wait "$api_pid" 2>/dev/null || true
  docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
}
trap cleanup EXIT
trap 'exit 130' INT TERM

export AWS_ACCESS_KEY_ID=local
export AWS_SECRET_ACCESS_KEY=local
export AWS_REGION=us-east-1
export AWS_EC2_METADATA_DISABLED=true

cd "$ROOT"

docker run --rm --detach \
  --name "$CONTAINER" \
  --publish 127.0.0.1:8000:8000 \
  amazon/dynamodb-local:3.3.0 >/dev/null

until aws dynamodb list-tables --endpoint-url "$ENDPOINT" >/dev/null 2>&1; do
  docker ps --format '{{.Names}}' | grep -qx "$CONTAINER" || {
    echo "DynamoDB Local stopped before becoming ready."
    exit 1
  }
  sleep 1
done

dotnet build src/Api/Api.csproj --nologo

AWS__ServiceURL="$ENDPOINT" \
DynamoDB__TableName=TeamCalendarSync \
CalendarProvider__Type=Mock \
ASPNETCORE_URLS="$API_URL" \
ASPNETCORE_ENVIRONMENT=Development \
dotnet src/Api/bin/Debug/net8.0/bootstrap.dll &
api_pid=$!

until curl --silent --fail "$API_URL/ping" >/dev/null 2>&1; do
  kill -0 "$api_pid" 2>/dev/null || { wait "$api_pid"; exit 1; }
  sleep 1
done

curl --silent --fail --request POST "$API_URL/admin/seed" >/dev/null

npm --prefix src/Web install
(
  cd src/Web
  exec ./node_modules/.bin/vite --host 0.0.0.0
) &
web_pid=$!

echo
echo "Team Calendar Sync is available at http://localhost:5173"
echo "The local organization data is seeded and the calendar provider is in memory."
echo "Press Ctrl-C to stop the stack."

wait -n "$api_pid" "$web_pid"
