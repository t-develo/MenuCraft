#!/bin/bash
set -euo pipefail

# Only run in remote (iOS / Claude Code on the web) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

echo "SessionStart: setting up MenuCraft environment..."

# .NET: restore backend dependencies if project file exists
if [ -f "$CLAUDE_PROJECT_DIR/src/api/MenuCraft.Api.csproj" ]; then
  echo "SessionStart: restoring .NET dependencies..."
  dotnet restore "$CLAUDE_PROJECT_DIR/src/api/"
fi

# Node.js: install frontend dependencies if package.json exists
if [ -f "$CLAUDE_PROJECT_DIR/src/client/package.json" ]; then
  echo "SessionStart: installing frontend npm dependencies..."
  npm install --prefix "$CLAUDE_PROJECT_DIR/src/client"
fi

echo "SessionStart: setup complete."
