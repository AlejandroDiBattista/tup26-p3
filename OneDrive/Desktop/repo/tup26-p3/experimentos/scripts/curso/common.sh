#!/usr/bin/env bash
set -euo pipefail

course_die() {
    echo "Error: $*" >&2
    exit 1
}

course_require_command() {
    command -v "$1" >/dev/null 2>&1 || course_die "No se encontro el comando requerido: $1"
}

course_repo_root() {
    git rev-parse --show-toplevel
}

course_normalize_tp() {
    local raw
    raw=$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')
    raw=${raw// /}
    raw=${raw//_/}
    raw=${raw//-/}

    [[ -n "$raw" ]] || course_die "El TP no puede estar vacio."

    if [[ "$raw" =~ ^tp[0-9]+$ ]]; then
        printf '%s\n' "$raw"
        return
    fi

    if [[ "$raw" =~ ^[0-9]+$ ]]; then
        printf 'tp%s\n' "$raw"
        return
    fi

    course_die "TP invalido: '$1'. Usa formatos como tp1 o 1."
}

course_normalize_legajo() {
    [[ "$1" =~ ^[0-9]+$ ]] || course_die "Legajo invalido: '$1'."
    printf '%s\n' "$1"
}

course_student_dir() {
    local repo_root=$1
    local legajo=$2
    local practicos_dir="$repo_root/practicos"
    local matches=()
    local match

    [[ -d "$practicos_dir" ]] || course_die "No existe la carpeta practicos en $repo_root."

    while IFS= read -r match; do
        matches+=("$match")
    done < <(find "$practicos_dir" -mindepth 1 -maxdepth 1 -type d -name "$legajo -*" | sort)

    if [[ ${#matches[@]} -eq 0 ]]; then
        course_die "No se encontro ninguna carpeta para el legajo $legajo dentro de practicos/."
    fi

    if [[ ${#matches[@]} -gt 1 ]]; then
        printf 'Coincidencias encontradas para %s:\n' "$legajo" >&2
        printf ' - %s\n' "${matches[@]}" >&2
        course_die "El legajo $legajo coincide con mas de una carpeta."
    fi

    printf '%s\n' "${matches[0]}"
}

course_student_name() {
    local student_dir=$1
    local base_name
    base_name=$(basename "$student_dir")
    printf '%s\n' "${base_name#* - }"
}

course_branch_name() {
    local tp=$1
    local legajo=$2
    printf '%s-%s\n' "$tp" "$legajo"
}

course_tp_label() {
    printf '%s\n' "$(printf '%s' "$1" | tr '[:lower:]' '[:upper:]')"
}

course_student_relpath() {
    local repo_root=$1
    local student_dir=$2
    python3 - "$repo_root" "$student_dir" <<'PY'
import os
import sys
print(os.path.relpath(sys.argv[2], sys.argv[1]))
PY
}

course_workspace_file() {
    local repo_root=$1
    local student_dir=$2
    local student_name=$3
    local legajo=$4
    local workspace_file="/tmp/tup26-p3-${legajo}.code-workspace"

    python3 - "$workspace_file" "$student_dir" "$repo_root" "$student_name" <<'PY'
import json
import sys

workspace_file, student_dir, repo_root, student_name = sys.argv[1:5]
data = {
    "folders": [
        {"name": student_name, "path": student_dir},
        {"name": "Repositorio", "path": repo_root},
    ],
    "settings": {
        "chat.promptFiles": True,
        "chat.useCustomizationsInParentRepositories": True,
    },
}
with open(workspace_file, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
    f.write("\n")
print(workspace_file)
PY
}

course_open_workspace() {
    local workspace_file=$1

    if command -v code >/dev/null 2>&1; then
        code -r "$workspace_file" >/dev/null 2>&1 &
        return 0
    fi

    if command -v open >/dev/null 2>&1; then
        open -a "Visual Studio Code" "$workspace_file" >/dev/null 2>&1
        return 0
    fi

    return 1
}

course_has_changes_outside() {
    local student_rel=$1
    local output

    output=$( {
        git diff --name-only -- . ":(exclude)$student_rel"
        git diff --cached --name-only -- . ":(exclude)$student_rel"
        git ls-files --others --exclude-standard -- . ":(exclude)$student_rel"
    } | sed '/^$/d' | sort -u )

    [[ -n "$output" ]] && printf '%s\n' "$output"
}
