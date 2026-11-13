#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

log() { echo "$(date '+%Y-%m-%d %H:%M:%S') $*"; }

if ! git pull --ff-only --quiet; then
  log "git pull impossible (modifications locales ?), docker-compose.yaml non mis à jour"
fi

running_image() {
  docker inspect --format '{{.Image}}' "$(docker compose ps -q agendabot 2>/dev/null)" 2>/dev/null || true
}

before=$(running_image)

docker compose pull --quiet agendabot
docker compose up -d --remove-orphans

after=$(running_image)

if [[ "$before" != "$after" ]]; then
  log "AgendaBot mis à jour : ${before:0:19} -> ${after:0:19}"
  docker image prune -f >/dev/null
fi
