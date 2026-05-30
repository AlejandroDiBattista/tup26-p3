#!/usr/bin/env bash
set -euo pipefail

find . -type d -name "tpx" -depth | while IFS= read -r dir; do
  parent="$(dirname "$dir")"
  target="$parent/tp1"

  if [ -e "$target" ]; then
    echo "Saltando: '$dir' porque ya existe '$target'"
  else
    mv "$dir" "$target"
    echo "Renombrado: '$dir' -> '$target'"
  fi
done