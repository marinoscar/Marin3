#!/usr/bin/env bash
#
# update.sh — Zero-downtime-ish updater for MarinApp on Ubuntu
#
# Location: /root/marinapp/update.sh
#
# What it does
#   - Verifies prerequisites (docker, docker compose, git, curl)
#   - Pulls latest code from GitHub repo at /root/marinapp/Marin3
#   - Rebuilds the app image using docker-compose.yml in /root/marinapp
#   - Restarts the stack with existing volumes (preserves DP keys & data)
#   - Health-checks the app via 127.0.0.1:9095/health (configurable)
#   - (Optional) Runs EF Core migrations inside the container
#
# Usage
#   sudo /root/marinapp/update.sh
#   sudo /root/marinapp/update.sh --migrate
#   sudo /root/marinapp/update.sh --no-healthcheck
#
# Notes
#   - Run this from ANY directory (script cd’s itself).
#   - Expects these paths:
#       /root/marinapp                 (compose folder, .env lives here)
#       /root/marinapp/Marin3          (cloned repo)
#       /root/marinapp/Marin3/src      (build context)
#   - Never uses `docker compose down -v` (so volumes are safe).
#   - Health endpoint defaults to http://127.0.0.1:9095/health
#     Change APP_HEALTH_URL below if needed.
#
set -euo pipefail

########## CONFIG (edit if needed) ##########
APP_ROOT="/root/marinapp"
REPO_DIR="${APP_ROOT}/Marin3"
COMPOSE_FILE="${APP_ROOT}/docker-compose.yml"
ENV_FILE="${APP_ROOT}/.env"
APP_SERVICE_NAME="marinapp"
APP_HEALTH_URL="${APP_HEALTH_URL:-http://127.0.0.1:9095/health}"
HEALTH_MAX_RETRIES="${HEALTH_MAX_RETRIES:-30}"
HEALTH_SLEEP_SECONDS="${HEALTH_SLEEP_SECONDS:-2}"
GIT_BRANCH="${GIT_BRANCH:-main}"           # change if you deploy a different branch
RUN_MIGRATIONS="false"                     # can be toggled by --migrate
DO_HEALTHCHECK="true"                      # can be disabled by --no-healthcheck
#############################################

# Pretty logging
log()  { printf "\033[1;32m[INFO]\033[0m %s\n" "$*"; }
warn() { printf "\033[1;33m[WARN]\033[0m %s\n" "$*"; }
err()  { printf "\033[1;31m[ERR ]\033[0m %s\n" "$*"; }
die()  { err "$*"; exit 1; }

# Parse flags
for arg in "${@:-}"; do
  case "$arg" in
    --migrate) RUN_MIGRATIONS="true" ;;
    --no-healthcheck) DO_HEALTHCHECK="false" ;;
    *) die "Unknown flag: $arg (use --migrate or --no-healthcheck)";;
  case_esac=true
  done
done

# Require commands
require_cmd() { command -v "$1" >/dev/null 2>&1 || die "Missing required command: $1"; }
require_cmd docker
require_cmd git
require_cmd curl

# Ensure docker compose plugin exists (docker compose v2)
if ! docker compose version >/dev/null 2>&1; then
  die "Docker Compose plugin not found. Install Docker Compose v2 (docker compose)."
fi

# Ensure paths exist
[ -d "$APP_ROOT" ] || die "APP_ROOT not found: $APP_ROOT"
[ -d "$REPO_DIR" ] || die "Repo dir not found: $REPO_DIR (did you git clone?)"
[ -f "$COMPOSE_FILE" ] || die "Compose file not found: $COMPOSE_FILE"
[ -f "$ENV_FILE" ] || warn ".env not found at $ENV_FILE — continuing (compose may still work if env vars are inline)."

# Show current versions
log "Docker: $(docker --version)"
log "Compose: $(docker compose version | head -n1)"
log "Git: $(git --version)"

# Pull latest code
log "Fetching latest code in $REPO_DIR (branch: $GIT_BRANCH)…"
(
  cd "$REPO_DIR"
  git fetch origin "$GIT_BRANCH"
  # Show diff summary (optional)
  CHANGES=$(git rev-list --left-right --count "HEAD...origin/${GIT_BRANCH}" || echo "")
  log "Ahead/Behind (local...origin/${GIT_BRANCH}): ${CHANGES:-unknown}"
  log "Pulling…"
  git checkout "$GIT_BRANCH"
  git pull --ff-only origin "$GIT_BRANCH"
)

# Validate compose config (merged)
log "Validating docker-compose config…"
docker compose -f "$COMPOSE_FILE" config >/dev/null

# Build (uses cache when possible)
log "Building images (this may take a bit)…"
(
  cd "$APP_ROOT"
  docker compose build
)

# Bring up new containers
log "Recreating containers with new build…"
(
  cd "$APP_ROOT"
  docker compose up -d
)

# Optional: Health check
if [ "$DO_HEALTHCHECK" = "true" ]; then
  log "Health check at ${APP_HEALTH_URL} (max ${HEALTH_MAX_RETRIES}×, every ${HEALTH_SLEEP_SECONDS}s)…"
  i=0
  until curl -fsS "$APP_HEALTH_URL" >/dev/null 2>&1; do
    i=$((i+1))
    if [ "$i" -ge "$HEALTH_MAX_RETRIES" ]; then
      warn "Health endpoint still failing after ${HEALTH_MAX_RETRIES} tries."
      # Show last logs for triage
      log "Recent logs:"
      ( cd "$APP_ROOT" && docker compose logs --no-color --tail=200 "$APP_SERVICE_NAME" || true )
      die "Deployment incomplete — health check failed."
    fi
    sleep "$HEALTH_SLEEP_SECONDS"
  done
  log "Health check passed ✔"
else
  warn "Health check skipped (--no-healthcheck)."
fi


# Show status & recent logs tail
log "Containers:"
( cd "$APP_ROOT" && docker compose ps )
log "Recent app logs (tail 60):"
( cd "$APP_ROOT" && docker compose logs --no-color --tail=60 "$APP_SERVICE_NAME" || true )

log "✅ Update finished. If you’re behind Nginx, your app should be live at https://app.marin.cr"
