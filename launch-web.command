#!/bin/bash
# ============================================
#  OpenDeepWiki (Web UI) — Double-click Launcher (macOS)
#  Starts .NET API backend + web/ Next.js frontend
#  This is the same stack that Docker runs.
# ============================================

# cd into the directory where this script lives,
# so it works when double-clicked from Finder
cd "$(dirname "$0")"

# ── Colors ──────────────────────────────────
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}   OpenDeepWiki (Web UI) — Local Launcher${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""

# ── Initialize and activate conda ──────────
# Required for non-interactive shells (e.g., double-clicking .command in Finder)
if [ -f "/opt/homebrew/Caskroom/miniconda/base/etc/profile.d/conda.sh" ]; then
  source "/opt/homebrew/Caskroom/miniconda/base/etc/profile.d/conda.sh"
elif [ -f "$HOME/miniconda3/etc/profile.d/conda.sh" ]; then
  source "$HOME/miniconda3/etc/profile.d/conda.sh"
elif [ -f "$HOME/anaconda3/etc/profile.d/conda.sh" ]; then
  source "$HOME/anaconda3/etc/profile.d/conda.sh"
else
  eval "$(conda shell.bash hook 2>/dev/null)"
fi

conda activate deepwiki
if [ $? -ne 0 ]; then
  echo -e "${RED}✗ Failed to activate conda environment 'deepwiki'.${NC}"
  echo "  Create it first (see README)."
  exit 1
fi
echo -e "${GREEN}✓${NC} Conda environment 'deepwiki' activated"

# ── Pre-flight checks ──────────────────────
check_command() {
  if ! command -v "$1" &> /dev/null; then
    echo -e "${RED}✗ '$1' is not installed or not on PATH.${NC}"
    echo "  Please install it before running this launcher."
    exit 1
  fi
}

check_command node
check_command npm
check_command dotnet

# ── Load .env ──────────────────────────────
if [ -f .env ]; then
  echo -e "${GREEN}✓${NC} Loading .env"
  set -a
  source .env
  set +a
else
  echo -e "${YELLOW}⚠ No .env file found — using defaults / existing env vars${NC}"
fi

# ── Set API_PROXY_URL for the web/ frontend ─
# The .NET backend listens on port 5265 in development mode (launchSettings.json)
export API_PROXY_URL="http://localhost:5265"

# ── Install web/ frontend deps if needed ───
if [ ! -d "web/node_modules" ]; then
  echo -e "${YELLOW}→ Installing web/ frontend dependencies...${NC}"
  (cd web && npm install)
fi

# ── Trap: clean up child processes on exit ──
cleanup() {
  echo ""
  echo -e "${YELLOW}Shutting down...${NC}"
  kill -- -$$ 2>/dev/null
  exit 0
}
trap cleanup SIGINT SIGTERM EXIT

# ── Kill any stale .NET process on port 5265 ─
STALE_PID=$(lsof -ti :5265 2>/dev/null)
if [ -n "$STALE_PID" ]; then
  echo -e "${YELLOW}→ Killing stale process on port 5265 (PID $STALE_PID)...${NC}"
  kill "$STALE_PID" 2>/dev/null
  sleep 1
fi

# ── Ensure data directory exists ────────────
mkdir -p data

# ── Start .NET API backend ─────────────────
echo ""
echo -e "${GREEN}▶ Starting .NET API backend${NC}  (http://localhost:5265)"
dotnet run --project src/OpenDeepWiki/OpenDeepWiki.csproj &
DOTNET_PID=$!

# Give the .NET backend time to compile and start
echo -e "${YELLOW}  Waiting for .NET backend to start (this may take a moment on first run)...${NC}"
sleep 10

# ── Start web/ Next.js frontend ────────────
echo -e "${GREEN}▶ Starting web/ Next.js frontend${NC}  (http://localhost:3000)"
(cd web && npm run dev) &
NEXT_PID=$!

# ── Wait a moment then open the browser ───
sleep 4
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}  Services running:${NC}"
echo -e "    .NET backend →  ${GREEN}http://localhost:5265${NC}"
echo -e "    Web frontend →  ${GREEN}http://localhost:3000${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop all services.${NC}"
echo ""

# Open the frontend in the default browser
open "http://localhost:3000" 2>/dev/null

# Wait for both background processes
wait $DOTNET_PID $NEXT_PID
