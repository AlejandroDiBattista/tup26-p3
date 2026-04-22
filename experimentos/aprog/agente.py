import asyncio
import subprocess
from pathlib import Path

from agents import (
    Agent,
    ApplyPatchTool,
    Runner,
    SQLiteSession,
    ShellCallOutcome,
    ShellCommandOutput,
    ShellCommandRequest,
    ShellResult,
    ShellTool,
    apply_diff,
)
from agents.editor import ApplyPatchEditor, ApplyPatchOperation, ApplyPatchResult

WORKSPACE = Path(__file__).resolve().parent
PROMPT_FILE = WORKSPACE / "nanop_prompt.txt"

BASE_RULES = f"""
Reglas operativas:
- Trabajás en {WORKSPACE}.
- Para leer o explorar, usá shell (ls, cat, grep, find).
- Para crear, modificar o borrar archivos, usá apply_patch con un diff unificado.
- Nunca edites archivos con comandos de shell (heredocs, sed, echo >).
- Antes de modificar un archivo existente, leelo con `cat`.
- Si apply_patch falla, reportá el error y no reintentes.
- No inventes resultados. Respondé corto.
""".strip()


def build_instructions() -> str:
    if PROMPT_FILE.exists():
        header = PROMPT_FILE.read_text(encoding="utf-8").format(workspace=WORKSPACE).strip()
        return f"{header}\n\n{BASE_RULES}"
    return BASE_RULES


async def local_shell(request: ShellCommandRequest) -> ShellResult:
    action = request.data.action
    outputs = []
    for cmd in action.commands:
        proc = subprocess.run(cmd, shell=True, cwd=WORKSPACE, capture_output=True, text=True)
        outputs.append(
            ShellCommandOutput(
                command=cmd,
                stdout=proc.stdout,
                stderr=proc.stderr,
                outcome=ShellCallOutcome(type="exit", exit_code=proc.returncode),
            )
        )
    return ShellResult(output=outputs, max_output_length=action.max_output_length)


class WorkspaceEditor(ApplyPatchEditor):
    def _resolve(self, path: str) -> Path:
        target = (WORKSPACE / path).resolve()
        if WORKSPACE not in target.parents and target != WORKSPACE:
            raise ValueError(f"Ruta fuera del workspace: {path}")
        return target

    async def create_file(self, op: ApplyPatchOperation) -> ApplyPatchResult:
        target = self._resolve(op.path)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(apply_diff("", op.diff, "create"), encoding="utf-8")
        return ApplyPatchResult(status="completed", output=f"Creado {op.path}")

    async def update_file(self, op: ApplyPatchOperation) -> ApplyPatchResult:
        target = self._resolve(op.path)
        target.write_text(apply_diff(target.read_text(encoding="utf-8"), op.diff), encoding="utf-8")
        return ApplyPatchResult(status="completed", output=f"Actualizado {op.path}")

    async def delete_file(self, op: ApplyPatchOperation) -> ApplyPatchResult:
        self._resolve(op.path).unlink()
        return ApplyPatchResult(status="completed", output=f"Borrado {op.path}")


agent = Agent(
    name="NanoProg",
    model="gpt-5.4",
    instructions=build_instructions(),
    tools=[ShellTool(executor=local_shell), ApplyPatchTool(editor=WorkspaceEditor())],
)


async def main() -> None:
    session = SQLiteSession("nanoprog")
    while True:
        try:
            user_input = input("Tú> ").strip()
        except (EOFError, KeyboardInterrupt):
            break
        if not user_input or user_input.lower() in {"salir", "exit", "quit"}:
            if not user_input:
                continue
            break

        result = await Runner.run(agent, user_input, session=session)
        print(f"\nAgente> {result.final_output}\n")


if __name__ == "__main__":
    asyncio.run(main())