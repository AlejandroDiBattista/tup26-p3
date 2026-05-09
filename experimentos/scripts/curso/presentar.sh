#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=scripts/curso/common.sh
source "$script_dir/common.sh"

usage() {
    cat <<'TXT'
Uso:
  bash scripts/curso/presentar.sh [--dry-run]
TXT
}

dry_run=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        --dry-run)
            dry_run=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        -*)
            course_die "Opcion desconocida: $1"
            ;;
        *)
            break
            ;;
    esac
done

[[ $# -eq 0 ]] || {
    usage
    exit 1
}

course_require_command git
course_require_command gh
course_require_command python3

repo_root=$(course_repo_root)
cd "$repo_root"

current_branch=$(git branch --show-current)
[[ -n "$current_branch" ]] || course_die "No hay ninguna rama activa."

if [[ ! "$current_branch" =~ ^(tp[0-9]+)-([0-9]+)$ ]]; then
    course_die "La rama actual '$current_branch' no sigue el formato tpN-LEGajo."
fi

tp=${BASH_REMATCH[1]}
legajo=${BASH_REMATCH[2]}
tp_label=$(course_tp_label "$tp")
student_dir=$(course_student_dir "$repo_root" "$legajo")
student_name=$(course_student_name "$student_dir")
student_rel=$(course_student_relpath "$repo_root" "$student_dir")
pr_title="$tp_label - $legajo - $student_name"
pr_body=$(cat <<TXT
Entrega automatizada para $pr_title.

- Carpeta del alumno: $student_rel
TXT
)

outside_changes=$(course_has_changes_outside "$student_rel" || true)
[[ -z "$outside_changes" ]] || course_die "Hay cambios fuera de $student_rel. Revisa esos archivos antes de usar /presentar.\n$outside_changes"

student_changes=$(git status --porcelain -- "$student_rel")

if [[ $dry_run -eq 1 ]]; then
    cat <<TXT
branch=$current_branch
student_dir=$student_dir
student_name=$student_name
pr_title=$pr_title
has_student_changes=$([[ -n "$student_changes" ]] && echo true || echo false)
dry_run=true
TXT
    exit 0
fi

if [[ -n "$student_changes" ]]; then
    git add --all -- "$student_rel"
    git commit -m "$pr_title"
fi

git push -u origin "$current_branch"

existing_url=$(gh pr list --head "$current_branch" --base main --state open --json url --jq '.[0].url // empty')
existing_title=$(gh pr list --head "$current_branch" --base main --state open --json title --jq '.[0].title // empty')

if [[ -n "$existing_url" ]]; then
    pr_url=$existing_url
    pr_title=$existing_title
else
    ahead_count=$(git rev-list --count "main..$current_branch")
    [[ "$ahead_count" -gt 0 ]] || course_die "La rama $current_branch no tiene commits para presentar contra main."
    pr_url=$(gh pr create --base main --head "$current_branch" --title "$pr_title" --body "$pr_body")
fi

git switch main >/dev/null
git pull --ff-only origin main
git branch -D "$current_branch" >/dev/null

cat <<TXT
branch=$current_branch
student_dir=$student_dir
student_name=$student_name
pr_title=$pr_title
pr_url=$pr_url
returned_to=main
local_branch_deleted=true
dry_run=false
TXT
