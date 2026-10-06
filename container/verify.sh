#!/usr/bin/env bash
# Build and test without cloud calls; starts and cleans up its own demo server.
set -euo pipefail
APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$APP_DIR")"
VERIFY_PORT="${VIBE_VERIFY_PORT:-8099}"
VERIFY_DIR="$REPO_DIR/.tools/verification-app"
TASK_PYTHON="$(command -v python3.12 || command -v python3)"
for tool in dotnet node npm; do command -v "$tool" >/dev/null || { echo "Missing prerequisite: $tool"; exit 1; }; done
mkdir -p "$REPO_DIR/.tools"
"$TASK_PYTHON" - "$VERIFY_PORT" <<'PY'
import socket, sys
with socket.socket() as listener:
    listener.bind(('127.0.0.1', int(sys.argv[1])))
PY
npm --prefix "$APP_DIR/frontend" ci --no-audit --no-fund
npm --prefix "$APP_DIR/frontend" run build
dotnet restore "$APP_DIR/backend" --locked-mode --verbosity quiet
dotnet test "$APP_DIR/backend.tests" --nologo --verbosity quiet
node --test "$APP_DIR/tools/n8n-workflow.test.mjs"
"$TASK_PYTHON" -m unittest discover -s "$APP_DIR/tools" -p 'test_*.py'
dotnet publish "$APP_DIR/backend" -c Release -o "$VERIFY_DIR" --no-restore --verbosity quiet
"$TASK_PYTHON" - "$APP_DIR/frontend/dist" "$VERIFY_DIR/wwwroot" <<'PY'
import pathlib, shutil, sys
source, target = map(pathlib.Path, sys.argv[1:])
if target.exists(): shutil.rmtree(target)
shutil.copytree(source, target)
PY
cp "$APP_DIR/config.env" "$VERIFY_DIR/config.env"
(
    cd "$VERIFY_DIR"
    export VIBE_LOCAL=1
    exec dotnet Backend.dll --urls "http://127.0.0.1:$VERIFY_PORT"
) > "$REPO_DIR/.tools/verification-server.log" 2>&1 &
VERIFY_PID=$!
cleanup() { kill "$VERIFY_PID" 2>/dev/null || true; wait "$VERIFY_PID" 2>/dev/null || true; }
trap cleanup EXIT
"$TASK_PYTHON" - "$VERIFY_PORT" <<'PY'
import json, sys, time, urllib.request
url = 'http://127.0.0.1:' + sys.argv[1] + '/healthz'
for attempt in range(60):
    try:
        with urllib.request.urlopen(url, timeout=1) as response:
            assert json.load(response) == {'status':'ok'}
        break
    except (OSError, AssertionError): time.sleep(.25)
else: raise SystemExit('Verification server did not become healthy. Inspect .tools/verification-server.log.')
PY
cd "$APP_DIR/frontend"
npx playwright install chromium
E2E_BASE_URL="http://127.0.0.1:$VERIFY_PORT" npm run test:e2e
"$TASK_PYTHON" "$APP_DIR/tools/readiness.py"
echo "Local verification passed. Cloud readiness is reported separately above."
