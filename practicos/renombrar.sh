#!/usr/bin/env bash
set -euo pipefail

find . -type d -name "TP1" -depth -print0 | while IFS= read -r -d '' dir; do
  parent="$(dirname "$dir")"
  target="$parent/tp1"

  if [ -e "$target" ]; then
    source_id="$(stat -f '%d:%i' "$dir")"
    target_id="$(stat -f '%d:%i' "$target")"

    if [ "$source_id" = "$target_id" ]; then
      tmp="$parent/.tp1-renombre-temporal.$$.$RANDOM"
      while [ -e "$tmp" ]; do
        tmp="$parent/.tp1-renombre-temporal.$$.$RANDOM"
      done

      mv "$dir" "$tmp"
      mv "$tmp" "$target"
      echo "Renombrado: '$dir' -> '$target' (vía '$tmp')"
    else
      echo "Saltando: '$dir' porque ya existe '$target'"
    fi
  else
    mv "$dir" "$target"
    echo "Renombrado: '$dir' -> '$target'"
  fi
done