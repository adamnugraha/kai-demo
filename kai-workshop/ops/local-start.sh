#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v docker >/dev/null || { echo 'Install Docker Engine and the Compose plugin first.'; exit 1; }
test -f .env || python3 ops/new-env.py
docker compose config --quiet
docker compose up --build -d
echo 'Open http://127.0.0.1:8080. For a remote VPS use an SSH tunnel. See docs/ACCESS.md.'
