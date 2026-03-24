#!/bin/bash
set -euo pipefail

# Only run in remote (iOS / Claude Code on the web) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

echo "SessionStart: setting up MenuCraft environment..."

# -------------------------------------------------------
# Add dependency installation commands here as the project
# grows. Examples:
#
#   npm install          # Node.js
#   pip install -r requirements.txt  # Python
#   bundle install       # Ruby
# -------------------------------------------------------

echo "SessionStart: setup complete."
