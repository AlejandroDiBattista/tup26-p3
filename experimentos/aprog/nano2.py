import asyncio
import subprocess
from pathlib import Path

from agents import Agent, Runner, SQLiteSession, ShellCallOutcome, ShellCommandOutput, ShellCommandRequest, ShellResult, ShellTool

WORKSPACE = Path(__file__).resolve().parent
PROMPT_FILE = WORKSPACE / "nanop_prompt.txt"

BASE_RULES = f"""
Reglas operativas:
- Trabajás en {WORKSPACE}.
- Antes de modificar, leé archivos relevantes.
- Usá shell para leer, escribir y ejecutar.
- No inventes resultados.
- Respondé corto.
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
        proc = subprocess.run( cmd, shell=True, cwd=WORKSPACE, capture_output=True, text=True )
        outputs.append(
            ShellCommandOutput(
                command=cmd,
                stdout=proc.stdout,
                stderr=proc.stderr,
                outcome=ShellCallOutcome(type="exit", exit_code=proc.returncode),
            )
        )
    return ShellResult(output=outputs, max_output_length=action.max_output_length)


agent = Agent(
    name="Nano",
    model="gpt-5.4",
    instructions=build_instructions(),
    tools=[ShellTool(executor=local_shell)],
)


async def main() -> None:
    session = SQLiteSession("nanoprog")
    while True:
        try:
            user_input = input("Tú> ").strip()
        except (EOFError, KeyboardInterrupt):
            break
        if not user_input:
            continue
        if user_input.lower() in {"salir", "exit", "quit"}:
            break

        result = await Runner.run(agent, user_input, session=session)
        print(f"\nAgente> {result.final_output}\n")


if __name__ == "__main__":
    asyncio.run(main())