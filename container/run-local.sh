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
# The copied cloud config.env pins bootstrap to the owner; the loopback demo
# identity must be allowed to initialize the in-memory admin instead.
export BOOTSTRAP_ADMIN_EMAIL="${BOOTSTRAP_ADMIN_EMAIL:-demo@alza.cz}"
exec dotnet run --no-launch-profile --urls "http://127.0.0.1:${VIBE_LOCAL_PORT:-8088}"
