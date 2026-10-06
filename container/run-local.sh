#!/usr/bin/env bash
set -euo pipefail
APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$APP_DIR/frontend"
if [ ! -d node_modules ]; then npm ci; fi
npm run build
mkdir -p "$APP_DIR/backend/wwwroot"
cp -R dist/. "$APP_DIR/backend/wwwroot/"
cd "$APP_DIR/backend"
export VIBE_LOCAL=1
exec dotnet run --no-launch-profile --urls "http://127.0.0.1:${VIBE_LOCAL_PORT:-8088}"
