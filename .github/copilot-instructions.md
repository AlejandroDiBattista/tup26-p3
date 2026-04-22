# Curso workflow instructions for Copilot

- Student work lives under `practicos/LEGajo - Nombre completo`.
- When the user wants to start or resume a TP for one student, prefer the `/desarrollar` prompt.
- When the user wants to submit a TP branch, prefer the `/presentar` prompt.
- Never guess the student folder name. Resolve it by matching the folder prefix `LEGajo - ` inside `practicos/`.
- Development branches must be named `tpN-LEGajo`, for example `tp1-63217`.
- Pull request titles must use the format `TPN - LEGajo - Nombre completo`.
- For development setup, always start from the latest `main` before creating a new branch.
- For presentation, do not delete the remote branch after opening the PR. Only return to `main` and delete the local branch.
- If one of the course scripts fails, stop and report the exact error instead of improvising manual Git commands.
- Use these scripts from the repository root:
  - `bash scripts/curso/desarrollar.sh <tp> <legajo>`
  - `bash scripts/curso/presentar.sh`
