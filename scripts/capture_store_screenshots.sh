#!/usr/bin/env bash
set -euo pipefail

ROOT="/home/ubuntu/desktop-bunny-pet"
APP="$ROOT/dist/my-bunny-desktop-pet-linux-x64/my-bunny-desktop-pet"
OUT="$ROOT/store-assets/screenshots"
mkdir -p "$OUT"
rm -f "$OUT"/*.png

export ROOT APP OUT
xvfb-run -a -s "-screen 0 1366x768x24" bash <<'INNER'
set -euo pipefail
openbox >/tmp/bunny-openbox.log 2>&1 &
WM_PID=$!
sleep 0.7
picom --backend xrender --vsync=false >/tmp/bunny-picom.log 2>&1 &
COMPOSITOR_PID=$!
sleep 0.5
xsetroot -solid '#d8d4cf'
"$APP" --disable-gpu >/tmp/bunny-capture-app.log 2>&1 &
APP_PID=$!
cleanup() {
  kill -TERM "$APP_PID" 2>/dev/null || true
  kill -TERM "$COMPOSITOR_PID" 2>/dev/null || true
  kill -TERM "$WM_PID" 2>/dev/null || true
  wait "$APP_PID" 2>/dev/null || true
  wait "$COMPOSITOR_PID" 2>/dev/null || true
  wait "$WM_PID" 2>/dev/null || true
}
trap cleanup EXIT

sleep 1.2
scrot "$OUT/01-welcome.png"

WINDOW_ID="$(xdotool search --name '내 토끼' | head -n 1)"
xdotool windowmove "$WINDOW_ID" 1000 300
sleep 0.3
xdotool mousemove 1053 360 click 1
sleep 0.28
scrot "$OUT/02-petting.png"

sleep 2.2
xdotool mousemove 1053 360 click 3
sleep 0.6
scrot "$OUT/03-menu.png"
xdotool key Escape
sleep 0.4

xdotool windowmove "$WINDOW_ID" 630 330
sleep 0.8
scrot "$OUT/04-movable.png"
INNER

file "$OUT"/*.png
