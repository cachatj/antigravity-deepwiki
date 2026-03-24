#!/bin/bash
# ============================================
#  OpenDeepWiki — Double-click Launcher (macOS)
#  Starts API backend + Next.js frontend
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
echo -e "${CYAN}       OpenDeepWiki  —  Local Launcher${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""

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
check_command conda

# Verify the deepwiki conda env exists
if ! conda env list | grep -q "deepwiki"; then
  echo -e "${RED}✗ Conda environment 'deepwiki' not found.${NC}"
  echo "  Create it first (see README)."
  exit 1
fi

# ── Load .env ──────────────────────────────
if [ -f .env ]; then
  echo -e "${GREEN}✓${NC} Loading .env"
  set -a
  source .env
  set +a
else
  echo -e "${YELLOW}⚠ No .env file found — using defaults / existing env vars${NC}"
fi

# ── Install frontend deps if needed ────────
if [ ! -d "node_modules" ]; then
  echo -e "${YELLOW}→ Installing frontend dependencies...${NC}"
  npm install
fi

# ── Trap: clean up child processes on exit ──
cleanup() {
  echo ""
  echo -e "${YELLOW}Shutting down...${NC}"
  # Kill the whole process group
  kill -- -$$ 2>/dev/null
  exit 0
}
trap cleanup SIGINT SIGTERM EXIT

# ── Start API backend ─────────────────────
echo ""
echo -e "${GREEN}▶ Starting API backend${NC}  (http://localhost:${PORT:-8001})"
conda run --no-capture-output -n deepwiki python -m api.main &
API_PID=$!

# Give the API a moment to bind
sleep 2

# ── Start Next.js frontend ────────────────
echo -e "${GREEN}▶ Starting Next.js frontend${NC}  (http://localhost:3000)"
npm run dev &
NEXT_PID=$!

# ── Wait a moment then open the browser ───
sleep 4
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}  Services running:${NC}"
echo -e "    API backend  →  ${GREEN}http://localhost:${PORT:-8001}${NC}"
echo -e "    Frontend     →  ${GREEN}http://localhost:3000${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop all services.${NC}"
echo ""

# Open the frontend in the default browser
open "http://localhost:3000" 2>/dev/null

# Wait for both background processes
wait $API_PID $NEXT_PID
