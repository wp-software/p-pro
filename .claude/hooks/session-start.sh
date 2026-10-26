#!/bin/bash
set -euo pipefail

# Only run in Claude Code remote (web) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

echo "==> Installing frontend dependencies..."
cd "${CLAUDE_PROJECT_DIR}/src/frontend"
npm ci

echo "==> Restoring .NET backend dependencies..."
if command -v dotnet &>/dev/null; then
  cd "${CLAUDE_PROJECT_DIR}/src/backend"
  dotnet restore
else
  echo "    dotnet not found, skipping restore."
fi

echo "==> Session setup complete."
