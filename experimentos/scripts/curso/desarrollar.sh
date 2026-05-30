#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=scripts/curso/common.sh
source "$script_dir/common.sh"

usage() {
    cat <<'TXT'
Uso:
  bash scripts/curso/desarrollar.sh [--dry-run] [--no-editor] <tp> <legajo>
TXT
}

dry_run=0
open_editor=1

while [[ $# -gt 0 ]]; do
    case "$1" in
        --dry-run)
            dry_run=1
            shift
            ;;
        --no-editor)
            open_editor=0
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        --)
            shift
            break
            ;;
        -*)
            course_die "Opcion desconocida: $1"
            ;;
        *)
            break
            ;;
    esac
done

[[ $# -eq 2 ]] || {
    usage
    exit 1
}

course_require_command git
course_require_command python3

requested_tp=$1
requested_legajo=$2

tp=$(course_normalize_tp "$requested_tp")
legajo=$(course_normalize_legajo "$requested_legajo")
branch=$(course_branch_name "$tp" "$legajo")
repo_root=$(course_repo_root)
student_dir=$(course_student_dir "$repo_root" "$legajo")
student_name=$(course_student_name "$student_dir")
workspace_file=$(course_workspace_file "$repo_root" "$student_dir" "$student_name" "$legajo")
editor_opened=false

cd "$repo_root"

if [[ $dry_run -eq 1 ]]; then
    cat <<TXT
student_dir=$student_dir
student_name=$student_name
branch=$branch
workspace_file=$workspace_file
editor_opened=$editor_opened
dry_run=true
TXT
    exit 0
fi

status_output=$(git status --porcelain)
[[ -z "$status_output" ]] || course_die "El repositorio tiene cambios sin guardar. Hace commit, stash o limpia el arbol antes de usar /desarrollar."

git fetch origin --prune

git switch main >/dev/null
git pull --ff-only origin main

if git show-ref --verify --quiet "refs/heads/$branch"; then
    git switch "$branch" >/dev/null
elif git show-ref --verify --quiet "refs/remotes/origin/$branch"; then
    git switch --track -c "$branch" "origin/$branch" >/dev/null
else
    git switch -c "$branch" >/dev/null
fi

if [[ $open_editor -eq 1 ]]; then
    if course_open_workspace "$workspace_file"; then
        editor_opened=true
    else
        course_die "No se pudo abrir Visual Studio Code automaticamente. El workspace generado es: $workspace_file"
    fi
fi

cat <<TXT
student_dir=$student_dir
student_name=$student_name
branch=$branch
workspace_file=$workspace_file
editor_opened=$editor_opened
dry_run=false
TXT
