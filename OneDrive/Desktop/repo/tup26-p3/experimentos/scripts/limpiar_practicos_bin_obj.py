#!/usr/bin/env python3

from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_PRACTICOS = REPO_ROOT / "practicos"
TARGET_NAMES = {"bin", "obj"}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Borra recursivamente carpetas bin y obj dentro de practicos."
    )
    parser.add_argument(
        "ruta",
        nargs="?",
        default=DEFAULT_PRACTICOS,
        type=Path,
        help="Ruta base a limpiar. Por defecto usa la carpeta practicos del repositorio.",
    )
    parser.add_argument(
        "-n",
        "--dry-run",
        action="store_true",
        help="Muestra qué carpetas se borrarían sin eliminarlas.",
    )
    return parser.parse_args()


def find_targets(practicos_dir: Path) -> list[Path]:
    if not practicos_dir.exists() or not practicos_dir.is_dir():
        raise FileNotFoundError(f"La ruta '{practicos_dir}' no existe o no es un directorio.")

    targets = [
        path for path in practicos_dir.rglob("*")
        if path.is_dir() and not path.is_symlink() and path.name.lower() in TARGET_NAMES
    ]
    return sorted(targets, key=lambda path: len(path.parts), reverse=True)


def remove_targets(targets: list[Path], dry_run: bool) -> int:
    if not targets:
        print("No se encontraron carpetas bin u obj para borrar.")
        return 0

    action = "Borraría" if dry_run else "Borrando"
    for path in targets:
        print(f"{action}: {path}")
        if not dry_run:
            shutil.rmtree(path)

    print(f"Total: {len(targets)} carpeta(s) {'a borrar' if dry_run else 'borradas' }.")
    return 0


def main() -> int:
    args = parse_args()

    try:
        targets = find_targets(args.ruta.resolve())
    except FileNotFoundError as exc:
        print(exc, file=sys.stderr)
        return 1

    return remove_targets(targets, dry_run=args.dry_run)


if __name__ == "__main__":
    raise SystemExit(main())